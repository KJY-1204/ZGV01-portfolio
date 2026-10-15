// 말 걸음걸이 선택 규칙(HorseGaitRule)을 검증하는 EditMode 테스트다.
using NUnit.Framework;

public class HorseGaitRuleTests
{
    private const float Walk = 4.5f, Gallop = 11f;

    [Test]
    public void SlowSpeed_IsIdle()
    {
        Assert.AreEqual(HorseGait.Idle, HorseGaitRule.Choose(0f, 0f, Walk, Gallop));
        Assert.AreEqual(HorseGait.Idle, HorseGaitRule.Choose(0.3f, 0f, Walk, Gallop));
    }

    [Test]
    public void WalkingSpeed_IsWalk_AndSprintingSpeed_IsGallop()
    {
        Assert.AreEqual(HorseGait.Walk, HorseGaitRule.Choose(4.5f, 0f, Walk, Gallop));
        Assert.AreEqual(HorseGait.Gallop, HorseGaitRule.Choose(11f, 0f, Walk, Gallop));
    }

    [Test]
    public void SpeedingUp_SwitchesToGallopOnce_NotBeforeThreshold()
    {
        // 걷기 4.5, 질주 11이면 경계는 4.5 + 6.5 * 0.4 = 7.1이다.
        Assert.AreEqual(HorseGait.Walk, HorseGaitRule.Choose(7.0f, 0f, Walk, Gallop));
        Assert.AreEqual(HorseGait.Gallop, HorseGaitRule.Choose(7.2f, 0f, Walk, Gallop));
    }

    [Test]
    public void LongEnoughInTheAir_IsJump_ButAShortHopIsNot()
    {
        Assert.AreEqual(HorseGait.Jump, HorseGaitRule.Choose(8f, 0.25f, Walk, Gallop));
        Assert.AreEqual(HorseGait.Walk, HorseGaitRule.Choose(4.5f, 0.1f, Walk, Gallop));
    }

    [Test]
    public void BadSpeeds_DoNotThrow()
    {
        Assert.AreEqual(HorseGait.Walk, HorseGaitRule.Choose(5f, 0f, 5f, 3f));
    }
}
