// 멀티플레이 참가자 저장 데이터(ParticipantSaveData)의 JSON 왕복과 옛 세이브 하위 호환을 검증하는 EditMode 테스트다.
using NUnit.Framework;
using UnityEngine;

public class ParticipantSaveDataTests
{
    [Test]
    public void LegacySave_HasNoParticipants()
    {
        const string legacy = "{ \"schemaVersion\": 1, \"player\": { \"currentHealth\": 100 } }";
        GameSaveData data = JsonUtility.FromJson<GameSaveData>(legacy);
        Assert.IsNotNull(data.participants);
        Assert.AreEqual(0, data.participants.Count);
    }

    [Test]
    public void RoundTrip_PreservesParticipantInventoryAndBed()
    {
        var data = new GameSaveData();
        var alice = new ParticipantSaveData { playerId = "a1b2c3", respawnBedId = "bed-world-id" };
        alice.inventory.Add(new ItemStackSaveData { definitionId = "scrap_metal", quantity = 6 });
        alice.inventory.Add(new ItemStackSaveData { definitionId = "food_berries", quantity = 4 });
        data.participants.Add(alice);
        data.participants.Add(new ParticipantSaveData { playerId = "d4e5f6" });

        GameSaveData back = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(data));

        Assert.AreEqual(2, back.participants.Count);
        Assert.AreEqual("a1b2c3", back.participants[0].playerId);
        Assert.AreEqual("bed-world-id", back.participants[0].respawnBedId);
        Assert.AreEqual(2, back.participants[0].inventory.Count);
        Assert.AreEqual("scrap_metal", back.participants[0].inventory[0].definitionId);
        Assert.AreEqual(6, back.participants[0].inventory[0].quantity);
        Assert.AreEqual(0, back.participants[1].inventory.Count);
        Assert.IsTrue(string.IsNullOrEmpty(back.participants[1].respawnBedId));
    }

    [Test]
    public void Position_RoundTripsThroughJson_AndOldEntryHasNoPosition()
    {
        var data = new GameSaveData();
        data.participants.Add(new ParticipantSaveData { playerId = "p1", hasPosition = true, positionX = -38.5f, positionY = 24.6f, positionZ = -372.25f, rotationY = 135f });
        data.participants.Add(new ParticipantSaveData { playerId = "p2" });

        GameSaveData back = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(data));
        Assert.IsTrue(back.participants[0].hasPosition);
        Assert.AreEqual(-38.5f, back.participants[0].positionX);
        Assert.AreEqual(24.6f, back.participants[0].positionY);
        Assert.AreEqual(-372.25f, back.participants[0].positionZ);
        Assert.AreEqual(135f, back.participants[0].rotationY);
        Assert.IsFalse(back.participants[1].hasPosition);

        // 8단계, 9단계에서 저장된 기록(위치 필드 없음)은 위치가 없는 것으로 읽힌다.
        ParticipantSaveData old = JsonUtility.FromJson<ParticipantSaveData>("{ \"playerId\": \"p3\", \"hasState\": true }");
        Assert.IsFalse(old.hasPosition);
    }
}
