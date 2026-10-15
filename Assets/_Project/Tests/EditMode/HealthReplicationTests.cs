// 멀티플레이 참가자 쪽 Health가 서버가 알린 피해/사망을 그대로 재현하는지(방어구 감소율 미적용, 이벤트 순서) 검증하는 EditMode 테스트다.
using NUnit.Framework;
using UnityEngine;

public class HealthReplicationTests
{
    private GameObject go;
    private Health health;

    [SetUp]
    public void SetUp()
    {
        go = new GameObject("zombie");
        health = go.AddComponent<Health>();
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(go);

    [Test]
    public void ReplicatedDamage_SetsServerHealth_AndIgnoresLocalArmor()
    {
        health.DamageReduction = 0.5f;
        float reported = -1f;
        health.Damaged += (amount, current) => reported = current;

        health.ApplyReplicatedDamage(30f, 70f, Vector3.zero, false);

        Assert.AreEqual(70f, health.CurrentHealth);
        Assert.AreEqual(70f, reported);
        Assert.IsFalse(health.IsDead);
    }

    [Test]
    public void ReplicatedDamage_WithDirection_RaisesHitBeforeDamage_AndRecordsHit()
    {
        var order = new System.Collections.Generic.List<string>();
        health.HitReceived += (a, d, h) => order.Add("hit");
        health.Damaged += (a, c) => order.Add("damaged");

        health.ApplyReplicatedDamage(10f, 90f, Vector3.forward, true);

        CollectionAssert.AreEqual(new[] { "hit", "damaged" }, order);
        Assert.AreEqual(Vector3.forward, health.LastHitDirection);
        Assert.IsTrue(health.LastHitWasHeadshot);
    }

    [Test]
    public void ReplicatedLethalDamage_DiesOnce_AndLaterDamageIsIgnored()
    {
        int died = 0;
        health.Died += () => died++;

        health.ApplyReplicatedDamage(100f, 0f, Vector3.back, false);
        health.ApplyReplicatedDamage(5f, 0f, Vector3.back, false);

        Assert.IsTrue(health.IsDead);
        Assert.AreEqual(1, died);
    }

    [Test]
    public void ReplicatedDamage_WithoutDirection_DoesNotRaiseHitReaction()
    {
        int hits = 0;
        health.HitReceived += (a, d, h) => hits++;

        health.ApplyReplicatedDamage(5f, 95f, Vector3.zero, false);

        Assert.AreEqual(0, hits);
    }
}
