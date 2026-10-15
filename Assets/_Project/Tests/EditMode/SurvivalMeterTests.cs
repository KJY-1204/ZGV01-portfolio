// SurvivalMeter의 감소·회복·범위 제한 규칙을 검증하는 EditMode 테스트다.
using NUnit.Framework;

public class SurvivalMeterTests
{
    [Test]
    public void New_StartsFull()
    {
        var m = new SurvivalMeter(100f);
        Assert.AreEqual(100f, m.Current);
        Assert.AreEqual(1f, m.Ratio);
        Assert.IsFalse(m.IsEmpty);
    }

    [Test]
    public void New_ClampsMaxToAtLeastOne()
    {
        Assert.AreEqual(1f, new SurvivalMeter(0f).Max);
        Assert.AreEqual(1f, new SurvivalMeter(-5f).Max);
    }

    [Test]
    public void Drain_ReducesAndStopsAtZero()
    {
        var m = new SurvivalMeter(100f);
        m.Drain(30f);
        Assert.AreEqual(70f, m.Current);
        m.Drain(500f);
        Assert.AreEqual(0f, m.Current);
        Assert.IsTrue(m.IsEmpty);
    }

    [Test]
    public void Restore_IncreasesAndStopsAtMax()
    {
        var m = new SurvivalMeter(100f);
        m.Set(10f);
        m.Restore(40f);
        Assert.AreEqual(50f, m.Current);
        m.Restore(500f);
        Assert.AreEqual(100f, m.Current);
    }

    [Test]
    public void NonPositiveAmounts_AreIgnored()
    {
        var m = new SurvivalMeter(100f);
        m.Set(50f);
        m.Drain(-10f);
        m.Restore(-10f);
        m.Drain(0f);
        Assert.AreEqual(50f, m.Current);
    }

    [Test]
    public void Set_ClampsToRange()
    {
        var m = new SurvivalMeter(100f);
        m.Set(-1f);
        Assert.AreEqual(0f, m.Current);
        m.Set(999f);
        Assert.AreEqual(100f, m.Current);
    }

    [Test]
    public void Ratio_ReflectsCurrentOverMax()
    {
        var m = new SurvivalMeter(200f);
        m.Set(50f);
        Assert.AreEqual(0.25f, m.Ratio, 1e-6f);
    }
}
