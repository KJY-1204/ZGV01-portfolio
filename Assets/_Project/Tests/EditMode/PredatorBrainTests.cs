// PredatorBrain의 행동 전이(추격, 공격, 포기, 피격 반응)와 공격 타격 시점을 검증하는 EditMode 테스트다.
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class PredatorBrainTests
{
    private const float Far = 1000f;

    // 감지 10m, 포기 거리 30m / 5초, 공격 사거리 2m, 재공격 대기 2초, 공격 지속 1초, 타격 0.4초, 대기 2초, 배회 4초.
    private static PredatorBrain Make() =>
        new PredatorBrain(10f, 30f, 5f, 2f, 2f, 1f, 0.4f, 2f, 2f, 4f, 4f, (min, max) => min);

    [Test]
    public void StartsIdle()
    {
        Assert.AreEqual(PredatorState.Idle, Make().State);
    }

    [Test]
    public void Idle_Wanders_ThenIdles_OnTimers()
    {
        PredatorBrain brain = Make();
        brain.Tick(2.1f, Far);
        Assert.AreEqual(PredatorState.Wander, brain.State);
        brain.Tick(4.1f, Far);
        Assert.AreEqual(PredatorState.Idle, brain.State);
    }

    [Test]
    public void Wander_ReturnsToIdle_WhenArrived()
    {
        PredatorBrain brain = Make();
        brain.Tick(2.1f, Far);
        brain.NotifyArrived();
        Assert.AreEqual(PredatorState.Idle, brain.State);
    }

    [Test]
    public void TargetInsideDetectDistance_StartsChase_FromIdleAndWander()
    {
        PredatorBrain idle = Make();
        idle.Tick(0.1f, 10f);
        Assert.AreEqual(PredatorState.Chase, idle.State);

        PredatorBrain wander = Make();
        wander.Tick(2.1f, Far);
        wander.Tick(0.1f, 9f);
        Assert.AreEqual(PredatorState.Chase, wander.State);
    }

    [Test]
    public void TargetOutsideDetectDistance_DoesNotChase()
    {
        PredatorBrain brain = Make();
        brain.Tick(0.1f, 10.5f);
        Assert.AreEqual(PredatorState.Idle, brain.State);
    }

    [Test]
    public void Damage_StartsChase_EvenFarAway()
    {
        PredatorBrain brain = Make();
        brain.NotifyDamaged();
        Assert.AreEqual(PredatorState.Chase, brain.State);
    }

    [Test]
    public void Damage_DoesNotInterruptAttack()
    {
        PredatorBrain brain = Make();
        brain.Tick(0.1f, 1f);       // 감지 -> 추격
        brain.Tick(0.1f, 1f);       // 사거리 안 -> 공격
        Assert.AreEqual(PredatorState.Attack, brain.State);
        brain.NotifyDamaged();
        Assert.AreEqual(PredatorState.Attack, brain.State);
    }

    [Test]
    public void ChaseInsideAttackRange_StartsAttack()
    {
        PredatorBrain brain = Make();
        brain.Tick(0.1f, 5f);
        Assert.AreEqual(PredatorState.Chase, brain.State);
        brain.Tick(0.1f, 2f);       // 정확히 사거리 경계도 공격
        Assert.AreEqual(PredatorState.Attack, brain.State);
    }

    [Test]
    public void Chase_DoesNotAttack_OutsideRange()
    {
        PredatorBrain brain = Make();
        brain.Tick(0.1f, 5f);
        brain.Tick(0.1f, 2.5f);
        Assert.AreEqual(PredatorState.Chase, brain.State);
    }

    [Test]
    public void AttackLanded_FiresOncePerAttack_AtHitDelay()
    {
        int landed = 0;
        PredatorBrain brain = Make();
        brain.AttackLanded += () => landed++;
        brain.Tick(0.1f, 1f);
        brain.Tick(0.1f, 1f);               // 공격 시작
        brain.Tick(0.3f, 1f);               // 0.3초: 아직
        Assert.AreEqual(0, landed);
        brain.Tick(0.2f, 1f);               // 0.5초: 타격(0.4초) 지남
        Assert.AreEqual(1, landed);
        brain.Tick(0.3f, 1f);               // 같은 공격에서 또 나오지 않는다
        Assert.AreEqual(1, landed);
    }

    [Test]
    public void Attack_EndsAfterDuration_BackToChase()
    {
        PredatorBrain brain = Make();
        brain.Tick(0.1f, 1f);
        brain.Tick(0.1f, 1f);
        brain.Tick(0.9f, 1f);
        Assert.AreEqual(PredatorState.Attack, brain.State);
        brain.Tick(0.2f, 1f);
        Assert.AreEqual(PredatorState.Chase, brain.State);
    }

    [Test]
    public void NextAttack_WaitsForCooldown_MeasuredFromAttackStart()
    {
        int landed = 0;
        PredatorBrain brain = Make();
        brain.AttackLanded += () => landed++;
        brain.Tick(0.1f, 1f);
        brain.Tick(0.1f, 1f);               // 공격 시작 (대기 2초 시작)
        brain.Tick(1.1f, 1f);               // 공격 끝나 추격 복귀 (경과 1.1초)
        Assert.AreEqual(PredatorState.Chase, brain.State);
        brain.Tick(0.5f, 1f);               // 경과 1.6초: 대기 중이라 아직 공격 안 함
        Assert.AreEqual(PredatorState.Chase, brain.State);
        brain.Tick(0.5f, 1f);               // 경과 2.1초: 대기 끝, 다시 공격
        Assert.AreEqual(PredatorState.Attack, brain.State);
        brain.Tick(0.5f, 1f);
        Assert.AreEqual(2, landed);
    }

    [Test]
    public void Chase_GivesUp_AfterBeingFarLongEnough()
    {
        PredatorBrain brain = Make();
        brain.NotifyDamaged();
        brain.Tick(4.9f, 40f);
        Assert.AreEqual(PredatorState.Chase, brain.State);
        brain.Tick(0.2f, 40f);
        Assert.AreEqual(PredatorState.Idle, brain.State);
    }

    [Test]
    public void GiveUpTimer_Resets_WhenTargetComesBackWithinLoseDistance()
    {
        PredatorBrain brain = Make();
        brain.NotifyDamaged();
        brain.Tick(4f, 40f);
        brain.Tick(0.1f, 20f);              // 포기 거리(30m) 안으로 돌아옴 -> 초기화
        brain.Tick(4f, 40f);
        Assert.AreEqual(PredatorState.Chase, brain.State);
        brain.Tick(1.1f, 40f);
        Assert.AreEqual(PredatorState.Idle, brain.State);
    }

    [Test]
    public void AfterGivingUp_DoesNotImmediatelyReChase_WhenTargetStillFar()
    {
        PredatorBrain brain = Make();
        brain.NotifyDamaged();
        brain.Tick(5.1f, 40f);
        Assert.AreEqual(PredatorState.Idle, brain.State);
        brain.Tick(0.1f, 40f);
        Assert.AreEqual(PredatorState.Idle, brain.State);
    }

    [Test]
    public void Death_IsFinal_AndSilencesAttackEvent()
    {
        int landed = 0;
        PredatorBrain brain = Make();
        brain.AttackLanded += () => landed++;
        brain.Tick(0.1f, 1f);
        brain.Tick(0.1f, 1f);               // 공격 중
        brain.NotifyDead();
        brain.Tick(5f, 1f);
        brain.NotifyDamaged();
        Assert.IsTrue(brain.IsDead);
        Assert.AreEqual(PredatorState.Dead, brain.State);
        Assert.AreEqual(0, landed);
    }

    [Test]
    public void StateChanged_ReportsTransitionsInOrder()
    {
        var states = new List<PredatorState>();
        PredatorBrain brain = Make();
        brain.StateChanged += states.Add;
        brain.Tick(0.1f, 5f);               // Chase
        brain.Tick(0.1f, 1f);               // Attack
        brain.Tick(1.1f, 1f);               // Chase
        brain.NotifyDead();                 // Dead
        CollectionAssert.AreEqual(
            new[] { PredatorState.Chase, PredatorState.Attack, PredatorState.Chase, PredatorState.Dead }, states);
    }

    [Test]
    public void LoseDistance_IsNeverBelowDetectDistance()
    {
        // 포기 거리를 감지 거리보다 작게 줘도 감지 거리로 올려서 추격이 바로 풀리지 않는다.
        PredatorBrain brain = new PredatorBrain(10f, 5f, 1f, 2f, 2f, 1f, 0.4f, 2f, 2f, 4f, 4f, (min, max) => min);
        brain.Tick(0.1f, 8f);                // 감지 안 -> 추격
        brain.Tick(5f, 8f);                  // 포기 거리가 10으로 보정돼 8m는 안쪽이라 유지
        Assert.AreEqual(PredatorState.Chase, brain.State);
    }
}

public class PredatorDefinitionTests
{
    private PredatorDefinition def;

    [SetUp]
    public void SetUp()
    {
        def = ScriptableObject.CreateInstance<PredatorDefinition>();
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(def);

    [Test]
    public void CreatePredatorBrain_UsesDefinitionDetectDistance()
    {
        TestUtil.SetFloat(def, "detectDistance", 7f);
        PredatorBrain brain = def.CreatePredatorBrain();
        brain.Tick(0.01f, 8f);
        Assert.AreEqual(PredatorState.Idle, brain.State);
        brain.Tick(0.01f, 6f);
        Assert.AreEqual(PredatorState.Chase, brain.State);
    }

    [Test]
    public void PickAttackState_ReturnsOneOfConfigured_OrEmpty()
    {
        string picked = def.PickAttackState();
        CollectionAssert.Contains(new[] { "attack A", "attack B" }, picked);

        var so = new UnityEditor.SerializedObject(def);
        so.FindProperty("attackStates").arraySize = 0;
        so.ApplyModifiedPropertiesWithoutUndo();
        Assert.AreEqual(string.Empty, def.PickAttackState());
    }

    [Test]
    public void IsAnAnimalDefinition_SoMeatAndSpeedsAreShared()
    {
        AnimalDefinition asAnimal = def;
        Assert.AreEqual(0, asAnimal.RollMeat((min, max) => max));   // 고기 아이템이 없으면 0
        Assert.Greater(asAnimal.RunSpeed, asAnimal.WalkSpeed);
    }
}
