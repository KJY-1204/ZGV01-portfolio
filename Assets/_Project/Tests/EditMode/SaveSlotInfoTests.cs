// 저장 슬롯 요약(비어 있음, 일차, 시각, 저장 시각 문구)과 옛 세이브의 하위 호환을 검증하는 EditMode 테스트다.
using NUnit.Framework;
using UnityEngine;

public class SaveSlotInfoTests
{
    [Test]
    public void Empty_DescribesAsEmpty()
    {
        Assert.IsFalse(SaveSlotInfo.Empty.exists);
        Assert.AreEqual("비어 있음", SaveSlotInfo.Empty.Describe());
    }

    [Test]
    public void NullData_IsEmpty()
    {
        Assert.IsFalse(SaveSlotInfo.FromData(null).exists);
    }

    [Test]
    public void FullData_ShowsDayClockAndSavedTime()
    {
        GameSaveData data = new GameSaveData { elapsedGameHours = 51f, timeOfDay = 14.5f, savedAt = "2026-10-10 20:12" };
        SaveSlotInfo info = SaveSlotInfo.FromData(data);

        Assert.IsTrue(info.exists);
        Assert.AreEqual(3, info.day);
        Assert.AreEqual("3일째 14:30  ·  2026-10-10 20:12", info.Describe());
    }

    [Test]
    public void LegacyData_WithoutExtras_StillDescribesAsSaved()
    {
        SaveSlotInfo info = SaveSlotInfo.FromData(new GameSaveData());

        Assert.IsTrue(info.exists);
        Assert.AreEqual("저장됨", info.Describe());
    }

    [Test]
    public void SavedAt_RoundTripsThroughJson_AndLegacyIsEmpty()
    {
        GameSaveData data = new GameSaveData { savedAt = "2026-10-10 20:12" };
        Assert.AreEqual("2026-10-10 20:12", JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(data)).savedAt);
        Assert.AreEqual("", JsonUtility.FromJson<GameSaveData>("{\"schemaVersion\":1}").savedAt);
    }

    [Test]
    public void Clock_WrapsAtMidnight()
    {
        SaveSlotInfo info = new SaveSlotInfo(true, 1, 24f, "");
        Assert.AreEqual("1일째 00:00", info.Describe());
    }
}
