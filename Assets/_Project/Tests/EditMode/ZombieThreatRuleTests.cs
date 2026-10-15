// 거점 유무, 게임 시작부터 흐른 시간, 시각에 따른 좀비 정찰/추적 규칙(ZombieThreatRule)을 검증하는 EditMode 테스트다.
using NUnit.Framework;

public class ZombieThreatRuleTests
{
    private const float Grace = ZombieThreatRule.GraceHours;

    [Test]
    public void WithBase_NeverPatrols_AndHuntsAtNight_EvenInTheFirstDays()
    {
        Assert.IsFalse(ZombieThreatRule.Patrols(true, 0f));
        Assert.IsTrue(ZombieThreatRule.Hunts(true, true, 0f));
    }

    [Test]
    public void WithBase_DoesNotHuntInDaytime()
    {
        Assert.IsFalse(ZombieThreatRule.Hunts(false, true, 0f));
    }

    [Test]
    public void NoBase_PatrolsForTheFirstTenGameDays_AndNeverHuntsMeanwhile()
    {
        Assert.IsTrue(ZombieThreatRule.Patrols(false, 0f));
        Assert.IsTrue(ZombieThreatRule.Patrols(false, Grace - 0.01f));
        Assert.IsFalse(ZombieThreatRule.Hunts(true, false, 0f));
        Assert.IsFalse(ZombieThreatRule.Hunts(true, false, Grace - 0.01f));
    }

    [Test]
    public void NoBase_AfterTenGameDaysFromStart_StopsPatrollingAndHuntsAtNight()
    {
        Assert.AreEqual(240f, Grace);
        Assert.IsFalse(ZombieThreatRule.Patrols(false, Grace));
        Assert.IsTrue(ZombieThreatRule.Hunts(true, false, Grace));
        Assert.IsFalse(ZombieThreatRule.Hunts(false, false, Grace));
    }

    [Test]
    public void LosingTheBaseAfterTheGrace_DoesNotRestartIt()
    {
        // 유예가 끝난 뒤에는 거점이 부서져도 정찰로 돌아가지 않는다(게임 시작부터 센다).
        float elapsed = Grace + 50f;
        Assert.IsFalse(ZombieThreatRule.Patrols(false, elapsed));
        Assert.IsTrue(ZombieThreatRule.Hunts(true, false, elapsed));
    }

    [Test]
    public void LosingTheBaseInsideTheGrace_PatrolsOnlyUntilTheTenthDayFromStart()
    {
        float elapsed = ZombieThreatRule.AdvanceElapsedHours(0f, 5f * 24f);
        Assert.IsTrue(ZombieThreatRule.Patrols(false, elapsed));
        elapsed = ZombieThreatRule.AdvanceElapsedHours(elapsed, 5f * 24f);
        Assert.IsFalse(ZombieThreatRule.Patrols(false, elapsed));
    }

    [Test]
    public void Advance_AccumulatesRegardlessOfBase_AndIgnoresNegativeInputs()
    {
        float hours = ZombieThreatRule.AdvanceElapsedHours(0f, 5f);
        hours = ZombieThreatRule.AdvanceElapsedHours(hours, 3f);
        Assert.AreEqual(8f, hours, 0.0001f);
        Assert.AreEqual(2f, ZombieThreatRule.AdvanceElapsedHours(2f, -5f), 0.0001f);
        Assert.AreEqual(1f, ZombieThreatRule.AdvanceElapsedHours(-3f, 1f), 0.0001f);
    }

    [Test]
    public void SaveData_ElapsedGameHours_RoundTrips_AndLegacySaveIsNegative()
    {
        GameSaveData legacy = UnityEngine.JsonUtility.FromJson<GameSaveData>("{\"schemaVersion\":1}");
        Assert.Less(legacy.elapsedGameHours, 0f);

        string json = UnityEngine.JsonUtility.ToJson(new GameSaveData { elapsedGameHours = 37.5f });
        Assert.AreEqual(37.5f, UnityEngine.JsonUtility.FromJson<GameSaveData>(json).elapsedGameHours, 0.0001f);
    }
}
