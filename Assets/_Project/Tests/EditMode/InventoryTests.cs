// Inventory의 스택 병합/분할, 차감, 수량 조회 규칙을 검증하는 EditMode 테스트다.
using NUnit.Framework;
using UnityEngine;

public class InventoryTests
{
    private GameObject go;
    private Inventory inv;
    private ItemDefinition wood;   // 최대 스택 10
    private ItemDefinition gun;    // 최대 스택 1

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
    public void Add_FillsExistingStackBeforeCreatingNew()
    {
        inv.Add(wood, 6);
        inv.Add(wood, 3);
        Assert.AreEqual(1, inv.Stacks.Count);
        Assert.AreEqual(9, inv.Stacks[0].Quantity);
    }

    [Test]
    public void Add_OverflowSplitsAcrossStacks()
    {
        inv.Add(wood, 25);
        Assert.AreEqual(3, inv.Stacks.Count);
        Assert.AreEqual(10, inv.Stacks[0].Quantity);
        Assert.AreEqual(10, inv.Stacks[1].Quantity);
        Assert.AreEqual(5, inv.Stacks[2].Quantity);
        Assert.AreEqual(25, inv.GetQuantity(wood));
    }

    [Test]
    public void Add_TopsUpPartialStackThenOverflows()
    {
        inv.Add(wood, 8);
        inv.Add(wood, 5);
        Assert.AreEqual(2, inv.Stacks.Count);
        Assert.AreEqual(10, inv.Stacks[0].Quantity);
        Assert.AreEqual(3, inv.Stacks[1].Quantity);
    }

    [Test]
    public void Add_MaxStackOne_CreatesStackPerItem()
    {
        inv.Add(gun, 3);
        Assert.AreEqual(3, inv.Stacks.Count);
    }

    [Test]
    public void Add_IgnoresNullAndNonPositive()
    {
        inv.Add(null, 5);
        inv.Add(wood, 0);
        inv.Add(wood, -2);
        Assert.AreEqual(0, inv.Stacks.Count);
    }

    [Test]
    public void Add_RaisesItemAdded_ButNotPickedUp()
    {
        int added = 0, picked = 0;
        inv.ItemAdded += (_, q) => added += q;
        inv.ItemPickedUp += (_, q) => picked += q;
        inv.Add(wood, 4);
        Assert.AreEqual(4, added);
        Assert.AreEqual(0, picked);
    }

    [Test]
    public void AddPickup_RaisesBothEvents()
    {
        int added = 0, picked = 0;
        inv.ItemAdded += (_, q) => added += q;
        inv.ItemPickedUp += (_, q) => picked += q;
        inv.AddPickup(wood, 4);
        Assert.AreEqual(4, added);
        Assert.AreEqual(4, picked);
    }

    [Test]
    public void Remove_TakesFromLastStackFirstAndDropsEmptyStacks()
    {
        inv.Add(wood, 15); // 10 + 5
        Assert.IsTrue(inv.Remove(wood, 7));
        Assert.AreEqual(1, inv.Stacks.Count);
        Assert.AreEqual(8, inv.Stacks[0].Quantity);
    }

    [Test]
    public void Remove_InsufficientQuantity_FailsAndChangesNothing()
    {
        inv.Add(wood, 5);
        Assert.IsFalse(inv.Remove(wood, 6));
        Assert.AreEqual(5, inv.GetQuantity(wood));
    }

    [Test]
    public void Remove_InvalidArguments_Fail()
    {
        inv.Add(wood, 5);
        Assert.IsFalse(inv.Remove(null, 1));
        Assert.IsFalse(inv.Remove(wood, 0));
        Assert.IsFalse(inv.Remove(wood, -1));
        Assert.AreEqual(5, inv.GetQuantity(wood));
    }

    [Test]
    public void GetQuantity_CountsOnlyThatItem()
    {
        inv.Add(wood, 4);
        inv.Add(gun, 1);
        Assert.AreEqual(4, inv.GetQuantity(wood));
        Assert.AreEqual(1, inv.GetQuantity(gun));
    }

    [Test]
    public void Clear_RemovesEverything()
    {
        inv.Add(wood, 4);
        inv.Clear();
        Assert.AreEqual(0, inv.Stacks.Count);
        Assert.AreEqual(0, inv.GetQuantity(wood));
    }
}
