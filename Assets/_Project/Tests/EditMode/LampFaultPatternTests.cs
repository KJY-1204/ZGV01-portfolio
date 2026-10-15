// 고장 가로등 배정, 깜빡임 패턴, 보너스 드랍 판정 규칙을 검증하는 EditMode 테스트다.
using NUnit.Framework;

public class LampFaultPatternTests
{
    [Test]
    public void Assign_SplitsRollIntoDeadFlickerAndNormal()
    {
        Assert.AreEqual(LampFault.Dead, LampFaultPattern.Assign(0.1f, 0.4f, 0.25f));
        Assert.AreEqual(LampFault.Flicker, LampFaultPattern.Assign(0.5f, 0.4f, 0.25f));
        Assert.AreEqual(LampFault.None, LampFaultPattern.Assign(0.9f, 0.4f, 0.25f));
    }

    [Test]
    public void Multiplier_DeadIsAlwaysOff_NoneIsAlwaysOn()
    {
        for (float t = 0f; t < 60f; t += 0.37f)
        {
            Assert.AreEqual(0f, LampFaultPattern.Multiplier(LampFault.Dead, t, 3));
            Assert.AreEqual(1f, LampFaultPattern.Multiplier(LampFault.None, t, 3));
        }
    }

    [Test]
    public void Flicker_IsDeterministic_AndHasBothLongOffAndLongOnRests()
    {
        // 0.05초 간격으로 훑어 가장 긴 연속 꺼짐/켜짐 구간(초)을 잰다.
        float longestOff = 0f, longestOn = 0f, run = 0f;
        bool prev = true;
        for (float t = 0f; t < 600f; t += 0.05f)
        {
            float a = LampFaultPattern.Multiplier(LampFault.Flicker, t, 7);
            Assert.AreEqual(a, LampFaultPattern.Multiplier(LampFault.Flicker, t, 7));
            bool on = a > 0f;
            run = on == prev ? run + 0.05f : 0.05f;
            prev = on;
            if (on) longestOn = System.Math.Max(longestOn, run); else longestOff = System.Math.Max(longestOff, run);
        }
        Assert.Greater(longestOff, 3f, "깜빡인 뒤 한동안 꺼져 있어야 한다");
        Assert.Greater(longestOn, 3f, "깜빡인 뒤 한동안 켜져 있어야 한다");
    }

    [Test]
    public void Flicker_DiffersBetweenSeeds()
    {
        bool differs = false;
        for (float t = 0f; t < 120f && !differs; t += 0.05f)
            differs = LampFaultPattern.Multiplier(LampFault.Flicker, t, 1) != LampFaultPattern.Multiplier(LampFault.Flicker, t, 2);
        Assert.IsTrue(differs);
    }

    [Test]
    public void BonusDrops_RespectsChanceAndOwnership()
    {
        Assert.IsTrue(LampFaultPattern.BonusDrops(0.15f, 0.1f, false, true));
        Assert.IsFalse(LampFaultPattern.BonusDrops(0.15f, 0.2f, false, true));
        Assert.IsFalse(LampFaultPattern.BonusDrops(0.15f, 0.1f, true, true));
        Assert.IsTrue(LampFaultPattern.BonusDrops(0.15f, 0.1f, true, false));
        Assert.IsFalse(LampFaultPattern.BonusDrops(0f, 0f, false, false));
    }

    [Test]
    public void SparkStarts_OnlyWhenLampGoesOutAfterCooldown()
    {
        Assert.IsTrue(LampFaultPattern.SparkStarts(true, false, 10f, 0f));
        Assert.IsFalse(LampFaultPattern.SparkStarts(false, false, 10f, 0f));
        Assert.IsFalse(LampFaultPattern.SparkStarts(false, true, 10f, 0f));
        Assert.IsFalse(LampFaultPattern.SparkStarts(true, true, 10f, 0f));
        Assert.IsFalse(LampFaultPattern.SparkStarts(true, false, 10f, 9f), "쿨다운 안에서는 튀지 않는다");
    }
}
