// 저장된 플레이어 위치가 땅속에 묻힌 옛 좌표인지 판단하는 규칙(SavedPositionRule)의 EditMode 테스트다.
using NUnit.Framework;

public class SavedPositionRuleTests
{
    [Test]
    public void BelowTerrainWithoutFloor_IsBuried()
    {
        Assert.IsTrue(SavedPositionRule.IsBuried(-0.01f, 24.5f, false));
    }

    [Test]
    public void BelowTerrainWithFloor_IsNotBuried_LikeAnUndergroundMine()
    {
        Assert.IsFalse(SavedPositionRule.IsBuried(-12f, 24.5f, true));
    }

    [Test]
    public void OnOrAboveTerrain_IsNotBuried()
    {
        Assert.IsFalse(SavedPositionRule.IsBuried(24.6f, 24.5f, false));
        Assert.IsFalse(SavedPositionRule.IsBuried(80f, 24.5f, false));
    }

    [Test]
    public void JustUnderTerrainWithinMargin_IsNotBuried()
    {
        Assert.IsFalse(SavedPositionRule.IsBuried(24.0f, 24.5f, false));
    }

    [Test]
    public void NonFiniteHeight_IsBuried_ButNaNTerrainIsIgnored()
    {
        Assert.IsTrue(SavedPositionRule.IsBuried(float.NaN, 24.5f, true));
        Assert.IsTrue(SavedPositionRule.IsBuried(float.NegativeInfinity, 24.5f, true));
        Assert.IsFalse(SavedPositionRule.IsBuried(-5f, float.NaN, false));
    }
}
