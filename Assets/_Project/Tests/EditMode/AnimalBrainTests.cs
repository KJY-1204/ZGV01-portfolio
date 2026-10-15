// AnimalBrain의 행동 전이(대기, 배회, 도주, 사망)와 AnimalDefinition의 고기 굴림을 검증하는 EditMode 테스트다.
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class AnimalBrainTests
{
    private const float Far = 1000f;

    // 알림 거리 10, 도주 3초, 대기 2초, 배회 4초(범위 최소=최대라 값이 고정된다).
    private static AnimalBrain Make(float alert = 10f, float flee = 3f) =>
        new AnimalBrain(alert, flee, 2f, 2f, 4f, 4f, (min, max) => min);

    [Test]
    public void StartsIdle()
    {
        Assert.AreEqual(PreyState.Idle, Make().State);
    }

    [Test]
    public void Idle_BecomesWander_AfterIdleDuration()
    {
        AnimalBrain brain = Make();
        brain.Tick(1.9f, Far);
        Assert.AreEqual(PreyState.Idle, brain.State);
        brain.Tick(0.2f, Far);
        Assert.AreEqual(PreyState.Wander, brain.State);
    }

    [Test]
    public void Wander_BecomesIdle_AfterWanderDuration()
    {
        AnimalBrain brain = Make();
        brain.Tick(2f, Far);
        Assert.AreEqual(PreyState.Wander, brain.State);
        brain.Tick(3.9f, Far);
        Assert.AreEqual(PreyState.Wander, brain.State);
        brain.Tick(0.2f, Far);
        Assert.AreEqual(PreyState.Idle, brain.State);
    }

    [Test]
    public void Wander_ReturnsToIdle_WhenArrived()
    {
        AnimalBrain brain = Make();
        brain.Tick(2f, Far);
        brain.NotifyArrived();
        Assert.AreEqual(PreyState.Idle, brain.State);
    }

    [Test]
    public void NotifyArrived_IgnoredOutsideWander()
    {
        AnimalBrain brain = Make();
        brain.NotifyArrived();
        Assert.AreEqual(PreyState.Idle, brain.State);
    }

    [Test]
    public void ThreatInsideAlertDistance_MakesIdleAnimalFlee()
    {
        AnimalBrain brain = Make();
        brain.Tick(0.1f, 9f);
        Assert.AreEqual(PreyState.Flee, brain.State);
    }

    [Test]
    public void ThreatAtExactlyAlertDistance_Flees()
    {
        AnimalBrain brain = Make();
        brain.Tick(0.1f, 10f);
        Assert.AreEqual(PreyState.Flee, brain.State);
    }

    [Test]
    public void ThreatOutsideAlertDistance_DoesNotStartFlee()
    {
        AnimalBrain brain = Make();
        brain.Tick(0.1f, 10.5f);
        Assert.AreEqual(PreyState.Idle, brain.State);
    }

    [Test]
    public void ThreatInsideAlertDistance_MakesWanderingAnimalFlee()
    {
        AnimalBrain brain = Make();
        brain.Tick(2f, Far);
        Assert.AreEqual(PreyState.Wander, brain.State);
        brain.Tick(0.1f, 5f);
        Assert.AreEqual(PreyState.Flee, brain.State);
    }

    [Test]
    public void Damage_MakesAnimalFlee_EvenWithoutThreatInSight()
    {
        AnimalBrain brain = Make();
        brain.NotifyDamaged();
        Assert.AreEqual(PreyState.Flee, brain.State);
    }

    [Test]
    public void Damage_WithZeroAlertDistance_StillFlees_ButProximityDoesNot()
    {
        AnimalBrain calm = Make(alert: 0f);
        calm.Tick(0.1f, 0.5f);
        Assert.AreEqual(PreyState.Idle, calm.State);   // 바로 옆에 있어도 놀라지 않는다
        calm.NotifyDamaged();
        Assert.AreEqual(PreyState.Flee, calm.State);
    }

    [Test]
    public void Flee_EndsOnlyAfterFleeSeconds_AndThreatFarEnough()
    {
        AnimalBrain brain = Make();
        brain.NotifyDamaged();
        brain.Tick(2.9f, Far);
        Assert.AreEqual(PreyState.Flee, brain.State);   // 도주 시간 3초 전
        brain.Tick(0.2f, Far);
        Assert.AreEqual(PreyState.Idle, brain.State);
    }

    [Test]
    public void Flee_Continues_WhileThreatStillClose_EvenAfterFleeSeconds()
    {
        AnimalBrain brain = Make();
        brain.NotifyDamaged();
        brain.Tick(5f, 11f);   // 경계 거리 밖이지만 1.25배(12.5m) 안이라 아직 진정하지 않는다
        Assert.AreEqual(PreyState.Flee, brain.State);
        brain.Tick(0.1f, 13f);
        Assert.AreEqual(PreyState.Idle, brain.State);
    }

    [Test]
    public void Flee_TimerRestarts_WhenThreatReappears()
    {
        AnimalBrain brain = Make();
        brain.NotifyDamaged();
        brain.Tick(2.5f, Far);
        brain.Tick(0.1f, 5f);      // 다시 위협이 가까워짐 -> 도주 시간 초기화
        brain.Tick(2.5f, Far);
        Assert.AreEqual(PreyState.Flee, brain.State);
        brain.Tick(0.6f, Far);
        Assert.AreEqual(PreyState.Idle, brain.State);
    }

    [Test]
    public void DamageWhileFleeing_RestartsFleeTimer_WithoutReenteringState()
    {
        var states = new List<PreyState>();
        AnimalBrain brain = Make();
        brain.StateChanged += states.Add;
        brain.NotifyDamaged();
        brain.Tick(2.5f, Far);
        brain.NotifyDamaged();
        brain.Tick(2.5f, Far);
        Assert.AreEqual(PreyState.Flee, brain.State);
        CollectionAssert.AreEqual(new[] { PreyState.Flee }, states);
    }

    [Test]
    public void Death_IsFinal()
    {
        AnimalBrain brain = Make();
        brain.NotifyDead();
        Assert.IsTrue(brain.IsDead);
        brain.NotifyDamaged();
        brain.Tick(100f, 1f);
        brain.NotifyArrived();
        Assert.AreEqual(PreyState.Dead, brain.State);
    }

    [Test]
    public void StateChanged_ReportsEachTransitionOnce()
    {
        var states = new List<PreyState>();
        AnimalBrain brain = Make();
        brain.StateChanged += states.Add;
        brain.Tick(2f, Far);          // Idle -> Wander
        brain.Tick(0.1f, 3f);         // Wander -> Flee
        brain.Tick(0.1f, 3f);         // 계속 Flee (알림 없음)
        brain.NotifyDead();           // Flee -> Dead
        CollectionAssert.AreEqual(new[] { PreyState.Wander, PreyState.Flee, PreyState.Dead }, states);
    }

    [Test]
    public void Durations_UseRandomFunctionPerState()
    {
        // 최대값을 고르는 난수: 대기 5초, 배회 8초
        AnimalBrain brain = new AnimalBrain(10f, 3f, 1f, 5f, 2f, 8f, (min, max) => max);
        brain.Tick(4.9f, Far);
        Assert.AreEqual(PreyState.Idle, brain.State);
        brain.Tick(0.2f, Far);
        Assert.AreEqual(PreyState.Wander, brain.State);
        brain.Tick(7.9f, Far);
        Assert.AreEqual(PreyState.Wander, brain.State);
        brain.Tick(0.2f, Far);
        Assert.AreEqual(PreyState.Idle, brain.State);
    }
}

public class AnimalDefinitionTests
{
    private readonly List<Object> created = new List<Object>();

    private AnimalDefinition Make(int min, int max, bool withMeat = true)
    {
        var def = ScriptableObject.CreateInstance<AnimalDefinition>();
        created.Add(def);
        TestUtil.SetInt(def, "meatMin", min);
        TestUtil.SetInt(def, "meatMax", max);
        if (withMeat)
        {
            ItemDefinition meat = TestItems.Item("meat", 20);
            created.Add(meat);
            TestUtil.SetObject(def, "meatItem", meat);
        }
        return def;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Object o in created) Object.DestroyImmediate(o);
        created.Clear();
    }

    [Test]
    public void RollMeat_UsesMinAndMaxFromDefinition()
    {
        int gotMin = -1, gotMax = -1;
        Make(2, 5).RollMeat((min, max) => { gotMin = min; gotMax = max; return min; });
        Assert.AreEqual(2, gotMin);
        Assert.AreEqual(5, gotMax);
    }

    [Test]
    public void RollMeat_ReturnsRolledValue()
    {
        Assert.AreEqual(4, Make(2, 5).RollMeat((min, max) => 4));
    }

    [Test]
    public void RollMeat_WithoutMeatItem_IsZero()
    {
        Assert.AreEqual(0, Make(2, 5, withMeat: false).RollMeat((min, max) => max));
    }

    [Test]
    public void RollMeat_MaxBelowMin_IsRaisedToMin()
    {
        int gotMax = -1;
        Make(3, 1).RollMeat((min, max) => { gotMax = max; return min; });
        Assert.AreEqual(3, gotMax);
    }

    [Test]
    public void RollMeat_NegativeMin_IsClampedToZero()
    {
        int gotMin = -1;
        Make(-4, 2).RollMeat((min, max) => { gotMin = min; return min; });
        Assert.AreEqual(0, gotMin);
    }

    [Test]
    public void CreateBrain_UsesDefinitionAlertDistance()
    {
        AnimalDefinition def = Make(1, 2);
        TestUtil.SetFloat(def, "alertDistance", 7f);
        AnimalBrain brain = def.CreateBrain();
        brain.Tick(0.01f, 6f);
        Assert.AreEqual(PreyState.Flee, brain.State);
    }
}

public class AnimalHideTests
{
    private readonly System.Collections.Generic.List<Object> created = new System.Collections.Generic.List<Object>();

    private AnimalDefinition Make(int min, int max, bool withHide = true)
    {
        var def = ScriptableObject.CreateInstance<AnimalDefinition>();
        created.Add(def);
        TestUtil.SetInt(def, "hideMin", min);
        TestUtil.SetInt(def, "hideMax", max);
        if (withHide)
        {
            ItemDefinition hide = TestItems.Item("hide", 20);
            created.Add(hide);
            TestUtil.SetObject(def, "hideItem", hide);
        }
        return def;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Object o in created) Object.DestroyImmediate(o);
        created.Clear();
    }

    [Test]
    public void RollHide_UsesHideRange_NotMeatRange()
    {
        AnimalDefinition def = Make(2, 3);
        TestUtil.SetInt(def, "meatMin", 7);
        TestUtil.SetInt(def, "meatMax", 9);
        int gotMin = -1, gotMax = -1;
        def.RollHide((min, max) => { gotMin = min; gotMax = max; return min; });
        Assert.AreEqual(2, gotMin);
        Assert.AreEqual(3, gotMax);
    }

    [Test]
    public void RollHide_WithoutHideItem_IsZero()
    {
        Assert.AreEqual(0, Make(2, 3, withHide: false).RollHide((min, max) => max));
    }

    [Test]
    public void RollHide_ReturnsRolledValue_AndClampsBadRange()
    {
        Assert.AreEqual(5, Make(1, 6).RollHide((min, max) => 5));
        int gotMin = -1, gotMax = -1;
        Make(-3, -1).RollHide((min, max) => { gotMin = min; gotMax = max; return min; });
        Assert.AreEqual(0, gotMin);
        Assert.AreEqual(0, gotMax);
    }

    [Test]
    public void DefaultDefinition_DropsNoHide()
    {
        var def = ScriptableObject.CreateInstance<AnimalDefinition>();
        created.Add(def);
        Assert.AreEqual(0, def.RollHide((min, max) => max));
    }
}
