// 해 고도에 따른 하늘 비중 규칙(SkyBlendWeights)이 시간대 사이에서 끊김 없이 이어지는지 검증하는 EditMode 테스트다.
using NUnit.Framework;

public class SkyBlendWeightsTests
{
    [Test]
    public void Elevation_FollowsTheSunArc()
    {
        Assert.AreEqual(0f, SkyBlendWeights.ElevationDegrees(6f), 0.01f);
        Assert.AreEqual(90f, SkyBlendWeights.ElevationDegrees(12f), 0.01f);
        Assert.AreEqual(0f, SkyBlendWeights.ElevationDegrees(18f), 0.01f);
        Assert.AreEqual(-90f, SkyBlendWeights.ElevationDegrees(0f), 0.01f);
        Assert.AreEqual(30f, SkyBlendWeights.ElevationDegrees(8f), 0.01f);
    }

    [Test]
    public void Weights_AlwaysSumToOne()
    {
        for (float e = -90f; e <= 90f; e += 0.5f)
        {
            SkyBlendWeights.Weights w = SkyBlendWeights.At(e);
            Assert.AreEqual(1f, w.night + w.glow + w.day, 0.001f, "고도 " + e);
            Assert.GreaterOrEqual(w.night, 0f);
            Assert.GreaterOrEqual(w.glow, 0f);
            Assert.GreaterOrEqual(w.day, 0f);
        }
    }

    [Test]
    public void Weights_PickTheRightSky_AtEachTimeOfDay()
    {
        Assert.Greater(SkyBlendWeights.At(-60f).night, 0.99f);
        Assert.Greater(SkyBlendWeights.At(0f).glow, 0.9f);
        Assert.Greater(SkyBlendWeights.At(70f).day, 0.99f);
    }

    [Test]
    public void Weights_ChangeSmoothly_NoJumpsBetweenSteps()
    {
        // 0.25도(약 1분 40초) 사이에 어떤 비중도 크게 튀지 않아야 한다.
        SkyBlendWeights.Weights prev = SkyBlendWeights.At(-90f);
        for (float e = -89.75f; e <= 90f; e += 0.25f)
        {
            SkyBlendWeights.Weights w = SkyBlendWeights.At(e);
            Assert.Less(System.Math.Abs(w.night - prev.night), 0.05f, "고도 " + e);
            Assert.Less(System.Math.Abs(w.glow - prev.glow), 0.05f, "고도 " + e);
            Assert.Less(System.Math.Abs(w.day - prev.day), 0.05f, "고도 " + e);
            prev = w;
        }
    }

    [Test]
    public void DayWarmth_IsHighNearHorizon_AndZeroHigh()
    {
        Assert.Greater(SkyBlendWeights.DayWarmth(2f), 0.99f);
        Assert.AreEqual(0f, SkyBlendWeights.DayWarmth(60f), 0.001f);
        Assert.Greater(SkyBlendWeights.DayWarmth(10f), SkyBlendWeights.DayWarmth(25f));
    }

    [Test]
    public void SunAndMoonDiscs_AppearOnlyWhenTheirBodyIsUp()
    {
        Assert.AreEqual(0f, SkyBlendWeights.SunDisc(-5f), 0.001f);
        Assert.AreEqual(1f, SkyBlendWeights.SunDisc(30f), 0.001f);
        Assert.AreEqual(0f, SkyBlendWeights.MoonDisc(20f), 0.001f);
        Assert.AreEqual(1f, SkyBlendWeights.MoonDisc(-40f), 0.001f);
    }

    [Test]
    public void Exposures_StayInSaneRange()
    {
        for (float e = -90f; e <= 90f; e += 1f)
        {
            Assert.That(SkyBlendWeights.DayExposure(e), Is.InRange(0.5f, 1f));
            Assert.That(SkyBlendWeights.GlowExposure(e), Is.InRange(0.6f, 1f));
        }
    }
}
