// Inventory 칸 상한(MaxSlots/MaxAddable)과 StorageTransfer 이동 규칙, 보관함 세이브 하위 호환을 검증하는 EditMode 테스트다.
using NUnit.Framework;
using UnityEngine;

public class StorageTransferTests
{
    private GameObject playerGo, chestGo;
    private Inventory player, chest;
    private ItemDefinition wood;   // 최대 스택 10
    private ItemDefinition gun;    // 최대 스택 1

    [SetUp]
    public void SetUp()
    {
        playerGo = new GameObject("player");
        player = playerGo.AddComponent<Inventory>();
        chestGo = new GameObject("chest");
        chest = chestGo.AddComponent<Inventory>();
        chest.MaxSlots = 2;
        wood = TestItems.Item("wood", 10);
        gun = TestItems.Item("gun", 1);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(playerGo);
        Object.DestroyImmediate(chestGo);
        Object.DestroyImmediate(wood);
        Object.DestroyImmediate(gun);
    }

    [Test]
    public void NoSlotLimit_MaxAddableIsUnbounded()
    {
        Assert.AreEqual(int.MaxValue, player.MaxAddable(wood));
    }

    [Test]
    public void MaxAddable_CountsFreeSlotsAndPartialStacks()
    {
        Assert.AreEqual(20, chest.MaxAddable(wood));
        chest.Add(wood, 4);
        Assert.AreEqual(16, chest.MaxAddable(wood));
        Assert.AreEqual(1, chest.MaxAddable(gun));
    }

    [Test]
    public void MaxAddable_NullItem_IsZero()
    {
        Assert.AreEqual(0, chest.MaxAddable(null));
    }

    [Test]
    public void Move_TransfersRequestedAmount()
    {
        player.Add(wood, 8);
        Assert.AreEqual(5, StorageTransfer.Move(player, chest, wood, 5));
        Assert.AreEqual(3, player.GetQuantity(wood));
        Assert.AreEqual(5, chest.GetQuantity(wood));
    }

    [Test]
    public void Move_StopsAtDestinationCapacity()
    {
        player.Add(wood, 35);
        Assert.AreEqual(20, StorageTransfer.Move(player, chest, wood, 35));
        Assert.AreEqual(15, player.GetQuantity(wood));
        Assert.AreEqual(20, chest.GetQuantity(wood));
        Assert.AreEqual(0, StorageTransfer.Move(player, chest, wood, 5));
    }

    [Test]
    public void Move_FullChest_StillAcceptsTopUpOfExistingStack()
    {
        chest.Add(wood, 5);
        chest.Add(gun, 1);
        chest.Add(gun, 1);
        // 칸은 모두 찼지만(wood 1칸 + gun 2칸 = 3칸은 상한 초과 상태) 새 칸이 필요 없는 보충은 남은 자리까지 된다.
        player.Add(wood, 8);
        Assert.AreEqual(0, chest.MaxAddable(gun));
        Assert.AreEqual(5, chest.MaxAddable(wood));
        Assert.AreEqual(5, StorageTransfer.Move(player, chest, wood, 8));
    }

    [Test]
    public void Move_LimitedBySourceQuantity()
    {
        chest.Add(wood, 3);
        Assert.AreEqual(3, StorageTransfer.Move(chest, player, wood, 99));
        Assert.AreEqual(0, chest.GetQuantity(wood));
        Assert.AreEqual(3, player.GetQuantity(wood));
    }

    [Test]
    public void Move_InvalidArguments_MoveNothing()
    {
        player.Add(wood, 5);
        Assert.AreEqual(0, StorageTransfer.Move(player, chest, null, 1));
        Assert.AreEqual(0, StorageTransfer.Move(player, chest, wood, 0));
        Assert.AreEqual(0, StorageTransfer.Move(player, null, wood, 1));
        Assert.AreEqual(0, StorageTransfer.Move(player, player, wood, 1));
        Assert.AreEqual(5, player.GetQuantity(wood));
    }

    [Test]
    public void BuildableSaveData_RoundTripsItems_AndLegacyHasNone()
    {
        BuildableSaveData data = new BuildableSaveData { buildableId = "storage_chest" };
        data.items.Add(new ItemStackSaveData { definitionId = "wood", quantity = 7 });
        BuildableSaveData back = JsonUtility.FromJson<BuildableSaveData>(JsonUtility.ToJson(data));
        Assert.AreEqual(1, back.items.Count);
        Assert.AreEqual("wood", back.items[0].definitionId);
        Assert.AreEqual(7, back.items[0].quantity);

        BuildableSaveData legacy = JsonUtility.FromJson<BuildableSaveData>("{\"buildableId\":\"wall\",\"worldId\":\"x\"}");
        Assert.IsNotNull(legacy.items);
        Assert.AreEqual(0, legacy.items.Count);
    }

    [Test]
    public void MoveAll_MovesEveryStack_FromUnlimitedToUnlimited()
    {
        player.Add(wood, 25);
        player.Add(gun, 1);
        player.Add(gun, 1);
        Inventory bag = new GameObject("bag").AddComponent<Inventory>();

        int moved = StorageTransfer.MoveAll(player, bag);

        Assert.AreEqual(27, moved);
        Assert.AreEqual(0, player.Stacks.Count);
        Assert.AreEqual(25, bag.GetQuantity(wood));
        Assert.AreEqual(2, bag.GetQuantity(gun));
        Object.DestroyImmediate(bag.gameObject);
    }

    [Test]
    public void MoveAll_RespectsDestinationCapacity_AndLeavesRestBehind()
    {
        player.Add(wood, 35);
        Assert.AreEqual(20, StorageTransfer.MoveAll(player, chest));
        Assert.AreEqual(15, player.GetQuantity(wood));
    }

    [Test]
    public void MoveAll_NullOrSame_MovesNothing()
    {
        player.Add(wood, 5);
        Assert.AreEqual(0, StorageTransfer.MoveAll(player, null));
        Assert.AreEqual(0, StorageTransfer.MoveAll(null, chest));
        Assert.AreEqual(0, StorageTransfer.MoveAll(player, player));
        Assert.AreEqual(5, player.GetQuantity(wood));
    }

    [Test]
    public void PlayerSaveData_RespawnBedId_RoundTripsAndLegacyIsEmpty()
    {
        PlayerSaveData data = new PlayerSaveData { respawnBedId = "abc123" };
        Assert.AreEqual("abc123", JsonUtility.FromJson<PlayerSaveData>(JsonUtility.ToJson(data)).respawnBedId);

        PlayerSaveData legacy = JsonUtility.FromJson<PlayerSaveData>("{\"currentHealth\":50}");
        Assert.IsTrue(string.IsNullOrEmpty(legacy.respawnBedId));
    }
}
