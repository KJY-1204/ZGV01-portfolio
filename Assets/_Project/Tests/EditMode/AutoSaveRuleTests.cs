// 자동 저장 규칙(AutoSaveRule)의 주기와 저장 가능 조건을 검증하는 EditMode 테스트다.
using NUnit.Framework;

public class AutoSaveRuleTests
{
    [Test]
    public void Interval_IsAboutTenMinutes()
    {
        Assert.AreEqual(600f, AutoSaveRule.IntervalSeconds);
    }

    [Test]
    public void IsDue_OnlyAfterTheInterval()
    {
        Assert.IsFalse(AutoSaveRule.IsDue(599.9f, 0f));
        Assert.IsTrue(AutoSaveRule.IsDue(600f, 0f));
        Assert.IsTrue(AutoSaveRule.IsDue(1300f, 700f));
        Assert.IsFalse(AutoSaveRule.IsDue(1299f, 700f));
    }

    [Test]
    public void IsDue_UsesTheGivenInterval()
    {
        Assert.IsTrue(AutoSaveRule.IsDue(5f, 0f, 5f));
        Assert.IsFalse(AutoSaveRule.IsDue(4.9f, 0f, 5f));
    }

    [Test]
    public void CanSave_AliveHostOrSinglePlayerInGameScene()
    {
        Assert.IsTrue(AutoSaveRule.CanSave(false, false, true, false));
    }

    [Test]
    public void CanSave_IsFalseInMenuForClientsWithoutPlayerOrWhenDead()
    {
        Assert.IsFalse(AutoSaveRule.CanSave(true, false, true, false));
        Assert.IsFalse(AutoSaveRule.CanSave(false, true, true, false));
        Assert.IsFalse(AutoSaveRule.CanSave(false, false, false, false));
        Assert.IsFalse(AutoSaveRule.CanSave(false, false, true, true));
    }
}
