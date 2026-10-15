// 낮에 불타 죽는 좀비의 피해 계산 규칙(ZombieSunBurnRule)을 검증하는 EditMode 테스트다.
using NUnit.Framework;

public class ZombieSunBurnRuleTests
{
    [Test]
    public void Damage_SumsToMaxHealthOverBurnSeconds()
    {
        float total = 0f;
        for (int i = 0; i < 12; i++) total += ZombieSunBurnRule.Damage(150f, 6f, 0.5f);
        Assert.AreEqual(150f, total, 0.01f);
    }

    [Test]
    public void Damage_InvalidInputs_AreZero()
    {
        Assert.AreEqual(0f, ZombieSunBurnRule.Damage(0f, 6f, 0.5f));
        Assert.AreEqual(0f, ZombieSunBurnRule.Damage(100f, 6f, 0f));
    }

    [Test]
    public void Damage_TinyBurnSeconds_IsClamped()
    {
        Assert.AreEqual(1000f * 0.5f, ZombieSunBurnRule.Damage(100f, 0f, 0.5f), 0.01f);
    }

    [Test]
    public void IsLethal_OnlyWhenDamageReachesHealth()
    {
        Assert.IsTrue(ZombieSunBurnRule.IsLethal(10f, 10f));
        Assert.IsTrue(ZombieSunBurnRule.IsLethal(5f, 10f));
        Assert.IsFalse(ZombieSunBurnRule.IsLethal(11f, 10f));
        Assert.IsFalse(ZombieSunBurnRule.IsLethal(10f, 0f));
    }
}
