// 방어구 장착 규칙(Equipment), Health의 피해 감소율/비전투 피해, Inventory.Modified, 세이브의 방어구 목록을 검증하는 EditMode 테스트다.
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class EquipmentTests
{
    private readonly List<Object> created = new List<Object>();
    private GameObject go;
    private Inventory inv;
    private Health health;
    private Equipment equipment;

    private ArmorDefinition Armor(string id, ArmorSlot slot, float reduction)
    {
        var armor = ScriptableObject.CreateInstance<ArmorDefinition>();
        created.Add(armor);
        TestUtil.SetString(armor, "stableId", id);
        TestUtil.SetInt(armor, "maxStack", 1);
        TestUtil.SetEnum(armor, "slot", (int)slot);
        // damageReduction은 Range(0,0.6)이라 직렬화 값은 그대로 들어간다.
        TestUtil.SetFloat(armor, "damageReduction", reduction);
        return armor;
    }

    [SetUp]
    public void SetUp()
    {
        go = new GameObject("player");
        inv = go.AddComponent<Inventory>();
        health = go.AddComponent<Health>();
        equipment = go.AddComponent<Equipment>();   // RequireComponent 두 개가 이미 있다
        TestUtil.Awake(health);
        TestUtil.Awake(equipment);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(go);
        foreach (Object o in created) Object.DestroyImmediate(o);
        created.Clear();
    }

    [Test]
    public void TryEquip_RequiresOwningTheItem()
    {
        ArmorDefinition helmet = Armor("helmet", ArmorSlot.Head, 0.1f);
        Assert.IsFalse(equipment.TryEquip(helmet));
        Assert.IsNull(equipment.Get(ArmorSlot.Head));
        inv.Add(helmet, 1);
        Assert.IsTrue(equipment.TryEquip(helmet));
        Assert.AreSame(helmet, equipment.Get(ArmorSlot.Head));
        Assert.IsTrue(equipment.IsEquipped(helmet));
    }

    [Test]
    public void TryEquip_Null_Fails()
    {
        Assert.IsFalse(equipment.TryEquip(null));
    }

    [Test]
    public void Equipping_AppliesReductionToHealth()
    {
        ArmorDefinition vest = Armor("vest", ArmorSlot.Body, 0.2f);
        inv.Add(vest, 1);
        equipment.TryEquip(vest);
        Assert.AreEqual(0.2f, equipment.TotalReduction, 1e-5f);
        Assert.AreEqual(0.2f, health.DamageReduction, 1e-5f);
        health.ApplyDamage(50f);
        Assert.AreEqual(60f, health.CurrentHealth, 1e-4f);   // 50 * (1 - 0.2) = 40
    }

    [Test]
    public void ReductionsFromAllSlots_AreSummed()
    {
        ArmorDefinition a = Armor("h", ArmorSlot.Head, 0.1f), b = Armor("b", ArmorSlot.Body, 0.2f), c = Armor("l", ArmorSlot.Legs, 0.15f);
        foreach (var armor in new[] { a, b, c }) { inv.Add(armor, 1); equipment.TryEquip(armor); }
        Assert.AreEqual(0.45f, equipment.TotalReduction, 1e-5f);
    }

    [Test]
    public void TotalReduction_IsCapped()
    {
        ArmorDefinition a = Armor("h", ArmorSlot.Head, 0.4f), b = Armor("b", ArmorSlot.Body, 0.4f), c = Armor("l", ArmorSlot.Legs, 0.4f);
        foreach (var armor in new[] { a, b, c }) { inv.Add(armor, 1); equipment.TryEquip(armor); }
        Assert.AreEqual(Equipment.MaxReduction, equipment.TotalReduction, 1e-5f);
        Assert.AreEqual(Equipment.MaxReduction, health.DamageReduction, 1e-5f);
    }

    [Test]
    public void EquippingSameSlot_ReplacesPrevious()
    {
        ArmorDefinition old = Armor("old", ArmorSlot.Head, 0.08f), better = Armor("new", ArmorSlot.Head, 0.14f);
        inv.Add(old, 1); inv.Add(better, 1);
        equipment.TryEquip(old);
        equipment.TryEquip(better);
        Assert.AreSame(better, equipment.Get(ArmorSlot.Head));
        Assert.IsFalse(equipment.IsEquipped(old));
        Assert.AreEqual(0.14f, equipment.TotalReduction, 1e-5f);
    }

    [Test]
    public void Unequip_ClearsSlotAndReduction_AndKeepsItemInInventory()
    {
        ArmorDefinition vest = Armor("vest", ArmorSlot.Body, 0.2f);
        inv.Add(vest, 1);
        equipment.TryEquip(vest);
        equipment.Unequip(ArmorSlot.Body);
        Assert.IsNull(equipment.Get(ArmorSlot.Body));
        Assert.AreEqual(0f, health.DamageReduction);
        Assert.AreEqual(1, inv.GetQuantity(vest));
    }

    [Test]
    public void RemovingItemFromInventory_AutoUnequipsAndDropsReduction()
    {
        ArmorDefinition vest = Armor("vest", ArmorSlot.Body, 0.2f);
        inv.Add(vest, 1);
        equipment.TryEquip(vest);
        int changed = 0;
        equipment.Changed += () => changed++;
        inv.Remove(vest, 1);
        Assert.IsNull(equipment.Get(ArmorSlot.Body));
        Assert.AreEqual(0f, health.DamageReduction);
        Assert.AreEqual(1, changed);
    }

    [Test]
    public void ClearingInventory_UnequipsEverything()
    {
        ArmorDefinition a = Armor("h", ArmorSlot.Head, 0.1f), b = Armor("b", ArmorSlot.Body, 0.2f);
        foreach (var armor in new[] { a, b }) { inv.Add(armor, 1); equipment.TryEquip(armor); }
        inv.Clear();
        Assert.AreEqual(0f, equipment.TotalReduction);
        Assert.IsNull(equipment.Get(ArmorSlot.Head));
        Assert.IsNull(equipment.Get(ArmorSlot.Body));
    }

    [Test]
    public void Changed_FiresOnEquipAndUnequip()
    {
        ArmorDefinition vest = Armor("vest", ArmorSlot.Body, 0.2f);
        inv.Add(vest, 1);
        int changed = 0;
        equipment.Changed += () => changed++;
        equipment.TryEquip(vest);
        equipment.Unequip(ArmorSlot.Body);
        equipment.Unequip(ArmorSlot.Body);   // 이미 비어 있으면 알리지 않는다
        Assert.AreEqual(2, changed);
    }

    [Test]
    public void RestoreState_EquipsOnlyOwnedItems_AndReplacesCurrent()
    {
        ArmorDefinition worn = Armor("worn", ArmorSlot.Head, 0.1f), owned = Armor("owned", ArmorSlot.Body, 0.2f), missing = Armor("missing", ArmorSlot.Legs, 0.15f);
        inv.Add(worn, 1); inv.Add(owned, 1);
        equipment.TryEquip(worn);
        equipment.RestoreState(new[] { owned, missing });
        Assert.IsNull(equipment.Get(ArmorSlot.Head));        // 이전 장착은 풀린다
        Assert.AreSame(owned, equipment.Get(ArmorSlot.Body));
        Assert.IsNull(equipment.Get(ArmorSlot.Legs));        // 없는 것은 장착하지 않는다
        Assert.AreEqual(0.2f, equipment.TotalReduction, 1e-5f);
    }

    [Test]
    public void FindArmor_ReturnsNullForNonArmorItems()
    {
        ItemDefinition plain = TestItems.Item("plain", 5);
        created.Add(plain);
        Assert.IsNull(equipment.FindArmor(plain));
        ArmorDefinition vest = Armor("vest", ArmorSlot.Body, 0.2f);
        Assert.AreSame(vest, equipment.FindArmor(vest));
    }

    [Test]
    public void TrueDamage_IgnoresArmor()
    {
        ArmorDefinition vest = Armor("vest", ArmorSlot.Body, 0.5f);
        inv.Add(vest, 1);
        equipment.TryEquip(vest);
        health.ApplyTrueDamage(30f);
        Assert.AreEqual(70f, health.CurrentHealth, 1e-4f);
    }
}

public class HealthDamageReductionTests
{
    private GameObject go;
    private Health health;

    [SetUp]
    public void SetUp()
    {
        go = new GameObject("h");
        health = go.AddComponent<Health>();
        TestUtil.Awake(health);
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(go);

    [Test]
    public void DefaultReduction_IsZero()
    {
        health.ApplyDamage(10f);
        Assert.AreEqual(90f, health.CurrentHealth);
    }

    [Test]
    public void Reduction_ScalesDamage_AndEventReportsEffectiveAmount()
    {
        health.DamageReduction = 0.25f;
        float reported = 0f;
        health.Damaged += (amount, _) => reported = amount;
        health.ApplyDamage(40f);
        Assert.AreEqual(70f, health.CurrentHealth, 1e-4f);
        Assert.AreEqual(30f, reported, 1e-4f);
    }

    [Test]
    public void DirectionalOverload_AlsoUsesReduction()
    {
        health.DamageReduction = 0.5f;
        health.ApplyDamage(20f, Vector3.forward, false);
        Assert.AreEqual(90f, health.CurrentHealth, 1e-4f);
    }

    [Test]
    public void Reduction_IsClampedToValidRange()
    {
        health.DamageReduction = 5f;
        Assert.AreEqual(Health.MaxDamageReduction, health.DamageReduction);
        health.DamageReduction = -1f;
        Assert.AreEqual(0f, health.DamageReduction);
    }

    [Test]
    public void TrueDamage_BypassesReduction_AndCanKill()
    {
        health.DamageReduction = 0.9f;
        health.ApplyTrueDamage(100f);
        Assert.IsTrue(health.IsDead);
    }

    [Test]
    public void Reduction_NeverMakesDamageNegative_OrHealing()
    {
        health.DamageReduction = Health.MaxDamageReduction;
        health.ApplyDamage(10f);
        Assert.Less(health.CurrentHealth, 100f);
        Assert.GreaterOrEqual(health.CurrentHealth, 0f);
    }
}

public class InventoryModifiedTests
{
    [Test]
    public void Modified_FiresOnAddRemoveClear_ButNotOnFailedRemove()
    {
        var go = new GameObject("inv");
        var inv = go.AddComponent<Inventory>();
        ItemDefinition item = TestItems.Item("x", 10);
        int fired = 0;
        inv.Modified += () => fired++;

        inv.Add(item, 3);          // 1
        inv.Remove(item, 1);       // 2
        inv.Remove(item, 99);      // 부족해서 실패: 알리지 않는다
        inv.Clear();               // 3
        Assert.AreEqual(3, fired);

        Object.DestroyImmediate(go);
        Object.DestroyImmediate(item);
    }
}

public class ArmorSaveDataTests
{
    [Test]
    public void LegacySave_HasNoEquippedArmor()
    {
        PlayerSaveData player = JsonUtility.FromJson<GameSaveData>("{ \"player\": { \"currentHealth\": 80 } }").player;
        Assert.IsNotNull(player.equippedArmorIds);
        Assert.AreEqual(0, player.equippedArmorIds.Count);
    }

    [Test]
    public void RoundTrip_PreservesEquippedArmorIds()
    {
        var data = new GameSaveData();
        data.player.equippedArmorIds.Add("armor_leather_cap");
        data.player.equippedArmorIds.Add("armor_iron_chest");
        GameSaveData back = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(data));
        CollectionAssert.AreEqual(new[] { "armor_leather_cap", "armor_iron_chest" }, back.player.equippedArmorIds);
    }
}
