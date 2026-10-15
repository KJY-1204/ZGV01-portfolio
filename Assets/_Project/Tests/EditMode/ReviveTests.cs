// Health.Revive와 SurvivalStats.Revive(부활 규칙)를 검증하는 EditMode 테스트다.
using NUnit.Framework;
using UnityEngine;

public class ReviveTests
{
    private GameObject go;
    private Health health;

    [SetUp]
    public void SetUp()
    {
        go = new GameObject("p");
        health = go.AddComponent<Health>();
        TestUtil.Awake(health);
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(go);

    [Test]
    public void Revive_WhenDead_RestoresHealthAndRaisesEvent()
    {
        health.ApplyTrueDamage(health.MaxHealth);
        Assert.IsTrue(health.IsDead);

        int revived = 0;
        health.Revived += () => revived++;
        health.Revive(1f);

        Assert.IsFalse(health.IsDead);
        Assert.AreEqual(health.MaxHealth, health.CurrentHealth);
        Assert.AreEqual(1, revived);
    }

    [Test]
    public void Revive_WhenAlive_DoesNothing()
    {
        health.ApplyDamage(10f);
        float before = health.CurrentHealth;
        int revived = 0;
        health.Revived += () => revived++;

        health.Revive(1f);

        Assert.AreEqual(before, health.CurrentHealth);
        Assert.AreEqual(0, revived);
    }

    [Test]
    public void Revive_FractionIsClamped_AndNeverLeavesZeroHealth()
    {
        health.ApplyTrueDamage(health.MaxHealth);
        health.Revive(0f);
        Assert.IsFalse(health.IsDead);
        Assert.GreaterOrEqual(health.CurrentHealth, 1f);

        health.ApplyTrueDamage(health.MaxHealth);
        health.Revive(5f);
        Assert.AreEqual(health.MaxHealth, health.CurrentHealth);
    }

    [Test]
    public void Revive_PartialFraction_ScalesMaxHealth()
    {
        health.ApplyTrueDamage(health.MaxHealth);
        health.Revive(0.5f);
        Assert.AreEqual(health.MaxHealth * 0.5f, health.CurrentHealth, 0.01f);
    }

    [Test]
    public void Revived_CanTakeDamageAndDieAgain()
    {
        health.ApplyTrueDamage(health.MaxHealth);
        health.Revive(1f);
        health.ApplyTrueDamage(health.MaxHealth);
        Assert.IsTrue(health.IsDead);
    }
}
