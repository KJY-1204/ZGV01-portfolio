// 육식 맹수의 행동 전이(대기, 배회, 추격, 공격, 사망)와 공격 타격 시점을 정하는 순수 규칙이다. 이동, 애니메이션, 피해 적용은 모르고 시간과 목표 거리만 입력으로 받는다.
using System;

public enum PredatorState
{
    Idle,
    Wander,
    Chase,
    Attack,
    Dead,
}

public class PredatorBrain
{
    private readonly float detectDistance;
    private readonly float loseDistance;
    private readonly float loseSeconds;
    private readonly float attackRange;
    private readonly float attackCooldown;
    private readonly float attackDuration;
    private readonly float attackHitDelay;
    private readonly float idleMin, idleMax;
    private readonly float wanderMin, wanderMax;
    private readonly Func<float, float, float> random;

    private float stateTime;
    private float stateDuration;
    private float cooldown;
    private float loseTimer;
    private bool landed;

    public PredatorState State { get; private set; }
    public bool IsDead => State == PredatorState.Dead;

    // 상태가 바뀔 때마다 새 상태를 알린다(이동 목표, 애니메이션 전환에 쓴다).
    public event Action<PredatorState> StateChanged;
    // 공격이 시작되고 타격 지연이 지난 순간 공격마다 한 번 알린다. 실제 피해는 받는 쪽이 사거리를 다시 확인해 준다.
    public event Action AttackLanded;

    // random은 (최소, 최대) 사이 값을 돌려준다. attackCooldown은 공격이 시작된 시점부터 센다.
    public PredatorBrain(float detectDistance, float loseDistance, float loseSeconds,
        float attackRange, float attackCooldown, float attackDuration, float attackHitDelay,
        float idleMin, float idleMax, float wanderMin, float wanderMax, Func<float, float, float> random)
    {
        this.detectDistance = Math.Max(0f, detectDistance);
        this.loseDistance = Math.Max(this.detectDistance, loseDistance);
        this.loseSeconds = Math.Max(0f, loseSeconds);
        this.attackRange = Math.Max(0f, attackRange);
        this.attackCooldown = Math.Max(0f, attackCooldown);
        this.attackDuration = Math.Max(0.01f, attackDuration);
        this.attackHitDelay = Math.Min(Math.Max(0f, attackHitDelay), this.attackDuration);
        this.idleMin = idleMin;
        this.idleMax = idleMax;
        this.wanderMin = wanderMin;
        this.wanderMax = wanderMax;
        this.random = random ?? ((min, max) => (min + max) * 0.5f);
        EnterState(PredatorState.Idle, false);
    }

    // dt초가 지났고 목표(플레이어)까지 targetDistance(m)일 때 상태를 갱신한다.
    public void Tick(float dt, float targetDistance)
    {
        if (IsDead) return;

        stateTime += dt;
        cooldown = Math.Max(0f, cooldown - dt);

        switch (State)
        {
            case PredatorState.Idle:
                if (targetDistance <= detectDistance) StartChase();
                else if (stateTime >= stateDuration) EnterState(PredatorState.Wander);
                break;

            case PredatorState.Wander:
                if (targetDistance <= detectDistance) StartChase();
                else if (stateTime >= stateDuration) EnterState(PredatorState.Idle);
                break;

            case PredatorState.Chase:
                if (targetDistance <= attackRange && cooldown <= 0f)
                {
                    StartAttack();
                }
                else if (targetDistance > loseDistance)
                {
                    // 멀리 떨어진 채로 일정 시간이 지나면 포기한다. 잠깐 멀어졌다 돌아오면 다시 센다.
                    loseTimer += dt;
                    if (loseTimer >= loseSeconds) EnterState(PredatorState.Idle);
                }
                else
                {
                    loseTimer = 0f;
                }
                break;

            case PredatorState.Attack:
                if (!landed && stateTime >= attackHitDelay)
                {
                    landed = true;
                    AttackLanded?.Invoke();
                }
                if (stateTime >= attackDuration) EnterState(PredatorState.Chase);
                break;
        }
    }

    // 맞으면 목표가 감지 거리 밖이어도 곧바로 추격한다.
    public void NotifyDamaged()
    {
        if (State == PredatorState.Idle || State == PredatorState.Wander) StartChase();
    }

    // 배회 목표에 도착하면 바로 대기로 돌아간다.
    public void NotifyArrived()
    {
        if (State == PredatorState.Wander) EnterState(PredatorState.Idle);
    }

    public void NotifyDead()
    {
        if (IsDead) return;
        EnterState(PredatorState.Dead);
    }

    private void StartChase()
    {
        loseTimer = 0f;
        EnterState(PredatorState.Chase);
    }

    private void StartAttack()
    {
        cooldown = attackCooldown;
        landed = false;
        EnterState(PredatorState.Attack);
    }

    private void EnterState(PredatorState next, bool notify = true)
    {
        State = next;
        stateTime = 0f;
        switch (next)
        {
            case PredatorState.Idle: stateDuration = random(idleMin, idleMax); break;
            case PredatorState.Wander: stateDuration = random(wanderMin, wanderMax); break;
            default: stateDuration = float.MaxValue; break;
        }
        if (notify) StateChanged?.Invoke(next);
    }
}
