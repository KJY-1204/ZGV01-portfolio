// LifetimeTimer(수명 만료 규칙)와 수명 세이브 하위 호환을 검증하는 EditMode 테스트다.
using NUnit.Framework;
using UnityEngine;

public class LifetimeTimerTests
{
    [Test]
    public void StartsFull_AndNotExpired()
    {
        LifetimeTimer timer = new LifetimeTimer(600f);
        Assert.AreEqual(600f, timer.Remaining);
        Assert.IsFalse(timer.IsExpired);
    }

    [Test]
    public void Tick_ReducesRemaining_AndExpiresExactlyOnce()
    {
        LifetimeTimer timer = new LifetimeTimer(10f);
        Assert.IsFalse(timer.Tick(4f));
        Assert.AreEqual(6f, timer.Remaining, 0.001f);
        Assert.IsTrue(timer.Tick(6f));
        Assert.IsTrue(timer.IsExpired);
        Assert.IsFalse(timer.Tick(1f));
    }

    [Test]
    public void Tick_Overshoot_ClampsToZero()
    {
        LifetimeTimer timer = new LifetimeTimer(5f);
        Assert.IsTrue(timer.Tick(100f));
        Assert.AreEqual(0f, timer.Remaining);
    }

    [Test]
    public void Tick_NonPositive_IsIgnored()
    {
        LifetimeTimer timer = new LifetimeTimer(5f);
        Assert.IsFalse(timer.Tick(0f));
        Assert.IsFalse(timer.Tick(-3f));
        Assert.AreEqual(5f, timer.Remaining);
    }

    [Test]
    public void SetRemaining_ClampsToRange()
    {
        LifetimeTimer timer = new LifetimeTimer(60f);
        timer.SetRemaining(25f);
        Assert.AreEqual(25f, timer.Remaining);
        timer.SetRemaining(999f);
        Assert.AreEqual(60f, timer.Remaining);
        timer.SetRemaining(-5f);
        Assert.AreEqual(0f, timer.Remaining);
        Assert.IsTrue(timer.IsExpired);
    }

    [Test]
    public void BuildableSaveData_LifetimeRemaining_RoundTripsAndLegacyIsNegative()
    {
        BuildableSaveData data = new BuildableSaveData { buildableId = "corpse_bag", lifetimeRemaining = 123.5f };
        Assert.AreEqual(123.5f, JsonUtility.FromJson<BuildableSaveData>(JsonUtility.ToJson(data)).lifetimeRemaining);

        BuildableSaveData legacy = JsonUtility.FromJson<BuildableSaveData>("{\"buildableId\":\"corpse_bag\",\"worldId\":\"x\"}");
        Assert.Less(legacy.lifetimeRemaining, 0f);
    }
}
