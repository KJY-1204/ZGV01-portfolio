// 참가자 상태 보고 값을 걸러 내는 규칙(ParticipantStateRule)과 참가자 상태 저장 필드의 JSON 호환을 검증하는 EditMode 테스트다.
using NUnit.Framework;
using UnityEngine;

public class ParticipantStateRuleTests
{
    [Test]
    public void ReportWithoutState_IsRejected()
    {
        Assert.IsFalse(ParticipantStateRule.Sanitize(new ParticipantSaveData { hasState = false }));
        Assert.IsFalse(ParticipantStateRule.Sanitize(null));
    }

    [Test]
    public void ValidReport_IsKeptAsIs()
    {
        var data = new ParticipantSaveData { hasState = true, equippedWeaponId = "weapon_ak47", lastWeaponId = "weapon_ak47", hasSurvival = true, hunger = 40f, thirst = 25.5f };
        data.magazines.Add(new MagazineSaveData { weaponId = "weapon_ak47", rounds = 17 });
        data.equippedArmorIds.Add("armor_iron_helmet");

        Assert.IsTrue(ParticipantStateRule.Sanitize(data));
        Assert.AreEqual("weapon_ak47", data.equippedWeaponId);
        Assert.AreEqual(17, data.magazines[0].rounds);
        Assert.AreEqual(1, data.equippedArmorIds.Count);
        Assert.AreEqual(40f, data.hunger);
        Assert.AreEqual(25.5f, data.thirst);
    }

    [Test]
    public void BrokenMeters_AreClamped()
    {
        var data = new ParticipantSaveData { hasState = true, hasSurvival = true, hunger = float.NaN, thirst = -5f };
        ParticipantStateRule.Sanitize(data);
        Assert.AreEqual(0f, data.hunger);
        Assert.AreEqual(0f, data.thirst);

        data.hunger = float.PositiveInfinity;
        data.thirst = 1e9f;
        ParticipantStateRule.Sanitize(data);
        Assert.AreEqual(0f, data.hunger);
        Assert.AreEqual(ParticipantStateRule.MaxMeter, data.thirst);
    }

    [Test]
    public void LongIdsAndHugeLists_AreTrimmed()
    {
        var data = new ParticipantSaveData { hasState = true, equippedWeaponId = new string('x', 500) };
        for (int i = 0; i < 100; i++)
        {
            data.magazines.Add(new MagazineSaveData { weaponId = "w" + i, rounds = -3 });
            data.equippedArmorIds.Add("a" + i);
        }
        data.magazines.Add(null);
        data.equippedArmorIds.Add("a1"); // 중복
        data.equippedArmorIds.Add("");

        ParticipantStateRule.Sanitize(data);

        Assert.AreEqual("", data.equippedWeaponId);
        Assert.AreEqual(ParticipantStateRule.MaxMagazines, data.magazines.Count);
        Assert.AreEqual(0, data.magazines[0].rounds);
        Assert.AreEqual(ParticipantStateRule.MaxArmor, data.equippedArmorIds.Count);
    }

    [Test]
    public void StateFields_RoundTripThroughJson_AndOldParticipantEntryHasNoState()
    {
        var data = new ParticipantSaveData { playerId = "p1", hasState = true, equippedWeaponId = "weapon_ak47", hasSurvival = true, hunger = 12f, thirst = 7f };
        data.equippedArmorIds.Add("armor_iron_plate");
        data.magazines.Add(new MagazineSaveData { weaponId = "weapon_ak47", rounds = 9 });

        ParticipantSaveData back = JsonUtility.FromJson<ParticipantSaveData>(JsonUtility.ToJson(data));
        Assert.IsTrue(back.hasState);
        Assert.AreEqual("weapon_ak47", back.equippedWeaponId);
        Assert.AreEqual("armor_iron_plate", back.equippedArmorIds[0]);
        Assert.AreEqual(9, back.magazines[0].rounds);
        Assert.AreEqual(12f, back.hunger);

        // 8단계에서 저장된 기록(상태 필드 없음)은 상태가 없는 것으로 읽힌다.
        ParticipantSaveData old = JsonUtility.FromJson<ParticipantSaveData>("{ \"playerId\": \"p2\", \"inventory\": [] }");
        Assert.IsFalse(old.hasState);
        Assert.IsFalse(old.hasSurvival);
        Assert.AreEqual(0, old.magazines.Count);
    }

    [Test]
    public void Health_IsClamped_AndRoundTripsThroughJson_AndOldEntryHasNone()
    {
        var data = new ParticipantSaveData { hasState = true, hasHealth = true, health = float.NaN };
        ParticipantStateRule.Sanitize(data);
        Assert.AreEqual(0f, data.health);

        data.health = 1e9f;
        ParticipantStateRule.Sanitize(data);
        Assert.AreEqual(ParticipantStateRule.MaxMeter, data.health);

        var report = new ParticipantSaveData { hasState = true, hasHealth = true, health = 42.5f };
        ParticipantSaveData back = JsonUtility.FromJson<ParticipantSaveData>(JsonUtility.ToJson(report));
        Assert.IsTrue(back.hasHealth);
        Assert.AreEqual(42.5f, back.health);

        // 9단계까지 저장된 기록(체력 필드 없음)은 체력이 없는 것으로 읽힌다.
        ParticipantSaveData old = JsonUtility.FromJson<ParticipantSaveData>("{ \"playerId\": \"p\", \"hasState\": true, \"hasSurvival\": true }");
        Assert.IsFalse(old.hasHealth);
    }
}
