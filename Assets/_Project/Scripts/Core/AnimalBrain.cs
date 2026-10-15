// 사냥감 동물의 행동 전이(대기, 배회, 도주, 사망)를 정하는 순수 규칙이다. 이동과 애니메이션은 모르고 시간과 위협 거리만 입력으로 받는다.
using System;

public enum PreyState
{
    Idle,
    Wander,
    Flee,
    Dead,
}

public class AnimalBrain
{
    private readonly float alertDistance;
    private readonly float fleeSeconds;
    private readonly float idleMin, idleMax;
    private readonly float wanderMin, wanderMax;
    private readonly Func<float, float, float> random;

    private float stateTime;
    private float stateDuration;
    private float fleeTimer;

    public PreyState State { get; private set; }
    public bool IsDead => State == PreyState.Dead;

    // 상태가 바뀔 때마다 새 상태를 알린다(이동 목표 고르기, 애니메이션 전환에 쓴다).
    public event Action<PreyState> StateChanged;

    // alertDistance가 0이면 위협이 다가와도 놀라지 않고 피해를 입었을 때만 도주한다. random은 (최소, 최대) 사이 값을 돌려준다.
    public AnimalBrain(float alertDistance, float fleeSeconds, float idleMin, float idleMax, float wanderMin, float wanderMax, Func<float, float, float> random)
    {
        this.alertDistance = Math.Max(0f, alertDistance);
        this.fleeSeconds = Math.Max(0f, fleeSeconds);
        this.idleMin = idleMin;
        this.idleMax = idleMax;
        this.wanderMin = wanderMin;
        this.wanderMax = wanderMax;
        this.random = random ?? ((min, max) => (min + max) * 0.5f);
        EnterState(PreyState.Idle, false);
    }

    // dt초가 지났고 가장 가까운 위협(플레이어)까지 threatDistance(m)일 때 상태를 갱신한다.
    public void Tick(float dt, float threatDistance)
    {
        if (IsDead) return;

        stateTime += dt;
        bool threatened = alertDistance > 0f && threatDistance <= alertDistance;

        switch (State)
        {
            case PreyState.Idle:
                if (threatened) StartFleeing();
                else if (stateTime >= stateDuration) EnterState(PreyState.Wander);
                break;

            case PreyState.Wander:
                if (threatened) StartFleeing();
                else if (stateTime >= stateDuration) EnterState(PreyState.Idle);
                break;

            case PreyState.Flee:
                if (threatened) fleeTimer = 0f;
                else fleeTimer += dt;
                // 위협이 경계 거리 밖으로 충분히(1.25배) 벗어나고 도주 시간이 지나야 진정한다.
                if (fleeTimer >= fleeSeconds && threatDistance > alertDistance * 1.25f) EnterState(PreyState.Idle);
                break;
        }
    }

    // 피해를 입으면 위협이 보이지 않아도 도주한다. 이미 도주 중이면 도주 시간을 다시 센다.
    public void NotifyDamaged()
    {
        if (IsDead) return;
        fleeTimer = 0f;
        if (State != PreyState.Flee) StartFleeing();
    }

    // 배회 목표에 도착하면 바로 대기로 돌아간다.
    public void NotifyArrived()
    {
        if (State == PreyState.Wander) EnterState(PreyState.Idle);
    }

    public void NotifyDead()
    {
        if (IsDead) return;
        EnterState(PreyState.Dead);
    }

    private void StartFleeing()
    {
        fleeTimer = 0f;
        EnterState(PreyState.Flee);
    }

    private void EnterState(PreyState next, bool notify = true)
    {
        State = next;
        stateTime = 0f;
        switch (next)
        {
            case PreyState.Idle: stateDuration = random(idleMin, idleMax); break;
            case PreyState.Wander: stateDuration = random(wanderMin, wanderMax); break;
            default: stateDuration = float.MaxValue; break;
        }
        if (notify) StateChanged?.Invoke(next);
    }
}
