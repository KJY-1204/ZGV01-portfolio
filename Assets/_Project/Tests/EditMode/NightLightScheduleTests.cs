// NightLightSchedule(밤 조명 켜짐 시간표)과 랜턴 세이브 하위 호환을 검증하는 EditMode 테스트다.
using NUnit.Framework;
using UnityEngine;

public class NightLightScheduleTests
{
    [TestCase(12f)]
    [TestCase(8f)]
    [TestCase(17f)]
    [TestCase(18.4f)]
    [TestCase(6.5f)]
    [TestCase(6.6f)]
    public void Daytime_IsOff(float hour)
    {
        Assert.AreEqual(0f, NightLightSchedule.Intensity(hour), 0.0001f);
    }

    [TestCase(19.5f)]
    [TestCase(22f)]
    [TestCase(0f)]
    [TestCase(3f)]
    [TestCase(5.5f)]
    public void Night_IsFullyOn(float hour)
    {
        Assert.AreEqual(1f, NightLightSchedule.Intensity(hour), 0.0001f);
    }

    [Test]
    public void Evening_RampsUpMonotonically()
    {
        float previous = -1f;
        for (float h = 18.5f; h <= 19.5f; h += 0.05f)
        {
            float value = NightLightSchedule.Intensity(h);
            Assert.GreaterOrEqual(value, previous - 0.0001f);
            previous = value;
        }
        Assert.Greater(NightLightSchedule.Intensity(19f), 0.3f);
        Assert.Less(NightLightSchedule.Intensity(19f), 0.7f);
    }

    [Test]
    public void Dawn_RampsDownMonotonically()
    {
        float previous = 2f;
        for (float h = 5.5f; h <= 6.5f; h += 0.05f)
        {
            float value = NightLightSchedule.Intensity(h);
            Assert.LessOrEqual(value, previous + 0.0001f);
            previous = value;
        }
    }

    [Test]
    public void Hour_WrapsAround24()
    {
        Assert.AreEqual(NightLightSchedule.Intensity(1f), NightLightSchedule.Intensity(25f), 0.0001f);
        Assert.AreEqual(NightLightSchedule.Intensity(12f), NightLightSchedule.Intensity(-12f), 0.0001f);
    }

    [Test]
    public void PlayerSaveData_LanternOn_RoundTripsAndLegacyIsOff()
    {
        PlayerSaveData data = new PlayerSaveData { lanternOn = true };
        Assert.IsTrue(JsonUtility.FromJson<PlayerSaveData>(JsonUtility.ToJson(data)).lanternOn);
        Assert.IsFalse(JsonUtility.FromJson<PlayerSaveData>("{\"currentHealth\":50}").lanternOn);
    }
}
