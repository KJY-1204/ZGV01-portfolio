// 멀티플레이 참가자용 인벤토리 복제본 모드(직접 변경 거부, 서버 내용으로 통째 교체)를 검증하는 EditMode 테스트다.
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class InventoryReplicaTests
{
    private GameObject go;
    private Inventory inv;
    private ItemDefinition wood;
    private ItemDefinition gun;

    [SetUp]
    public void SetUp()
    {
        go = new GameObject("inv");
        inv = go.AddComponent<Inventory>();
        wood = TestItems.Item("wood", 10);
        gun = TestItems.Item("gun", 1);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(wood);
        Object.DestroyImmediate(gun);
    }

    [Test]
    public void Replica_RejectsDirectAddRemoveClearAndPickup()
    {
        inv.Add(wood, 5);
        inv.IsReplica = true;

        inv.Add(wood, 3);
        inv.AddPickup(gun, 1);
        Assert.IsFalse(inv.Remove(wood, 1));
        inv.Clear();

        Assert.AreEqual(5, inv.GetQuantity(wood));
        Assert.AreEqual(0, inv.GetQuantity(gun));
    }

    [Test]
    public void ReplaceAll_ReplacesContentsAndRaisesModifiedOnce()
    {
        inv.IsReplica = true;
        int modified = 0;
        inv.Modified += () => modified++;

        inv.ReplaceAll(new List<(ItemDefinition, int)> { (wood, 14), (gun, 1) });

        Assert.AreEqual(14, inv.GetQuantity(wood));
        Assert.AreEqual(1, inv.GetQuantity(gun));
        // 최대 스택 10이라 나무는 두 스택(10 + 4), 총은 한 스택이다.
        Assert.AreEqual(3, inv.Stacks.Count);
        Assert.AreEqual(1, modified);

        inv.ReplaceAll(new List<(ItemDefinition, int)> { (gun, 1) });
        Assert.AreEqual(0, inv.GetQuantity(wood));
        Assert.AreEqual(2, modified);
    }

    [Test]
    public void ReplaceAll_SkipsNullAndNonPositive()
    {
        inv.ReplaceAll(new List<(ItemDefinition, int)> { (null, 5), (wood, 0), (wood, -2), (wood, 3) });
        Assert.AreEqual(3, inv.GetQuantity(wood));
        Assert.AreEqual(1, inv.Stacks.Count);
    }

    [Test]
    public void NotifyPickedUp_RaisesEventWithoutChangingContents()
    {
        ItemDefinition seen = null;
        int seenAmount = 0;
        inv.ItemPickedUp += (item, amount) => { seen = item; seenAmount = amount; };

        inv.NotifyPickedUp(wood, 2);

        Assert.AreSame(wood, seen);
        Assert.AreEqual(2, seenAmount);
        Assert.AreEqual(0, inv.GetQuantity(wood));
    }

    [Test]
    public void NormalInventory_IsNotAReplicaByDefault()
    {
        Assert.IsFalse(inv.IsReplica);
        inv.Add(wood, 2);
        Assert.AreEqual(2, inv.GetQuantity(wood));
    }
}
