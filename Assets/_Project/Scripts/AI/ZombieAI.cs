// NavMeshAgent 기반 좀비 상태 머신이다. 감지(시야/소리), 조사, 추적, 수색, 공격, 사망을 분리해서 처리한다.
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))]
public class ZombieAI : MonoBehaviour
{
    private enum State { Idle, Investigate, Chase, Search, Attack, Dead }

    // 시야로 알아채는 최대 거리다(정면 시야각 안이고 사이에 가리는 것이 없을 때).
    [SerializeField] private float detectionRange = 12f;
    // 이 거리 안에서는 시야가 가려져도(냄새, 인기척) 바로 알아챈다.
    [SerializeField] private float proximityRange = 5f;
    // 알아채는 시야각(정면 기준 한쪽 각도)이다. 등 뒤는 못 본다.
    [SerializeField] private float perceptionHalfAngle = 100f;
    // 추적 중에는 이 거리 안이면 시야가 가려져도 위치를 계속 안다. 그보다 멀리 숨으면 마지막 위치로만 쫓는다.
    [SerializeField] private float trackRange = 14f;
    // 위치를 놓친 뒤 추적을 포기하고 수색으로 바꾸기까지의 시간, 마지막 위치에서 둘러보는 시간(초)이다.
    [SerializeField] private float loseSightTime = 5f;
    [SerializeField] private float searchDuration = 6f;
    // 소리에 끌려 간 자리에서 둘러보는 시간(초)과 소리를 듣는 배율(0이면 못 듣는다)이다.
    [SerializeField] private float investigateScanTime = 3f;
    [SerializeField] private float hearingScale = 1f;
    // 처음 발견했을 때 지르는 소리가 들리는 반경이다(근처 좀비가 몰려온다).
    [SerializeField] private float shoutRadius = 12f;
    [SerializeField] private float perceptionInterval = 0.2f;
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackCooldown = 1.2f;
    // 근접 공격의 타격 순간(공격 시작 뒤 초)이다. 공격 변형마다 하나씩이며 배열 길이가 곧 변형 수다(애니메이션의 때리는 순간에 맞춘다).
    [SerializeField] private float[] attackHitDelays = { 0.33f };
    // 피격 경직: 최대 체력 대비 이 비율 이상을 한 번에 맞으면 잠깐 멈추고 공격이 끊긴다. 저항(0~1)이 클수록 짧게 멈춘다.
    [SerializeField] private float staggerThreshold = 0.07f;
    [SerializeField, Range(0f, 1f)] private float staggerResistance = 0f;

    // 원거리 공격 좀비가 목표를 볼 수 있는지 판단하는 눈 높이와 시야각(정면 기준 한쪽 각도)이다.
    [SerializeField] private float eyeHeight = 1.6f;
    [SerializeField] private float sightHalfAngle = 75f;
    // 플레이어를 발견한 직후 제자리에서 포효하며 멈춰 있는 시간이다(0이면 바로 추적한다).
    [SerializeField] private float alertPause = 0f;
    // 목표까지 길이 건축물에 막혔을 때 근접 좀비가 막은 구조물 앞으로 가서 부수거나(문은 선택에 따라 밀어 연다) 하는 설정이다(원거리 좀비는 하지 않는다).
    public enum DoorBehavior { Break, Open }
    [SerializeField] private bool breachBlockers = true;
    [SerializeField] private DoorBehavior doorBehavior = DoorBehavior.Break;
    // 구조물에 주는 피해 배율이다(근접 피해에 곱한다).
    [SerializeField] private float structureDamageScale = 0.7f;
    // 거점이 없는 유예 기간(BaseThreat.Patrolling)에는 생성된 곳(home) 반경 안을 느릿하게 돌며 정찰한다. 쉬는 시간은 한 곳에 닿은 뒤 다음 곳으로 가기까지다.
    [SerializeField] private float patrolRadius = 14f;
    [SerializeField, Range(0.2f, 1f)] private float patrolSpeedScale = 0.5f;
    [SerializeField] private float patrolPauseMin = 2f;
    [SerializeField] private float patrolPauseMax = 6f;
    [SerializeField] private float doorSearchRange = 25f;
    [SerializeField] private float doorReach = 1.7f;
    [SerializeField] private float doorOpenDelay = 1.2f;
    // 문이 없을 때 길이 끊긴 지점에서 이 거리 안의 벽을 부순다.
    [SerializeField] private float wallSearchRadius = 3.5f;

    private NavMeshAgent agent;
    private Health health;
    private ZombieRangedAttack rangedAttack;
    private Transform target;
    private State state = State.Idle;
    private float nextAttackTime;
    private float resumeTime;
    // 피격 경직과 휘두르는 중인 근접 공격(타격 순간에 피해를 확정한다)이다.
    private float staggerUntil;
    private Vector3 pushVelocity;
    private enum PendingKind { None, Target, Structure }
    private PendingKind pendingKind;
    private float pendingHitTime;
    private BuildableHealth pendingStructure;
    // 지각 결과와 기억이다. noticed는 지금 알아챘는지(시야/근접), tracking은 추적을 이어 갈 만큼 위치를 아는지다.
    private bool noticed;
    private bool tracking;
    private float nextPerceive;
    private Vector3 lastKnownPosition;
    private float lastSeenTime;
    private Vector3 investigatePoint;
    private bool arrived;
    private float arrivedTime;
    private float stateStartTime;
    private float scanDirection = 1f;
    private float nextScanFlip;
    // 길을 막아 부수거나 열 구조물이다. 문이 아닌 벽이면 blockerDoor는 비고, 부술 수 없는 문이면 blocker가 빈다.
    private bool blockerActive;
    private BuildableHealth blocker;
    private BuildableDoor blockerDoor;
    private float doorPush;
    private float nextBlockerSearch;
    // 정찰: 태어난 자리(home), 지금 목적지로 가는 중인지, 다음 목적지를 고를 시각, 이동 시작 시각이다.
    private Vector3 home;
    private float baseSpeed;
    private bool patrolMoving;
    private float nextPatrolTime;
    private float patrolStartTime;

    // 소리 같은 표현이 상태를 직접 들여다보지 않고 구독할 수 있게 알린다.
    public event System.Action Alerted;
    public event System.Action Attacked;
    // 맞아서 경직될 때(경직 시간)를 알린다.
    public event System.Action<float> Staggered;
    // 방금 시작한 공격의 변형 번호다(애니메이션 선택에 쓴다).
    public int LastAttackVariant { get; private set; }
    public bool IsChasing => puppet ? remoteChasing : state == State.Chase || state == State.Attack;

    // 멀티플레이 참가자 쪽 좀비는 AI를 돌리지 않는 꼭두각시다. 모습과 소리에 필요한 사건(경계, 공격, 추적 중 여부)만 서버에서 받아 낸다.
    private bool puppet;
    private bool remoteChasing;
    private float nextTargetPick;

    public void BecomePuppet()
    {
        puppet = true;
        enabled = false;
    }

    public void RemoteAlerted() => Alerted?.Invoke();

    public void RemoteAttacked(int variant)
    {
        LastAttackVariant = variant;
        Attacked?.Invoke();
    }

    public void RemoteChasing(bool chasing) => remoteChasing = chasing;

    // 시험과 디버그용 상태 이름이다.
    public string DebugState => state.ToString();
    public Vector3 LastKnownPosition => lastKnownPosition;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();
        rangedAttack = GetComponent<ZombieRangedAttack>();
        home = transform.position;
        baseSpeed = agent.speed;
        health.Died += HandleDied;
        health.HitReceived += HandleHit;
    }

    private void OnEnable() => NoiseSystem.Emitted += HandleNoise;
    private void OnDisable() => NoiseSystem.Emitted -= HandleNoise;

    private void OnDestroy()
    {
        health.Died -= HandleDied;
        health.HitReceived -= HandleHit;
    }

    private void Update()
    {
        if (state == State.Dead) return;

        // 목표는 가장 가까운 살아 있는 플레이어다(멀티플레이에서는 호스트 플레이어와 참가자 아바타 중에서 고른다).
        if (target == null || Time.time >= nextTargetPick)
        {
            nextTargetPick = Time.time + 0.4f;
            target = PlayerTargets.Nearest(transform.position);
        }

        UpdatePendingHit();
        if (HandleStagger()) return;

        switch (state)
        {
            case State.Idle:
                TickIdle();
                break;
            case State.Investigate:
                TickInvestigate();
                break;
            case State.Chase:
                TickChase();
                break;
            case State.Search:
                TickSearch();
                break;
            case State.Attack:
                TickAttack();
                break;
        }
    }

    // ---- 피격 반응과 타격 순간 ----

    private void HandleHit(float amount, Vector3 direction, bool headshot)
    {
        if (puppet || state == State.Dead) return;

        // 맞으면 등 뒤에서라도 놀라서 플레이어를 알아챈다.
        if (target != null && (state == State.Idle || state == State.Investigate || state == State.Search)) BeginChase(true);

        float fraction = amount / Mathf.Max(1f, health.MaxHealth);
        if (fraction < staggerThreshold) return;

        float seconds = Mathf.Clamp(0.15f + 1.5f * fraction, 0.2f, 0.9f) * (headshot ? 1.4f : 1f) * (1f - staggerResistance);
        if (seconds < 0.12f) return;

        staggerUntil = Mathf.Max(staggerUntil, Time.time + seconds);
        // 휘두르던 공격은 끊긴다(맞았으면 타격이 나가지 않는다).
        pendingKind = PendingKind.None;
        pendingStructure = null;

        direction.y = 0f;
        pushVelocity = direction.sqrMagnitude > 0.001f ? direction.normalized * Mathf.Lerp(0.8f, 2.4f, Mathf.Clamp01(fraction * 3f)) * (1f - staggerResistance) : Vector3.zero;
        Staggered?.Invoke(seconds);
    }

    // 경직 중이면 멈춰 서서 맞은 방향으로 조금 밀려난다. 경직 중이면 true.
    private bool HandleStagger()
    {
        if (Time.time >= staggerUntil) return false;

        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            if (pushVelocity.sqrMagnitude > 0.01f)
            {
                agent.Move(pushVelocity * Time.deltaTime);
                pushVelocity = Vector3.MoveTowards(pushVelocity, Vector3.zero, 5f * Time.deltaTime);
            }
        }
        return true;
    }

    // 공격을 시작하고 타격 순간에 피해를 확정하도록 예약한다. 변형 번호는 애니메이션이 같이 쓴다.
    private void BeginMeleeSwing(PendingKind kind, BuildableHealth structure)
    {
        int variants = Mathf.Max(1, attackHitDelays.Length);
        LastAttackVariant = Random.Range(0, variants);

        // 예약을 먼저 해 두어야 공격 시작 알림을 듣는 쪽에서 맞아 끊기는 경우도 올바르게 취소된다.
        pendingKind = kind;
        pendingStructure = structure;
        pendingHitTime = Time.time + (attackHitDelays.Length > 0 ? attackHitDelays[LastAttackVariant] : 0f);
        Attacked?.Invoke();
    }

    // 타격 순간이 되면 피해를 확정한다. 그 사이 목표가 물러나거나 가려졌으면 빗나간다.
    private void UpdatePendingHit()
    {
        if (pendingKind == PendingKind.None || Time.time < pendingHitTime) return;

        PendingKind kind = pendingKind;
        BuildableHealth structure = pendingStructure;
        pendingKind = PendingKind.None;
        pendingStructure = null;

        if (kind == PendingKind.Target)
        {
            if (target == null) return;
            if (Vector3.Distance(transform.position, target.position) > attackRange * 1.3f || !CanSee(target, false)) return;

            PlayerTargets.Damage(target, attackDamage, (target.position - transform.position).normalized);
        }
        else if (structure != null && !structure.IsBroken)
        {
            Vector3 closest = structure.WorldBounds.ClosestPoint(transform.position);
            if (FlatDistance(transform.position, closest) > doorReach * 1.5f) return;

            structure.ApplyDamage(attackDamage * structureDamageScale, structure.WorldBounds.ClosestPoint(transform.position + Vector3.up));
            // 문/벽을 부수는 소리가 다른 좀비를 부른다.
            NoiseSystem.Emit(structure.WorldBounds.center, 14f, this);
        }
    }

    // ---- 지각 ----

    // 지각은 매 프레임이 아니라 일정 간격으로만 계산한다(시야 확인에 레이캐스트를 쓰므로).
    private void Perceive()
    {
        if (target == null)
        {
            noticed = false;
            tracking = false;
            return;
        }
        if (Time.time < nextPerceive) return;
        nextPerceive = Time.time + perceptionInterval + Random.Range(0f, 0.05f);

        // 거점이 있는 밤(또는 유예가 끝난 밤)에는 시야와 거리에 상관없이 플레이어 위치를 계속 알고 쫓아온다.
        if (BaseThreat.Hunting)
        {
            noticed = true;
            tracking = true;
            return;
        }

        float distance = Vector3.Distance(transform.position, target.position);
        noticed = distance <= proximityRange
            || (distance <= detectionRange && InPerceptionCone() && CanSee(target, false));
        tracking = noticed || distance <= trackRange;
    }

    private bool InPerceptionCone()
    {
        Vector3 flat = target.position - transform.position;
        flat.y = 0f;
        return flat.sqrMagnitude < 0.01f || Vector3.Angle(transform.forward, flat) <= perceptionHalfAngle;
    }

    // 총소리, 발소리, 망치질 같은 소음을 들으면 그 자리로 조사하러 간다(이미 추적/공격 중이면 무시한다).
    private void HandleNoise(Vector3 position, float radius, object source)
    {
        if (source == (object)this) return;
        if (state == State.Dead || state == State.Chase || state == State.Attack) return;
        if (hearingScale <= 0f || !agent.enabled || !agent.isOnNavMesh) return;

        // 벽 같은 것에 가려진 소리는 절반 거리까지만 들린다.
        float effective = radius * hearingScale;
        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        if (Physics.Linecast(eye, position + Vector3.up, out RaycastHit hit, AvatarBody.ExcludeMask, QueryTriggerInteraction.Ignore)
            && hit.collider.GetComponentInParent<ZombieAI>() != this
            && hit.collider.GetComponentInParent<Health>() == null)
        {
            effective *= 0.5f;
        }

        if (Vector3.Distance(transform.position, position) > effective) return;

        investigatePoint = NavMesh.SamplePosition(position, out NavMeshHit navHit, 4f, NavMesh.AllAreas) ? navHit.position : position;
        if (state != State.Investigate) EnterState(State.Investigate);
        arrived = false;
        agent.isStopped = false;
        agent.SetDestination(investigatePoint);
    }

    // ---- 상태 ----

    private void EnterState(State next)
    {
        state = next;
        stateStartTime = Time.time;
        arrived = false;
        patrolMoving = false;
        nextPatrolTime = Time.time + Random.Range(patrolPauseMin, patrolPauseMax);
        // 정찰 때만 늦춘 속도를 다른 상태에서는 원래대로 돌린다.
        if (agent.enabled) agent.speed = baseSpeed;
        if (next == State.Idle && agent.enabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }
    }

    // 목표를 발견해 추적을 시작한다. shout이면 근처 좀비를 부르는 소리를 낸다.
    private void BeginChase(bool shout)
    {
        bool wasChasing = state == State.Chase || state == State.Attack;
        EnterState(State.Chase);
        lastKnownPosition = target.position;
        lastSeenTime = Time.time;
        resumeTime = Time.time + alertPause;
        if (wasChasing) return;

        Alerted?.Invoke();
        if (shout) NoiseSystem.Emit(transform.position, shoutRadius, this);
    }

    private void TickIdle()
    {
        if (target != null)
        {
            Perceive();
            if (noticed) { BeginChase(true); return; }
        }

        if (BaseThreat.Patrolling) TickPatrol();
        else if (patrolMoving) StopPatrol();
    }

    // 태어난 자리 둘레의 NavMesh 위 지점을 골라 느릿하게 걸어가고, 닿으면 잠시 쉬었다가 다음 지점으로 간다.
    private void TickPatrol()
    {
        if (!agent.enabled || !agent.isOnNavMesh) return;

        if (patrolMoving)
        {
            bool arrivedAtPoint = !agent.pathPending && agent.remainingDistance <= 1f;
            // 길이 막혀 못 가면 일정 시간 뒤 포기한다.
            if (arrivedAtPoint || Time.time - patrolStartTime > 20f)
            {
                StopPatrol();
                nextPatrolTime = Time.time + Random.Range(patrolPauseMin, patrolPauseMax);
            }
            return;
        }

        if (Time.time < nextPatrolTime) return;

        Vector2 offset = Random.insideUnitCircle * patrolRadius;
        Vector3 candidate = home + new Vector3(offset.x, 0f, offset.y);
        if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, NavMesh.AllAreas))
        {
            nextPatrolTime = Time.time + 1f;
            return;
        }

        agent.speed = baseSpeed * patrolSpeedScale;
        agent.isStopped = false;
        agent.SetDestination(hit.position);
        patrolMoving = true;
        patrolStartTime = Time.time;
    }

    private void StopPatrol()
    {
        patrolMoving = false;
        agent.speed = baseSpeed;
        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }
    }

    private void TickInvestigate()
    {
        if (target == null) { EnterState(State.Idle); return; }
        Perceive();
        if (noticed) { BeginChase(true); return; }

        if (!arrived)
        {
            if (!agent.pathPending && agent.remainingDistance <= 1.5f)
            {
                arrived = true;
                arrivedTime = Time.time;
                agent.isStopped = true;
            }
            // 가는 길이 막혀 못 가면 일정 시간 뒤 포기한다.
            else if (Time.time - stateStartTime > 20f) EnterState(State.Idle);
            return;
        }

        ScanAround();
        if (Time.time - arrivedTime >= investigateScanTime) EnterState(State.Idle);
    }

    private void TickSearch()
    {
        if (target == null) { EnterState(State.Idle); return; }
        Perceive();
        if (noticed) { BeginChase(false); return; }

        if (!arrived)
        {
            agent.isStopped = false;
            agent.SetDestination(lastKnownPosition);
            if (!agent.pathPending && agent.remainingDistance <= 1.5f)
            {
                arrived = true;
                arrivedTime = Time.time;
                agent.isStopped = true;
            }
            else if (Time.time - stateStartTime > searchDuration * 3f) EnterState(State.Idle);
            return;
        }

        ScanAround();
        if (Time.time - arrivedTime >= searchDuration) EnterState(State.Idle);
    }

    // 제자리에서 좌우로 둘러본다.
    private void ScanAround()
    {
        if (Time.time >= nextScanFlip)
        {
            scanDirection = -scanDirection;
            nextScanFlip = Time.time + 2f;
        }
        transform.Rotate(0f, 55f * scanDirection * Time.deltaTime, 0f);
    }

    private void TickChase()
    {
        if (target == null) { EnterState(State.Idle); return; }

        Perceive();
        if (tracking)
        {
            lastKnownPosition = target.position;
            lastSeenTime = Time.time;
        }
        else if (Time.time - lastSeenTime > loseSightTime)
        {
            // 위치를 놓친 지 오래라 마지막으로 본 곳을 수색한다.
            ClearBlocker();
            EnterState(State.Search);
            return;
        }

        float distance = Vector3.Distance(transform.position, target.position);
        // 근접 좀비도 벽 너머로는 때리지 못한다(벽이 길을 막기 때문에 시야를 확인한다).
        if (distance <= attackRange && CanSee(target, rangedAttack != null))
        {
            agent.isStopped = true;
            state = State.Attack;
            return;
        }

        if (Time.time < resumeTime)
        {
            agent.isStopped = true;
            return;
        }

        if (breachBlockers && rangedAttack == null && TickBlockerApproach()) return;

        agent.isStopped = false;
        agent.SetDestination(lastKnownPosition);
        // 위치를 모르는 채 마지막 위치에 닿으면 바로 둘러보기(수색)로 넘어간다.
        if (!tracking && !agent.pathPending && agent.remainingDistance <= 1.5f)
        {
            EnterState(State.Search);
            arrived = true;
            arrivedTime = Time.time;
            agent.isStopped = true;
        }
    }

    // 목표까지 완전한 길이 없으면 막은 구조물(가까운 닫힌 문, 없으면 끊긴 지점 옆의 벽) 앞으로 가서 부수거나 문을 밀어 연다. 맡고 있으면 true.
    private bool TickBlockerApproach()
    {
        if (!blockerActive)
        {
            if (agent.pathPending || agent.pathStatus == NavMeshPathStatus.PathComplete) return false;
            if (Time.time < nextBlockerSearch) return false;
            nextBlockerSearch = Time.time + 0.5f;
            if (!PickBlocker()) return false;
            doorPush = 0f;
        }

        // 구조물이 사라졌거나 문이 열렸거나 목표를 놓쳤으면 포기한다.
        bool alive = blockerDoor != null ? !blockerDoor.IsOpen : blocker != null;
        if (!alive || state != State.Chase || Vector3.Distance(transform.position, lastKnownPosition) > detectionRange * 1.5f)
        {
            ClearBlocker();
            return false;
        }

        bool breakIt = blocker != null && (blockerDoor == null || doorBehavior == DoorBehavior.Break);
        Vector3 standPoint;
        Vector3 aimPoint;
        float reachDistance;
        if (blockerDoor != null)
        {
            aimPoint = blockerDoor.transform.position;
            standPoint = DoorFront(blockerDoor);
            reachDistance = FlatDistance(transform.position, aimPoint);
        }
        else
        {
            Bounds bounds = blocker.WorldBounds;
            aimPoint = bounds.center;
            Vector3 closest = bounds.ClosestPoint(transform.position);
            standPoint = NavMesh.SamplePosition(closest, out NavMeshHit hit, 2.5f, NavMesh.AllAreas) ? hit.position : closest;
            reachDistance = FlatDistance(transform.position, closest);
        }

        if (reachDistance > doorReach)
        {
            // 밀려나 닿지 못하면 공격 자리를 비워 다른 좀비가 쓰게 한다.
            if (blocker != null) blocker.ReleaseAttacker(this);
            agent.isStopped = false;
            agent.SetDestination(standPoint);
            doorPush = 0f;
            return true;
        }

        agent.isStopped = true;
        Vector3 toAim = aimPoint - transform.position;
        toAim.y = 0f;
        if (toAim.sqrMagnitude > 0.01f)
        {
            Quaternion look = Quaternion.LookRotation(toAim);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, agent.angularSpeed * Time.deltaTime);
        }

        if (breakIt)
        {
            // 문/벽 앞에는 한꺼번에 몇 마리밖에 붙지 못한다. 자리가 없으면 뒤에서 기다린다.
            if (!blocker.TryClaimAttacker(this)) return true;
            if (Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + attackCooldown;
                BeginMeleeSwing(PendingKind.Structure, blocker);
            }
            return true;
        }

        doorPush += Time.deltaTime;
        if (doorPush >= doorOpenDelay)
        {
            blockerDoor.Toggle(transform.position);
            ClearBlocker();
            agent.isStopped = false;
        }
        return true;
    }

    private void ClearBlocker()
    {
        if (blocker != null) blocker.ReleaseAttacker(this);
        blockerActive = false;
        blocker = null;
        blockerDoor = null;
        doorPush = 0f;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    // 목표에 가장 가까운 닫힌 문을, 없으면 길이 끊긴 지점 옆의 가장 가까운 부술 수 있는 벽을 고른다.
    private bool PickBlocker()
    {
        BuildableDoor door = FindClosedDoor();
        if (door != null)
        {
            blockerDoor = door;
            blocker = door.GetComponent<BuildableHealth>();
            blockerActive = true;
            return true;
        }

        Vector3 end = agent.pathEndPosition;
        BuildableHealth nearest = null;
        float best = wallSearchRadius;
        foreach (BuildableHealth candidate in BuildableHealth.Active)
        {
            if (candidate == null || !candidate.HasBounds || candidate.IsBroken) continue;
            float distance = Mathf.Sqrt(candidate.WorldBounds.SqrDistance(end));
            if (distance >= best) continue;
            best = distance;
            nearest = candidate;
        }
        if (nearest == null) return false;

        blocker = nearest;
        blockerDoor = null;
        blockerActive = true;
        return true;
    }

    // 목표에 가장 가까운 닫힌 문(미리보기 제외)을 찾는다.
    private BuildableDoor FindClosedDoor()
    {
        BuildableDoor best = null;
        float bestDistance = doorSearchRange;
        foreach (BuildableDoor door in BuildableDoor.Active)
        {
            if (door == null || door.IsOpen) continue;
            BuildableInstance placed = door.GetComponent<BuildableInstance>();
            if (placed == null || string.IsNullOrEmpty(placed.WorldId)) continue;

            float distance = Vector3.Distance(door.transform.position, lastKnownPosition);
            if (distance >= bestDistance) continue;
            bestDistance = distance;
            best = door;
        }
        return best;
    }

    // 좀비가 서 있는 쪽 문 앞 지점(NavMesh 위)이다.
    private Vector3 DoorFront(BuildableDoor door)
    {
        Vector3 toZombie = transform.position - door.transform.position;
        float side = Vector3.Dot(door.transform.forward, toZombie) >= 0f ? 1f : -1f;
        Vector3 front = door.transform.position + door.transform.forward * side * 1.4f;
        return NavMesh.SamplePosition(front, out NavMeshHit hit, 2f, NavMesh.AllAreas) ? hit.position : front;
    }

    private void TickAttack()
    {
        if (target == null) { EnterState(State.Idle); return; }

        float distance = Vector3.Distance(transform.position, target.position);
        // 사거리 안이어도 목표가 벽 등에 가려지면 다시 접근한다.
        if (distance > attackRange || !CanSee(target, false))
        {
            agent.isStopped = false;
            state = State.Chase;
            return;
        }

        FaceTarget();
        if (rangedAttack != null) rangedAttack.Reposition(target);

        if (Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackCooldown;
            if (rangedAttack != null)
            {
                LastAttackVariant = 0;
                Attacked?.Invoke();
                rangedAttack.Fire(target, attackDamage);
                return;
            }

            BeginMeleeSwing(PendingKind.Target, null);
        }
    }

    // 눈에서 목표 몸통까지 시야가 트였는지 본다. 시야각 확인은 선택이고, 다른 좀비와 자기 자신은 가리지 않는다.
    public bool CanSee(Transform seen, bool checkAngle)
    {
        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        Vector3 aim = seen.position + Vector3.up * 1.2f;
        Vector3 toTarget = aim - eye;

        if (checkAngle)
        {
            Vector3 flat = new Vector3(toTarget.x, 0f, toTarget.z);
            if (flat.sqrMagnitude > 0.01f && Vector3.Angle(transform.forward, flat) > sightHalfAngle) return false;
        }

        RaycastHit[] hits = Physics.RaycastAll(eye, toTarget.normalized, toTarget.magnitude, AvatarBody.ExcludeMask, QueryTriggerInteraction.Ignore);
        float nearest = float.MaxValue;
        bool blocked = false;
        foreach (RaycastHit hit in hits)
        {
            if (hit.distance >= nearest) continue;
            Transform hitTransform = hit.collider.transform;
            if (hitTransform.IsChildOf(transform) || hitTransform.IsChildOf(seen) || hit.collider.GetComponentInParent<ZombieAI>() != null) continue;
            nearest = hit.distance;
            blocked = true;
        }
        return !blocked;
    }

    // 멈춰서 공격하는 동안 목표 쪽으로 몸을 돌린다(원거리 공격이 엉뚱한 쪽으로 나가지 않게).
    private void FaceTarget()
    {
        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.01f) return;

        Quaternion look = Quaternion.LookRotation(toTarget);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, look, agent.angularSpeed * Time.deltaTime);
    }

    private void HandleDied()
    {
        state = State.Dead;
        pendingKind = PendingKind.None;
        pendingStructure = null;
        ClearBlocker();
        // NavMesh 밖이거나 이미 꺼진 에이전트에는 멈춤 요청이 오류가 되므로 확인한 뒤 멈춘다.
        if (agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
        agent.enabled = false;
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }
}
