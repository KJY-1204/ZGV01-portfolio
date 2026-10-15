// 멀티플레이 시작 위치 분산 규칙(StartSlotLayout)을 검증하는 EditMode 테스트다.
using System.Collections.Generic;
using NUnit.Framework;

public class StartSlotLayoutTests
{
    private const float BodyDiameter = 0.56f;

    private static (float x, float z) At(int slot)
    {
        StartSlotLayout.Offset(slot, out float x, out float z);
        return (x, z);
    }

    [Test]
    public void SlotZero_IsTheHostStartPosition()
    {
        Assert.AreEqual((0f, 0f), At(0));
        Assert.AreEqual((0f, 0f), At(-3));
    }

    [Test]
    public void FirstRing_IsAtFirstRadius()
    {
        for (int slot = 1; slot <= StartSlotLayout.SlotsPerRing; slot++)
        {
            var (x, z) = At(slot);
            Assert.AreEqual(StartSlotLayout.FirstRadius, (float)System.Math.Sqrt(x * x + z * z), 0.001f);
        }
    }

    [Test]
    public void AllSlotsUpToThirtyTwo_AreFarEnoughApart_AndFromTheHost()
    {
        const float minimum = BodyDiameter * 2f;
        for (int a = 0; a <= 32; a++)
        {
            var (ax, az) = At(a);
            for (int b = a + 1; b <= 32; b++)
            {
                var (bx, bz) = At(b);
                float distance = (float)System.Math.Sqrt((ax - bx) * (ax - bx) + (az - bz) * (az - bz));
                Assert.GreaterOrEqual(distance, minimum, $"슬롯 {a}와 {b}가 너무 가깝다({distance:F2}m)");
            }
        }
    }

    [Test]
    public void OuterRings_GrowOutward()
    {
        var (x1, z1) = At(1);
        var (x9, z9) = At(StartSlotLayout.SlotsPerRing + 1);
        float inner = (float)System.Math.Sqrt(x1 * x1 + z1 * z1);
        float outer = (float)System.Math.Sqrt(x9 * x9 + z9 * z9);
        Assert.AreEqual(StartSlotLayout.RingSpacing, outer - inner, 0.001f);
    }

    [Test]
    public void NextFree_PicksSmallestPositiveNotInUse()
    {
        Assert.AreEqual(1, StartSlotLayout.NextFree(new List<int>()));
        Assert.AreEqual(1, StartSlotLayout.NextFree(null));
        Assert.AreEqual(3, StartSlotLayout.NextFree(new List<int> { 1, 2, 4 }));
        Assert.AreEqual(1, StartSlotLayout.NextFree(new List<int> { 0, 2 }));
    }
}
