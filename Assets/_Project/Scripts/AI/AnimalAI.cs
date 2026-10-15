// 사냥감 동물의 이동·애니메이션·피격 도주·사망 시 고기 드랍을 맡는 권위 컴포넌트다. 상태 전이 규칙은 AnimalBrain에 있다.
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(NavMeshAgent))]
public class AnimalAI : MonoBehaviour, IAnimalVoiceSource, IAnimalNetworkSource
{
    [SerializeField] private AnimalDefinition definition;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject lootPickupPrefab;
    [SerializeField] private float animationBlendSeconds = 0.15f;

    private Health health;
    private NavMeshAgent agent;
    private AnimalBrain brain;
    private Transform player;
    private float nextTargetPick;
    private string currentAnimation;
    private float nextRepathTime;

    public AnimalDefinition Definition => definition;
    public PreyState State => puppet ? remoteState : brain != null ? brain.State : PreyState.Idle;
    public bool IsDead => brain != null && brain.IsDead;

    // 놀라 도망치기 시작할 때 알린다(소리가 구독한다). 초식 동물은 공격하지 않으므로 Attacked는 쓰지 않는다.
    public event System.Action Alarmed;
    public event System.Action Attacked { add { } remove { } }
    public event System.Action<string, bool> AnimationPlayed;

    // 멀티플레이 참가자 쪽 동물은 AI를 돌리지 않는 꼭두각시다. 상태와 사건은 서버에서 받아 모습과 소리에만 쓴다.
    private bool puppet;
    private PreyState remoteState;

    public bool IsPuppet => puppet;
    public int StateValue => (int)State;

    public void BecomePuppet()
    {
        puppet = true;
        if (agent.enabled) agent.enabled = false;
    }

    public void RemoteState(int state) => remoteState = (PreyState)state;
    public void RemoteAlarmed() => Alarmed?.Invoke();
    public void RemoteAttacked() { }

    // 서버가 재생한 애니메이션을 따라 재생한다. 이미 죽은 꼭두각시는 사망 처리가 낸 쓰러짐을 다시 시작하지 않는다.
    public void RemoteAnimation(string stateName, bool force)
    {
        if (brain.IsDead) return;
        PlayAnimation(stateName, force);
    }

    // 탈것으로 쓰이는 동안은 AI가 이동하지 않고, 발소리용 동작은 탑승 쪽(Mountable)이 알려 준다.
    public bool IsRidden { get; private set; }
    private AnimalMotion riddenMotion;

    public AnimalMotion Motion
    {
        get
        {
            if (IsRidden) return riddenMotion;
            switch (State)
            {
                case PreyState.Wander: return AnimalMotion.Walk;
                case PreyState.Flee: return AnimalMotion.Run;
                default: return AnimalMotion.None;
            }
        }
    }

    public bool CanCallIdly => !IsRidden && (State == PreyState.Idle || State == PreyState.Wander);

    private void Awake()
    {
        health = GetComponent<Health>();
        agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        // 이동은 NavMeshAgent가 하므로 애니메이션의 루트 모션은 쓰지 않는다.
        if (animator != null) animator.applyRootMotion = false;

        brain = definition.CreateBrain();
        brain.StateChanged += HandleStateChanged;
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

    // 탑승이 시작되면 이동을 멈추고 NavMeshAgent를 끈다. 끝나면 가장 가까운 NavMesh 위로 옮겨 대기 상태로 돌아간다.
    public void SetRidden(bool ridden)
    {
        if (IsRidden == ridden || brain.IsDead) return;
        IsRidden = ridden;
        riddenMotion = AnimalMotion.None;

        if (ridden)
        {
            if (agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
            agent.enabled = false;
            return;
        }

        agent.enabled = true;
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 4f, NavMesh.AllAreas)) agent.Warp(hit.position);
        currentAnimation = null;
        EnterIdle();
    }

    public void SetRiddenMotion(AnimalMotion motion) => riddenMotion = motion;

    private void Update()
    {
        if (puppet || brain.IsDead || IsRidden) return;

        brain.Tick(Time.deltaTime, DistanceToPlayer());
        if (!agent.enabled || !agent.isOnNavMesh) return;

        switch (brain.State)
        {
            case PreyState.Wander:
                if (HasArrived()) brain.NotifyArrived();
                break;
            case PreyState.Flee:
                // 도주 목표에 닿았는데 아직 위협이 가까우면 더 멀리 달린다.
                if (HasArrived() && Time.time >= nextRepathTime) PickFleeTarget();
                break;
        }
    }

    private float DistanceToPlayer()
    {
        Transform target = Player;
        if (target == null) return float.MaxValue;
        Vector3 offset = target.position - transform.position;
        offset.y = 0f;
        return offset.magnitude;
    }

    // 위협이 되는 플레이어는 가장 가까운 살아 있는 플레이어다(멀티플레이에서는 호스트 플레이어와 참가자 아바타 중에서 고른다).
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

    private bool HasArrived()
    {
        return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f;
    }

    private void HandleStateChanged(PreyState state)
    {
        if (puppet) return;
        if (state == PreyState.Flee) Alarmed?.Invoke();
        if (brain.IsDead || !agent.enabled || !agent.isOnNavMesh) return;

        switch (state)
        {
            case PreyState.Idle:
                EnterIdle();
                break;
            case PreyState.Wander:
                agent.speed = definition.WalkSpeed;
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
            case PreyState.Flee:
                agent.speed = definition.RunSpeed;
                agent.isStopped = false;
                PickFleeTarget();
                PlayAnimation(definition.RunState);
                break;
        }
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

    // 플레이어에게서 멀어지는 방향으로 도주 목표를 고른다. 막혀 있으면 좌우로 각도를 벌려 가며 찾는다.
    private void PickFleeTarget()
    {
        nextRepathTime = Time.time + 0.5f;

        Vector3 away = transform.forward;
        if (Player != null)
        {
            Vector3 fromPlayer = transform.position - Player.position;
            fromPlayer.y = 0f;
            if (fromPlayer.sqrMagnitude > 0.01f) away = fromPlayer.normalized;
        }

        float[] angles = { 0f, 35f, -35f, 70f, -70f, 110f, -110f };
        foreach (float angle in angles)
        {
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * away;
            Vector3 candidate = transform.position + direction * definition.FleeDistance;
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, NavMesh.AllAreas)) continue;
            if (agent.SetDestination(hit.position)) return;
        }
    }

    private void HandleDamaged(float amount, float remaining)
    {
        if (puppet || brain.IsDead || remaining <= 0f) return;

        PreyState before = brain.State;
        brain.NotifyDamaged();
        // 이미 도주 중이면 상태 변화가 없으므로 맞을 때마다 도주 방향을 다시 잡는다.
        if (before == PreyState.Flee && agent.enabled && agent.isOnNavMesh) PickFleeTarget();
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
