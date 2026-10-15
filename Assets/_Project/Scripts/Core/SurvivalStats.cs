// 플레이어의 허기/갈증을 시간에 따라 줄이고, 바닥나면 체력을 깎고, 소비 아이템으로 회복하는 도메인 컴포넌트다. UI는 이 컴포넌트의 이벤트만 구독한다.
using System;
using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Inventory))]
public class SurvivalStats : MonoBehaviour
{
    [SerializeField] private SurvivalStatsConfig config;

    private Health health;
    private Inventory inventory;
    private float penaltyTimer;

    public SurvivalMeter Hunger { get; private set; }
    public SurvivalMeter Thirst { get; private set; }
    public SurvivalStatsConfig Config => config;

    // 허기/갈증 감소 속도 배율이다. 달리기처럼 활동량이 큰 동안 어댑터가 설정한다(기본 1).
    public float DrainMultiplier { get; set; } = 1f;

    // 허기나 갈증이 경고 비율 이하면 허약 상태다. 점프/달리기 불가와 공격 약화의 기준이다.
    public bool IsWeakened { get; private set; }
    public float DamageMultiplier => IsWeakened && config != null ? config.WeakenedDamageMultiplier : 1f;
    public float AttackSpeedMultiplier => IsWeakened && config != null ? config.WeakenedAttackSpeedMultiplier : 1f;

    // 수치가 바뀔 때마다 알린다(HUD 갱신용).
    public event Action Changed;
    public event Action<bool> WeakenedChanged;
    public event Action<ConsumableDefinition> Consumed;

    private void Awake()
    {
        health = GetComponent<Health>();
        inventory = GetComponent<Inventory>();
        Hunger = new SurvivalMeter(config != null ? config.HungerMax : 100f);
        Thirst = new SurvivalMeter(config != null ? config.ThirstMax : 100f);
    }

    private void Update()
    {
        Tick(Time.deltaTime);
    }

    // 시간 경과 dt초만큼 수치를 줄이고 불이익을 적용한다. Update가 호출하며, 테스트에서는 시간을 직접 넣는다.
    public void Tick(float dt)
    {
        if (config == null || health.IsDead) return;

        float rate = Mathf.Max(0f, DrainMultiplier) * dt;
        Hunger.Drain(config.HungerDrainPerSecond * rate);
        Thirst.Drain(config.ThirstDrainPerSecond * rate);
        ApplyEmptyPenalty(dt);
        UpdateWeakened();
        Changed?.Invoke();
    }

    // 부활할 때 허기/갈증을 최대치의 fraction으로 맞추고 굶주림 피해 타이머와 허약 상태를 초기화한다.
    public void Revive(float fraction)
    {
        float ratio = Mathf.Clamp01(fraction);
        Hunger.Set(Hunger.Max * ratio);
        Thirst.Set(Thirst.Max * ratio);
        penaltyTimer = 0f;
        UpdateWeakened();
        Changed?.Invoke();
    }

    private void UpdateWeakened()
    {
        bool weakened = config != null
            && (Hunger.Ratio <= config.LowThreshold || Thirst.Ratio <= config.LowThreshold);
        if (weakened == IsWeakened) return;

        IsWeakened = weakened;
        WeakenedChanged?.Invoke(weakened);
    }

    // 바닥난 수치 하나당 초당 피해를 모았다가 일정 간격마다 한꺼번에 준다.
    private void ApplyEmptyPenalty(float dt)
    {
        int emptyCount = (Hunger.IsEmpty ? 1 : 0) + (Thirst.IsEmpty ? 1 : 0);
        if (emptyCount == 0)
        {
            penaltyTimer = 0f;
            return;
        }

        penaltyTimer += dt;
        if (penaltyTimer < config.DamageInterval) return;

        penaltyTimer -= config.DamageInterval;
        // 굶주림 피해는 방어구가 막아 주지 못한다.
        health.ApplyTrueDamage(config.EmptyDamagePerSecond * config.DamageInterval * emptyCount);
    }

    // 인벤토리에서 한 개를 소비해 효과를 적용한다. 죽었거나 아이템이 없으면 실패한다.
    public bool TryConsume(ConsumableDefinition item)
    {
        if (item == null || health.IsDead) return false;
        if (!inventory.Remove(item, 1)) return false;

        ApplyConsumable(item);
        return true;
    }

    // 소비 효과만 적용한다(아이템은 이미 다른 곳에서 차감됐다). 멀티플레이 참가자는 서버가 차감을 확정한 뒤 이것만 부른다.
    public void ApplyConsumable(ConsumableDefinition item)
    {
        if (item == null || health.IsDead) return;

        Hunger.Restore(item.HungerRestore);
        Thirst.Restore(item.ThirstRestore);
        // 음수면 오염된 음식처럼 체력을 깎는다.
        if (item.HealthRestore < 0f) health.ApplyTrueDamage(-item.HealthRestore);
        else health.Heal(item.HealthRestore);

        UpdateWeakened();
        Consumed?.Invoke(item);
        Changed?.Invoke();
    }

    // 저장 불러오기용이다. 값은 최대값 안으로 보정된다.
    public void Restore(float hunger, float thirst)
    {
        Hunger.Set(hunger);
        Thirst.Set(thirst);
        penaltyTimer = 0f;
        UpdateWeakened();
        Changed?.Invoke();
    }
}
