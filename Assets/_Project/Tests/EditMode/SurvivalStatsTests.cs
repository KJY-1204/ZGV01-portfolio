// SurvivalStats의 시간 경과 감소, 바닥남 피해, 허약 판정, 소비 규칙을 검증하는 EditMode 테스트다.
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class SurvivalStatsTests
{
    private GameObject go;
    private Health health;
    private Inventory inventory;
    private SurvivalStats stats;
    private SurvivalStatsConfig config;
    private readonly List<Object> created = new List<Object>();

    [SetUp]
    public void SetUp()
    {
        // 허기 1/s, 갈증 2/s(최대 100), 바닥난 수치당 초당 피해 2, 피해 간격 1초, 허약 기준 0.3, 배율 0.5
        config = ScriptableObject.CreateInstance<SurvivalStatsConfig>();
        created.Add(config);
        TestUtil.SetFloat(config, "hungerMax", 100f);
        TestUtil.SetFloat(config, "hungerDrainPerSecond", 1f);
        TestUtil.SetFloat(config, "thirstMax", 100f);
        TestUtil.SetFloat(config, "thirstDrainPerSecond", 2f);
        TestUtil.SetFloat(config, "emptyDamagePerSecond", 2f);
        TestUtil.SetFloat(config, "damageInterval", 1f);
        TestUtil.SetFloat(config, "lowThreshold", 0.3f);
        TestUtil.SetFloat(config, "weakenedDamageMultiplier", 0.5f);
        TestUtil.SetFloat(config, "weakenedAttackSpeedMultiplier", 0.5f);

        go = new GameObject("player");
        stats = go.AddComponent<SurvivalStats>(); // RequireComponent로 Health/Inventory도 붙는다
        health = go.GetComponent<Health>();
        inventory = go.GetComponent<Inventory>();
        TestUtil.SetObject(stats, "config", config);
        TestUtil.Awake(health);
        TestUtil.Awake(stats);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(go);
        foreach (Object o in created) Object.DestroyImmediate(o);
        created.Clear();
    }

    private ConsumableDefinition Consumable(float hunger, float thirst, float healthRestore)
    {
        var item = ScriptableObject.CreateInstance<ConsumableDefinition>();
        created.Add(item);
        TestUtil.SetFloat(item, "hungerRestore", hunger);
        TestUtil.SetFloat(item, "thirstRestore", thirst);
        TestUtil.SetFloat(item, "healthRestore", healthRestore);
        TestUtil.SetInt(item, "maxStack", 5);
        return item;
    }

    [Test]
    public void Awake_StartsFull()
    {
        Assert.AreEqual(100f, stats.Hunger.Current);
        Assert.AreEqual(100f, stats.Thirst.Current);
        Assert.IsFalse(stats.IsWeakened);
    }

    [Test]
    public void Tick_DrainsByRateTimesTime()
    {
        stats.Tick(10f);
        Assert.AreEqual(90f, stats.Hunger.Current, 1e-4f);
        Assert.AreEqual(80f, stats.Thirst.Current, 1e-4f);
    }

    [Test]
    public void DrainMultiplier_ScalesDrain()
    {
        stats.DrainMultiplier = 1.2f;
        stats.Tick(10f);
        Assert.AreEqual(88f, stats.Hunger.Current, 1e-3f);
        Assert.AreEqual(76f, stats.Thirst.Current, 1e-3f);
    }

    [Test]
    public void Tick_RaisesChanged()
    {
        int changed = 0;
        stats.Changed += () => changed++;
        stats.Tick(1f);
        Assert.AreEqual(1, changed);
    }

    [Test]
    public void Tick_WhenDead_DoesNothing()
    {
        health.ApplyDamage(1000f);
        stats.Tick(10f);
        Assert.AreEqual(100f, stats.Hunger.Current);
    }

    [Test]
    public void Tick_WithoutConfig_DoesNothing()
    {
        TestUtil.SetObject(stats, "config", null);
        Assert.DoesNotThrow(() => stats.Tick(10f));
        Assert.AreEqual(100f, stats.Hunger.Current);
    }

    [Test]
    public void EmptyMeter_DamagesHealthOnlyAfterInterval()
    {
        stats.Restore(0f, 100f);
        stats.Tick(0.5f);
        Assert.AreEqual(100f, health.CurrentHealth);
        stats.Tick(0.5f);
        Assert.AreEqual(98f, health.CurrentHealth, 1e-4f); // 2/s x 1s x 바닥난 수치 1개
    }

    [Test]
    public void TwoEmptyMeters_DoubleTheDamage()
    {
        stats.Restore(0f, 0f);
        stats.Tick(1f);
        Assert.AreEqual(96f, health.CurrentHealth, 1e-4f);
    }

    [Test]
    public void PenaltyTimer_ResetsWhenNoMeterIsEmpty()
    {
        stats.Restore(0f, 100f);
        stats.Tick(0.9f);                 // 0.9초 쌓임
        stats.Restore(100f, 100f);
        stats.Tick(0.05f);                // 바닥난 수치 없음 -> 타이머 초기화
        stats.Restore(0f, 100f);
        stats.Tick(0.2f);                 // 새로 0.2초만 쌓여야 한다
        Assert.AreEqual(100f, health.CurrentHealth);
    }

    [Test]
    public void LowMeter_MakesWeakened_AndRaisesEventOnce()
    {
        var events = new List<bool>();
        stats.WeakenedChanged += events.Add;

        stats.Restore(30f, 100f);   // 비율 0.3은 기준 이하
        stats.Tick(0f);
        stats.Tick(0f);
        Assert.IsTrue(stats.IsWeakened);
        CollectionAssert.AreEqual(new[] { true }, events);
        Assert.AreEqual(0.5f, stats.DamageMultiplier);
        Assert.AreEqual(0.5f, stats.AttackSpeedMultiplier);
    }

    [Test]
    public void Weakened_Clears_WhenAboveThreshold()
    {
        stats.Restore(10f, 100f);
        Assert.IsTrue(stats.IsWeakened);
        stats.Restore(31f, 100f);
        Assert.IsFalse(stats.IsWeakened);
        Assert.AreEqual(1f, stats.DamageMultiplier);
        Assert.AreEqual(1f, stats.AttackSpeedMultiplier);
    }

    [Test]
    public void Thirst_AlsoTriggersWeakened()
    {
        stats.Restore(100f, 5f);
        Assert.IsTrue(stats.IsWeakened);
    }

    [Test]
    public void Restore_ClampsToRange()
    {
        stats.Restore(999f, -5f);
        Assert.AreEqual(100f, stats.Hunger.Current);
        Assert.AreEqual(0f, stats.Thirst.Current);
    }

    [Test]
    public void TryConsume_AppliesEffects_RemovesOneItem_RaisesEvent()
    {
        ConsumableDefinition can = Consumable(40f, 10f, 15f);
        inventory.Add(can, 2);
        stats.Restore(10f, 50f);
        health.ApplyDamage(50f);
        ConsumableDefinition consumed = null;
        stats.Consumed += c => consumed = c;

        Assert.IsTrue(stats.TryConsume(can));

        Assert.AreEqual(50f, stats.Hunger.Current, 1e-4f);
        Assert.AreEqual(60f, stats.Thirst.Current, 1e-4f);
        Assert.AreEqual(65f, health.CurrentHealth, 1e-4f);
        Assert.AreEqual(1, inventory.GetQuantity(can));
        Assert.AreSame(can, consumed);
    }

    [Test]
    public void TryConsume_NegativeHealth_DamagesInsteadOfHealing()
    {
        ConsumableDefinition dirty = Consumable(0f, 15f, -6f);
        inventory.Add(dirty, 1);
        stats.Restore(100f, 40f);
        Assert.IsTrue(stats.TryConsume(dirty));
        Assert.AreEqual(55f, stats.Thirst.Current, 1e-4f);
        Assert.AreEqual(94f, health.CurrentHealth, 1e-4f);
        Assert.AreEqual(0, inventory.GetQuantity(dirty));
    }

    [Test]
    public void TryConsume_NegativeHealth_CanKill()
    {
        ConsumableDefinition poison = Consumable(0f, 0f, -500f);
        inventory.Add(poison, 1);
        bool died = false;
        health.Died += () => died = true;
        stats.TryConsume(poison);
        Assert.IsTrue(died);
        Assert.IsTrue(health.IsDead);
    }

    [Test]
    public void TryConsume_ClearsWeakenedWhenRestoredAboveThreshold()
    {
        ConsumableDefinition can = Consumable(50f, 0f, 0f);
        inventory.Add(can, 1);
        stats.Restore(10f, 100f);
        Assert.IsTrue(stats.IsWeakened);
        stats.TryConsume(can);
        Assert.IsFalse(stats.IsWeakened);
    }

    [Test]
    public void TryConsume_FailsWithoutItemOrWhenDeadOrNull()
    {
        ConsumableDefinition can = Consumable(40f, 0f, 0f);
        Assert.IsFalse(stats.TryConsume(can));   // 인벤토리에 없음
        Assert.IsFalse(stats.TryConsume(null));

        inventory.Add(can, 1);
        health.ApplyDamage(1000f);
        Assert.IsFalse(stats.TryConsume(can));   // 사망
        Assert.AreEqual(1, inventory.GetQuantity(can));
    }

    [Test]
    public void Revive_SetsMetersToFraction_ClearsWeakened_AndRaisesChanged()
    {
        stats.Hunger.Set(0f);
        stats.Thirst.Set(0f);
        stats.Tick(0.1f);
        Assert.IsTrue(stats.IsWeakened);

        int changed = 0;
        stats.Changed += () => changed++;
        stats.Revive(0.5f);

        Assert.AreEqual(50f, stats.Hunger.Current, 0.01f);
        Assert.AreEqual(50f, stats.Thirst.Current, 0.01f);
        Assert.IsFalse(stats.IsWeakened);
        Assert.AreEqual(1, changed);
    }
}
