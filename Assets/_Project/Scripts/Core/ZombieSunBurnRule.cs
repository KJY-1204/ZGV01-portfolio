// 낮에 햇빛으로 불타 죽는 좀비의 피해 계산 규칙이다. 체력이 얼마든 정해진 시간 안에 죽도록 초당 피해를 구한다. 유니티 컴포넌트에 의존하지 않는다.
using System;

public static class ZombieSunBurnRule
{
    // 최대 체력을 burnSeconds에 걸쳐 선형으로 깎는 데 필요한, dt초 동안의 피해다.
    public static float Damage(float maxHealth, float burnSeconds, float dt)
    {
        if (maxHealth <= 0f || dt <= 0f) return 0f;
        return maxHealth / Math.Max(0.1f, burnSeconds) * dt;
    }

    // 이번 피해로 죽는지다(드랍을 막을지 정할 때 쓴다).
    public static bool IsLethal(float currentHealth, float damage)
    {
        return damage > 0f && damage >= currentHealth;
    }
}
