// Health의 피해, 회복, 사망 규칙을 검증하는 EditMode 테스트다.
using NUnit.Framework;
using UnityEngine;

public class HealthTests
{
    private GameObject go;
    private Health health;

    [SetUp]
    public void SetUp()
    {
        go = new GameObject("h");
        health = go.AddComponent<Health>();
        TestUtil.Awake(health);
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(go);

    [Test]
    public void Awake_StartsAtMax()
    {
        Assert.AreEqual(100f, health.CurrentHealth);
        Assert.IsFalse(health.IsDead);
    }

    [Test]
    public void ApplyDamage_ReducesAndRaisesDamaged()
    {
        float amount = 0f, remaining = -1f;
        health.Damaged += (a, r) => { amount = a; remaining = r; };
        health.ApplyDamage(30f);
        Assert.AreEqual(70f, health.CurrentHealth);
        Assert.AreEqual(30f, amount);
        Assert.AreEqual(70f, remaining);
    }

    [Test]
    public void ApplyDamage_NonPositive_Ignored()
    {
        health.ApplyDamage(0f);
        health.ApplyDamage(-5f);
        Assert.AreEqual(100f, health.CurrentHealth);
    }

    [Test]
    public void ApplyDamage_Lethal_DiesOnceAtZero()
    {
        int died = 0;
        health.Died += () => died++;
        health.ApplyDamage(500f);
        health.ApplyDamage(10f);
        Assert.AreEqual(0f, health.CurrentHealth);
        Assert.IsTrue(health.IsDead);
        Assert.AreEqual(1, died);
    }

    [Test]
    public void ApplyDamage_WhenDead_DoesNothing()
    {
        health.ApplyDamage(100f);
        int damaged = 0;
        health.Damaged += (_, __) => damaged++;
        health.ApplyDamage(5f);
        Assert.AreEqual(0, damaged);
    }

    [Test]
    public void Heal_RestoresUpToMax()
    {
        health.ApplyDamage(60f);
        health.Heal(20f);
        Assert.AreEqual(60f, health.CurrentHealth);
        health.Heal(999f);
        Assert.AreEqual(100f, health.CurrentHealth);
    }

    [Test]
    public void Heal_WhenDeadOrNonPositive_DoesNothing()
    {
        health.ApplyDamage(40f);
        health.Heal(0f);
        health.Heal(-3f);
        Assert.AreEqual(60f, health.CurrentHealth);
        health.ApplyDamage(100f);
        health.Heal(50f);
        Assert.AreEqual(0f, health.CurrentHealth);
    }

    [Test]
    public void ApplyDamage_WithDirection_RecordsHitAndRaisesHitReceivedBeforeDamage()
    {
        float healthSeenInEvent = -1f;
        health.HitReceived += (_, __, ___) => healthSeenInEvent = health.CurrentHealth;
        health.ApplyDamage(25f, Vector3.left, true);
        Assert.AreEqual(100f, healthSeenInEvent); // 피해 적용 전에 알린다
        Assert.AreEqual(75f, health.CurrentHealth);
        Assert.AreEqual(Vector3.left, health.LastHitDirection);
        Assert.IsTrue(health.LastHitWasHeadshot);
    }
}

public class HealthRestoreStateTests
{
    private GameObject go;
    private Health health;

    [SetUp]
    public void SetUp()
    {
        go = new GameObject("h");
        health = go.AddComponent<Health>();
        TestUtil.Awake(health);
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(go);

    [Test]
    public void RestoreState_SetsCurrentHealth_WithoutEvents()
    {
        int damaged = 0, died = 0;
        health.Damaged += (_, __) => damaged++;
        health.HitReceived += (_, __, ___) => damaged++;
        health.Died += () => died++;
        health.RestoreState(37f);
        Assert.AreEqual(37f, health.CurrentHealth);
        Assert.AreEqual(0, damaged);
        Assert.AreEqual(0, died);
        Assert.IsFalse(health.IsDead);
    }

    [Test]
    public void RestoreState_ClampsToMax_AndKeepsAlive()
    {
        health.RestoreState(9999f);
        Assert.AreEqual(100f, health.CurrentHealth);
        health.RestoreState(-5f);
        Assert.Greater(health.CurrentHealth, 0f);   // 살아 있는 개체는 0 이하로 복원하지 않는다
        Assert.IsFalse(health.IsDead);
    }

    [Test]
    public void RestoreState_DoesNothing_WhenAlreadyDead()
    {
        health.ApplyDamage(500f);
        health.RestoreState(80f);
        Assert.AreEqual(0f, health.CurrentHealth);
        Assert.IsTrue(health.IsDead);
    }
}
