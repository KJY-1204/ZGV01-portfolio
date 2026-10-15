// CraftingSystem의 재료 확인, 차감, 결과물 지급 규칙을 검증하는 EditMode 테스트다.
using NUnit.Framework;
using UnityEngine;

public class CraftingSystemTests
{
    private GameObject go;
    private Inventory inv;
    private CraftingSystem crafting;
    private ItemDefinition scrap, wood, plank;
    private RecipeDefinition recipe;

    [SetUp]
    public void SetUp()
    {
        go = new GameObject("player");
        inv = go.AddComponent<Inventory>();
        crafting = go.AddComponent<CraftingSystem>();
        TestUtil.Awake(crafting);
        scrap = TestItems.Item("scrap", 20);
        wood = TestItems.Item("wood", 20);
        plank = TestItems.Item("plank", 20);
        recipe = TestItems.Recipe(plank, 2, (scrap, 2), (wood, 3));
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(go);
        foreach (Object o in new Object[] { scrap, wood, plank, recipe }) Object.DestroyImmediate(o);
    }

    [Test]
    public void CanCraft_NullRecipe_False()
    {
        Assert.IsFalse(crafting.CanCraft(null));
    }

    [Test]
    public void CanCraft_MissingIngredient_False()
    {
        inv.Add(scrap, 2);
        inv.Add(wood, 2);
        Assert.IsFalse(crafting.CanCraft(recipe));
    }

    [Test]
    public void CanCraft_Enough_True()
    {
        inv.Add(scrap, 2);
        inv.Add(wood, 3);
        Assert.IsTrue(crafting.CanCraft(recipe));
    }

    [Test]
    public void TryCraft_ConsumesInputsAndGrantsOutput()
    {
        inv.Add(scrap, 5);
        inv.Add(wood, 4);
        Assert.IsTrue(crafting.TryCraft(recipe));
        Assert.AreEqual(3, inv.GetQuantity(scrap));
        Assert.AreEqual(1, inv.GetQuantity(wood));
        Assert.AreEqual(2, inv.GetQuantity(plank));
    }

    [Test]
    public void TryCraft_Insufficient_ChangesNothing()
    {
        inv.Add(scrap, 1);
        inv.Add(wood, 9);
        Assert.IsFalse(crafting.TryCraft(recipe));
        Assert.AreEqual(1, inv.GetQuantity(scrap));
        Assert.AreEqual(9, inv.GetQuantity(wood));
        Assert.AreEqual(0, inv.GetQuantity(plank));
    }
}
