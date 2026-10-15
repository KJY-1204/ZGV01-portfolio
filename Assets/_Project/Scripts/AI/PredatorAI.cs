// 육식 맹수의 감지·추격·공격 이동과 애니메이션, 공격 피해, 사망 시 고기 드랍을 맡는 권위 컴포넌트다. 상태 전이 규칙은 PredatorBrain에 있다.
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(NavMeshAgent))]
public class PredatorAI : MonoBehaviour, IAnimalVoiceSource, IAnimalNetworkSource
{
    [SerializeField] private PredatorDefinition definition;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject lootPickupPrefab;
    [SerializeField] private float animationBlendSeconds = 0.15f;
    // 공격이 들어가는 순간에 목표가 사거리의 이 배율 안에 있으면 맞은 것으로 본다(공격하는 동안 조금 움직일 수 있으므로 여유를 둔다).
    [SerializeField] private float hitRangeTolerance = 1.4f;
    [SerializeField] private float turnSpeed = 360f;

    private Health health;
    private NavMeshAgent agent;
    private PredatorBrain brain;
    private Transform player;
    private float nextTargetPick;
    private string currentAnimation;
    private float nextRepathTime;
    private float roarUntil;
    private PredatorState previousState = PredatorState.Idle;

    public PredatorDefinition Definition => definition;
    public PredatorState State => puppet ? remoteState : brain != null ? brain.State : PredatorState.Idle;
    public bool IsDead => brain != null && brain.IsDead;

    public event System.Action Alarmed;
    public event System.Action Attacked;
    public event System.Action<string, bool> AnimationPlayed;

    // 멀티플레이 참가자 쪽 맹수는 AI를 돌리지 않는 꼭두각시다. 상태와 사건은 서버에서 받아 모습과 소리에만 쓴다.
    private bool puppet;
    private PredatorState remoteState;

    public bool IsPuppet => puppet;
    public int StateValue => (int)State;

    public void BecomePuppet()
    {
        puppet = true;
        if (agent.enabled) agent.enabled = false;
    }

    public void RemoteState(int state) => remoteState = (PredatorState)state;

    // 처음 발견했을 때의 포효 시간을 서버와 같게 잡아 포효하는 동안은 발소리가 나지 않게 한다.
    public void RemoteAlarmed()
    {
        Alarmed?.Invoke();
        if (definition.RoarSeconds > 0f && !string.IsNullOrEmpty(definition.RoarState)) roarUntil = Time.time + definition.RoarSeconds;
    }

    public void RemoteAttacked() => Attacked?.Invoke();

    // 서버가 재생한 애니메이션을 따라 재생한다. 이미 죽은 꼭두각시는 사망 처리가 낸 쓰러짐을 다시 시작하지 않는다.
    public void RemoteAnimation(string stateName, bool force)
    {
        if (brain.IsDead) return;
        PlayAnimation(stateName, force);
    }

    public AnimalMotion Motion
    {
        get
        {
            switch (State)
            {
                case PredatorState.Wander: return AnimalMotion.Walk;
                case PredatorState.Chase: return Time.time < roarUntil ? AnimalMotion.None : AnimalMotion.Run;
                default: return AnimalMotion.None;
            }
        }
    }

    public bool CanCallIdly => State == PredatorState.Idle || State == PredatorState.Wander;

    private void Awake()
    {
        health = GetComponent<Health>();
        agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        // 이동은 NavMeshAgent가 하므로 애니메이션의 루트 모션은 쓰지 않는다.
        if (animator != null) animator.applyRootMotion = false;

        brain = definition.CreatePredatorBrain();
        brain.StateChanged += HandleStateChanged;
        brain.AttackLanded += HandleAttackLanded;
        health.Damaged += HandleDamaged;
        health.Died += HandleDied;
    }

    private void OnDestroy()
    {
        if (health == null) return;
        health.Damaged -= HandleDamaged;
        health.Died -= HandleDied;
    }

    private void Start()
    {
        if (!puppet) EnterIdle();
    }

    private void Update()
    {
        if (puppet || brain.IsDead) return;

        brain.Tick(Time.deltaTime, DistanceToTarget());
        if (!agent.enabled || !agent.isOnNavMesh) return;

        switch (brain.State)
        {
            case PredatorState.Wander:
                if (HasArrived()) brain.NotifyArrived();
                break;

            case PredatorState.Chase:
                UpdateChase();
                break;

            case PredatorState.Attack:
                FaceTarget();
                break;
        }
    }

    // 목표는 가장 가까운 살아 있는 플레이어다(멀티플레이에서는 호스트 플레이어와 참가자 아바타 중에서 고른다). 죽은 플레이어는 목표 후보가 아니라서 추격을 멈추고 포기한다.
    private Transform Player
    {
        get
        {
            if (player == null || Time.time >= nextTargetPick)
            {
                nextTargetPick = Time.time + 0.4f;
                player = PlayerTargets.Nearest(transform.position);
            }
            return player;
        }
    }

    private float DistanceToTarget()
    {
        Transform target = Player;
        if (target == null) return float.MaxValue;

        Vector3 offset = target.position - transform.position;
        offset.y = 0f;
        return offset.magnitude;
    }

    private bool HasArrived()
    {
        return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f;
    }

    private void UpdateChase()
    {
        // 포효하는 동안은 제자리에서 목표를 바라본다.
        if (Time.time < roarUntil)
        {
            FaceTarget();
            return;
        }

        if (agent.isStopped)
        {
            agent.isStopped = false;
            PlayAnimation(definition.RunState);
        }

        if (Time.time < nextRepathTime || Player == null) return;
        nextRepathTime = Time.time + 0.2f;
        agent.SetDestination(Player.position);
    }

    private void FaceTarget()
    {
        if (Player == null) return;
        Vector3 toTarget = Player.position - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.01f) return;

        Quaternion look = Quaternion.LookRotation(toTarget);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * Time.deltaTime);
    }

    private void HandleStateChanged(PredatorState state)
    {
        if (puppet) return;
        PredatorState before = previousState;
        previousState = state;
        if (brain.IsDead || !agent.enabled || !agent.isOnNavMesh) return;

        switch (state)
        {
            case PredatorState.Idle:
                EnterIdle();
                break;

            case PredatorState.Wander:
                agent.speed = definition.WalkSpeed;
                agent.stoppingDistance = 0.3f;
                agent.isStopped = false;
                if (TryPickWanderTarget(out Vector3 destination))
                {
                    agent.SetDestination(destination);
                    PlayAnimation(definition.WalkState);
                }
                else
                {
                    brain.NotifyArrived();
                }
                break;

            case PredatorState.Chase:
                agent.speed = definition.RunSpeed;
                agent.stoppingDistance = definition.AttackRange * 0.7f;
                BeginChase(before);
                break;

            case PredatorState.Attack:
                agent.ResetPath();
                agent.isStopped = true;
                PlayAnimation(definition.PickAttackState(), true);
                Attacked?.Invoke();
                break;
        }
    }

    // 대기/배회 중에 처음 발견하면 잠깐 포효하고(제자리에서 목표를 보며) 달려든다. 공격 뒤 추격으로 돌아올 때는 포효 없이 바로 달린다.
    private void BeginChase(PredatorState before)
    {
        bool firstSighting = before == PredatorState.Idle || before == PredatorState.Wander;
        if (firstSighting)
        {
            Alarmed?.Invoke();
            if (definition.RoarSeconds > 0f && !string.IsNullOrEmpty(definition.RoarState))
            {
                roarUntil = Time.time + definition.RoarSeconds;
                agent.ResetPath();
                agent.isStopped = true;
                PlayAnimation(definition.RoarState, true);
                return;
            }
        }

        roarUntil = 0f;
        agent.isStopped = false;
        nextRepathTime = 0f;
        PlayAnimation(definition.RunState);
    }

    private void EnterIdle()
    {
        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }
        bool graze = definition.EatChance > 0f && !string.IsNullOrEmpty(definition.EatState) && Random.value < definition.EatChance;
        PlayAnimation(graze ? definition.EatState : definition.IdleState);
    }

    private bool TryPickWanderTarget(out Vector3 destination)
    {
        for (int i = 0; i < 8; i++)
        {
            Vector2 offset = Random.insideUnitCircle * definition.WanderRadius;
            Vector3 candidate = transform.position + new Vector3(offset.x, 0f, offset.y);
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                destination = hit.position;
                return true;
            }
        }
        destination = transform.position;
        return false;
    }

    // 공격 동작의 타격 순간이다. 목표가 사거리(여유 포함) 안이면 피해를 준다.
    private void HandleAttackLanded()
    {
        Transform target = Player;
        if (target == null) return;

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        if (toTarget.magnitude > definition.AttackRange * hitRangeTolerance) return;

        // 로컬 플레이어는 Health에 바로, 참가자 아바타는 소유자에게 전달한다.
        Health targetHealth = target.GetComponentInParent<Health>();
        if (targetHealth != null) targetHealth.ApplyDamage(definition.AttackDamage, toTarget.normalized, false);
        else PlayerTargets.Damage(target, definition.AttackDamage, toTarget.normalized);
    }

    private void HandleDamaged(float amount, float remaining)
    {
        if (puppet || brain.IsDead || remaining <= 0f) return;
        brain.NotifyDamaged();
    }

    private void HandleDied()
    {
        brain.NotifyDead();
        if (agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
        agent.enabled = false;

        foreach (Collider collider in GetComponents<Collider>()) collider.enabled = false;
        PlayAnimation(definition.DieState, true);
        // 고기와 시체 치우기는 서버(또는 싱글플레이)만 한다. 참가자의 꼭두각시는 쓰러지는 모습만 낸다.
        if (puppet) return;
        AnimalLoot.Drop(definition, lootPickupPrefab, transform);
        Destroy(gameObject, definition.CorpseSeconds);
    }

    private void PlayAnimation(string stateName, bool force = false)
    {
        if (animator == null || string.IsNullOrEmpty(stateName)) return;
        if (!force && currentAnimation == stateName) return;

        currentAnimation = stateName;
        animator.CrossFadeInFixedTime(stateName, animationBlendSeconds);
        AnimationPlayed?.Invoke(stateName, force);
    }
}
