// 프레임 시간 통계(FrameTimeStats)의 평균, 상위 1% 최악 평균, 빈 입력 처리를 검증하는 EditMode 테스트다.
using System.Collections.Generic;
using NUnit.Framework;

public class FrameTimeStatsTests
{
    [Test]
    public void Average_IsArithmeticMean()
    {
        Assert.AreEqual(20f, FrameTimeStats.Average(new List<float> { 10f, 20f, 30f }), 0.001f);
    }

    [Test]
    public void EmptyOrNull_IsZero()
    {
        Assert.AreEqual(0f, FrameTimeStats.Average(new List<float>()));
        Assert.AreEqual(0f, FrameTimeStats.WorstOnePercentAverage(null));
        Assert.AreEqual(0f, FrameTimeStats.ToFps(0f));
    }

    [Test]
    public void WorstOnePercent_UsesTheSlowestHundredth()
    {
        List<float> samples = new List<float>();
        for (int i = 0; i < 198; i++) samples.Add(10f);
        samples.Add(50f);
        samples.Add(70f);
        // 200개의 1%는 2개: 50과 70의 평균이다.
        Assert.AreEqual(60f, FrameTimeStats.WorstOnePercentAverage(samples), 0.001f);
    }

    [Test]
    public void WorstOnePercent_AtLeastOneSample()
    {
        Assert.AreEqual(30f, FrameTimeStats.WorstOnePercentAverage(new List<float> { 10f, 30f, 20f }), 0.001f);
    }

    [Test]
    public void ToFps_ConvertsMilliseconds()
    {
        Assert.AreEqual(60f, FrameTimeStats.ToFps(1000f / 60f), 0.01f);
    }
}
