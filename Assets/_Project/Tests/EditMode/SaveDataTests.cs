// 세이브 데이터의 JSON 왕복과 옛 세이브 파일의 하위 호환 기본값을 검증하는 EditMode 테스트다.
using NUnit.Framework;
using UnityEngine;

public class SaveDataTests
{
    // 2026-09-23에 실제로 저장된 세이브 파일이다(생존/무기/문/내구도/시각 필드가 없던 시절).
    private const string LegacySave = @"{
        ""schemaVersion"": 1,
        ""player"": {
            ""positionX"": 0.0, ""positionY"": -0.01, ""positionZ"": 0.0, ""rotationY"": 0.0,
            ""currentHealth"": 100.0,
            ""inventory"": [
                { ""definitionId"": ""basic_building_material"", ""quantity"": 5 },
                { ""definitionId"": ""refined_material"", ""quantity"": 8 }
            ]
        },
        ""buildables"": [
            { ""worldId"": ""2d052fe9242c45c6bd67cea1774473e4"", ""buildableId"": ""wall_basic"",
              ""positionX"": 0.0, ""positionY"": 0.0, ""positionZ"": 4.0, ""rotationY"": 0.0 }
        ]
    }";

    [Test]
    public void LegacySave_Parses_AndKeepsKnownFields()
    {
        GameSaveData data = JsonUtility.FromJson<GameSaveData>(LegacySave);
        Assert.IsNotNull(data);
        Assert.AreEqual(100f, data.player.currentHealth);
        Assert.AreEqual(2, data.player.inventory.Count);
        Assert.AreEqual("refined_material", data.player.inventory[1].definitionId);
        Assert.AreEqual(8, data.player.inventory[1].quantity);
        Assert.AreEqual(1, data.buildables.Count);
        Assert.AreEqual("wall_basic", data.buildables[0].buildableId);
    }

    [Test]
    public void LegacySave_MissingSurvival_LoadsAsNotSaved()
    {
        GameSaveData data = JsonUtility.FromJson<GameSaveData>(LegacySave);
        Assert.IsFalse(data.player.hasSurvival); // 로더는 이때 허기/갈증을 건드리지 않는다(가득 찬 상태 유지)
    }

    [Test]
    public void LegacySave_MissingWeaponFields_MeansUnarmedWithNoMagazines()
    {
        GameSaveData data = JsonUtility.FromJson<GameSaveData>(LegacySave);
        Assert.IsTrue(string.IsNullOrEmpty(data.player.equippedWeaponId));
        Assert.IsTrue(string.IsNullOrEmpty(data.player.lastWeaponId));
        Assert.AreEqual(0, data.player.magazines.Count);
    }

    [Test]
    public void LegacySave_MissingBuildableFields_UseSafeDefaults()
    {
        BuildableSaveData b = JsonUtility.FromJson<GameSaveData>(LegacySave).buildables[0];
        Assert.IsFalse(b.doorOpen);
        Assert.AreEqual(1f, b.doorSide);
        Assert.Less(b.health, 0f);          // 음수 = 가득 찬 내구도
        Assert.IsTrue(string.IsNullOrEmpty(b.supportId));
    }

    [Test]
    public void LegacySave_MissingTimeOfDay_IsNegative()
    {
        Assert.Less(JsonUtility.FromJson<GameSaveData>(LegacySave).timeOfDay, 0f);
    }

    [Test]
    public void RoundTrip_PreservesAllFields()
    {
        var data = new GameSaveData { timeOfDay = 18.5f };
        data.player.positionX = 1.5f;
        data.player.positionY = 2f;
        data.player.positionZ = -3f;
        data.player.rotationY = 90f;
        data.player.currentHealth = 42f;
        data.player.inventory.Add(new ItemStackSaveData { definitionId = "wood", quantity = 7 });
        data.player.equippedWeaponId = "ak47";
        data.player.lastWeaponId = "mp5";
        data.player.magazines.Add(new MagazineSaveData { weaponId = "ak47", rounds = 12 });
        data.player.hasSurvival = true;
        data.player.hunger = 33f;
        data.player.thirst = 66f;
        data.buildables.Add(new BuildableSaveData
        {
            worldId = "w1", buildableId = "door_basic", positionX = 4f, positionY = 0f, positionZ = 8f, rotationY = 180f,
            doorOpen = true, doorSide = -1f, health = 55f, supportId = "w0"
        });

        GameSaveData back = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(data, true));

        Assert.AreEqual(1, back.schemaVersion);
        Assert.AreEqual(18.5f, back.timeOfDay);
        Assert.AreEqual(1.5f, back.player.positionX);
        Assert.AreEqual(-3f, back.player.positionZ);
        Assert.AreEqual(90f, back.player.rotationY);
        Assert.AreEqual(42f, back.player.currentHealth);
        Assert.AreEqual("wood", back.player.inventory[0].definitionId);
        Assert.AreEqual(7, back.player.inventory[0].quantity);
        Assert.AreEqual("ak47", back.player.equippedWeaponId);
        Assert.AreEqual("mp5", back.player.lastWeaponId);
        Assert.AreEqual(12, back.player.magazines[0].rounds);
        Assert.IsTrue(back.player.hasSurvival);
        Assert.AreEqual(33f, back.player.hunger);
        Assert.AreEqual(66f, back.player.thirst);

        BuildableSaveData b = back.buildables[0];
        Assert.AreEqual("w1", b.worldId);
        Assert.AreEqual("door_basic", b.buildableId);
        Assert.IsTrue(b.doorOpen);
        Assert.AreEqual(-1f, b.doorSide);
        Assert.AreEqual(55f, b.health);
        Assert.AreEqual("w0", b.supportId);
    }

    [Test]
    public void NewSave_UsesStableIdsNotInstanceIds()
    {
        var data = new GameSaveData();
        data.player.inventory.Add(new ItemStackSaveData { definitionId = "wood", quantity = 1 });
        string json = JsonUtility.ToJson(data);
        StringAssert.Contains("definitionId", json);
        StringAssert.DoesNotContain("instanceID", json);
    }

    [Test]
    public void MalformedJson_Throws_SoLoaderCanReportIt()
    {
        Assert.Throws<System.ArgumentException>(() => JsonUtility.FromJson<GameSaveData>("{ not json"));
    }
}

public class AnimalSaveDataTests
{
    [Test]
    public void LegacySave_HasNoAnimals_AndIsNotMarkedAsSaved()
    {
        const string legacy = "{ \"schemaVersion\": 1, \"player\": { \"currentHealth\": 100 } }";
        GameSaveData data = JsonUtility.FromJson<GameSaveData>(legacy);
        Assert.IsFalse(data.hasAnimals);       // 로더는 이때 지금 있는 동물을 건드리지 않는다
        Assert.AreEqual(0, data.animals.Count);
    }

    [Test]
    public void RoundTrip_PreservesAnimals()
    {
        var data = new GameSaveData { hasAnimals = true };
        data.animals.Add(new AnimalSaveData { definitionId = "animal_pig", positionX = 1.5f, positionY = 0.08f, positionZ = -4f, rotationY = 135f, health = 22f });
        data.animals.Add(new AnimalSaveData { definitionId = "predator_tiger", positionX = 30f, positionY = 0f, positionZ = 12f, rotationY = 10f, health = 120f });

        GameSaveData back = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(data));

        Assert.IsTrue(back.hasAnimals);
        Assert.AreEqual(2, back.animals.Count);
        Assert.AreEqual("animal_pig", back.animals[0].definitionId);
        Assert.AreEqual(1.5f, back.animals[0].positionX);
        Assert.AreEqual(-4f, back.animals[0].positionZ);
        Assert.AreEqual(135f, back.animals[0].rotationY);
        Assert.AreEqual(22f, back.animals[0].health);
        Assert.AreEqual("predator_tiger", back.animals[1].definitionId);
        Assert.AreEqual(120f, back.animals[1].health);
    }

    [Test]
    public void EmptyButMarkedSave_MeansNoAnimalsAlive()
    {
        // 동물이 하나도 없는 상태로 저장했으면(hasAnimals true, 목록 비어 있음) 불러올 때 모두 치워야 한다.
        var data = new GameSaveData { hasAnimals = true };
        GameSaveData back = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(data));
        Assert.IsTrue(back.hasAnimals);
        Assert.AreEqual(0, back.animals.Count);
    }
}
