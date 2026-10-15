// 체력 변경의 유일한 진입점이 되는 도메인 컴포넌트다. 외부에서 CurrentHealth를 직접 대입하지 않는다.
using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;

    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    // 받는 피해에서 깎는 비율(0~1)이다. 방어구가 정한다. ApplyDamage는 이 비율만큼 줄여 받고, ApplyTrueDamage는 무시한다.
    // 이 값이 너무 커서 피해가 사라지지 않게 상한을 둔다.
    public const float MaxDamageReduction = 0.9f;
    private float damageReduction;
    public float DamageReduction
    {
        get => damageReduction;
        set => damageReduction = Mathf.Clamp(value, 0f, MaxDamageReduction);
    }

    public event Action<float, float> Damaged;
    // 맞은 방향과 머리 피격 여부까지 아는 피해가 들어올 때(피해를 적용하기 전에) 알린다. 피격 반응이 구독한다.
    public event Action<float, Vector3, bool> HitReceived;
    // 가장 마지막으로 맞은 방향(공격하는 쪽에서 맞은 쪽으로)과 머리 피격 여부다. 사망 연출이 읽는다.
    public Vector3 LastHitDirection { get; private set; }
    public bool LastHitWasHeadshot { get; private set; }
    public event Action Died;
    // 죽었다가 되살아날 때 알린다(HUD가 사망 안내를 거둔다).
    public event Action Revived;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    // 맞은 방향과 머리 피격 여부를 함께 전달하는 피해다.
    public void ApplyDamage(float amount, Vector3 hitDirection, bool headshot)
    {
        if (IsDead || amount <= 0f) return;

        LastHitDirection = hitDirection;
        LastHitWasHeadshot = headshot;
        HitReceived?.Invoke(amount, hitDirection, headshot);
        ApplyDamage(amount);
    }

    // 전투 피해다. 방어구 감소율을 적용해서 받는다(Damaged 이벤트의 값도 실제로 받은 양이다).
    public void ApplyDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;
        ApplyEffectiveDamage(amount * (1f - damageReduction));
    }

    // 굶주림, 오염 같은 비전투 피해다. 방어구가 막아 주지 못한다.
    public void ApplyTrueDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;
        ApplyEffectiveDamage(amount);
    }

    private void ApplyEffectiveDamage(float amount)
    {
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        Damaged?.Invoke(amount, CurrentHealth);

        if (CurrentHealth <= 0f)
        {
            IsDead = true;
            Died?.Invoke();
        }
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
    }

    // 죽은 개체를 되살린다(살아 있으면 아무것도 하지 않는다). fraction은 최대 체력 대비 비율이고 최소 1은 남는다.
    public void Revive(float fraction = 1f)
    {
        if (!IsDead) return;
        IsDead = false;
        CurrentHealth = Mathf.Clamp(maxHealth * Mathf.Clamp01(fraction), 1f, maxHealth);
        Revived?.Invoke();
    }

    // 멀티플레이 참가자 쪽: 서버가 확정한 피해를 그대로 재현한다. 값은 서버가 준 현재 체력이고, 피격 방향이 있으면 피격 반응도 낸다. 방어구 감소율은 다시 적용하지 않는다.
    public void ApplyReplicatedDamage(float amount, float currentHealth, Vector3 hitDirection, bool headshot)
    {
        if (IsDead) return;

        if (hitDirection.sqrMagnitude > 0.0001f)
        {
            LastHitDirection = hitDirection;
            LastHitWasHeadshot = headshot;
            HitReceived?.Invoke(amount, hitDirection, headshot);
        }

        CurrentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        Damaged?.Invoke(amount, CurrentHealth);
        if (CurrentHealth <= 0f)
        {
            IsDead = true;
            Died?.Invoke();
        }
    }

    // 저장을 불러올 때 체력을 그 값으로 되돌린다. 피해/회복 이벤트를 내지 않아서 피격 반응(도주 등)이 일어나지 않는다.
    // 살아 있는 개체만 복원하므로 0 이하는 아주 작은 양수로 올린다(죽은 상태는 복원하지 않는다).
    public void RestoreState(float current)
    {
        if (IsDead) return;
        CurrentHealth = Mathf.Clamp(current, 0.01f, maxHealth);
    }
}
