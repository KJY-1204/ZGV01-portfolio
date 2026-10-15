// 서버 응답 대기 중 중복 요청을 막는 규칙(PendingRequests)을 검증하는 EditMode 테스트다.
using NUnit.Framework;

public class PendingRequestsTests
{
    [Test]
    public void SecondRequestForSameKey_IsRejectedWhilePending()
    {
        var pending = new PendingRequests();
        Assert.IsTrue(pending.TryBegin("scrap_metal", 10f, 1.5f));
        Assert.IsFalse(pending.TryBegin("scrap_metal", 10.5f, 1.5f));
        Assert.IsTrue(pending.IsPending("scrap_metal", 10.5f));
    }

    [Test]
    public void DifferentKeys_AreIndependent()
    {
        var pending = new PendingRequests();
        Assert.IsTrue(pending.TryBegin("scrap_metal", 0f, 1.5f));
        Assert.IsTrue(pending.TryBegin("food_berries", 0.1f, 1.5f));
        Assert.IsFalse(pending.TryBegin("scrap_metal", 0.2f, 1.5f));
    }

    [Test]
    public void Request_ExpiresAfterTimeout_SoALostResponseDoesNotLockForever()
    {
        var pending = new PendingRequests();
        Assert.IsTrue(pending.TryBegin("scrap_metal", 0f, 1.5f));
        Assert.IsFalse(pending.IsPending("scrap_metal", 1.5f));
        Assert.IsTrue(pending.TryBegin("scrap_metal", 1.5f, 1.5f));
    }

    [Test]
    public void Clear_ReleasesEveryKey()
    {
        var pending = new PendingRequests();
        pending.TryBegin("a", 0f, 10f);
        pending.TryBegin("b", 0f, 10f);
        pending.Clear();
        Assert.IsFalse(pending.IsPending("a", 1f));
        Assert.IsTrue(pending.TryBegin("b", 1f, 10f));
    }

    [Test]
    public void NullKey_IsNeverPending()
    {
        Assert.IsFalse(new PendingRequests().IsPending(null, 0f));
    }
}
