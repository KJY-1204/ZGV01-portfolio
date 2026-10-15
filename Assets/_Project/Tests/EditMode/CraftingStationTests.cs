// 작업대 근처 제작 규칙(CraftingStation, CraftingSystem의 작업대 요구)을 검증하는 EditMode 테스트다.
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class CraftingStationTests
{
    private readonly List<Object> created = new List<Object>();
    private GameObject player;
    private Inventory inv;
    private CraftingSystem crafting;
    private ItemDefinition berry, roasted;
    private RecipeDefinition cookRecipe, plainRecipe;

    private CraftingStation MakeStation(Vector3 position, CraftingStationType type = CraftingStationType.Campfire, float radius = 3f)
    {
        var go = new GameObject("station");
        created.Add(go);
        go.transform.position = position;
        var station = go.AddComponent<CraftingStation>();
        TestUtil.SetFloat(station, "radius", radius);
        TestUtil.SetEnum(station, "type", (int)type);
        TestUtil.Call(station, "OnEnable");
        return station;
    }

    [SetUp]
    public void SetUp()
    {
        player = new GameObject("player");
        created.Add(player);
        inv = player.AddComponent<Inventory>();
        crafting = player.AddComponent<CraftingSystem>();
        TestUtil.Awake(crafting);

        berry = TestItems.Item("berry", 20);
        roasted = TestItems.Item("roasted", 20);
        created.Add(berry);
        created.Add(roasted);
        cookRecipe = TestItems.Recipe(roasted, 1, (berry, 1));
        plainRecipe = TestItems.Recipe(roasted, 1, (berry, 1));
        created.Add(cookRecipe);
        created.Add(plainRecipe);
        TestUtil.SetEnum(cookRecipe, "requiredStation", (int)CraftingStationType.Campfire);
        inv.Add(berry, 5);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Object o in created)
        {
            if (o is GameObject g) foreach (CraftingStation s in g.GetComponents<CraftingStation>()) TestUtil.Call(s, "OnDisable");
            Object.DestroyImmediate(o);
        }
        created.Clear();
    }

    [Test]
    public void IsNear_None_AlwaysTrue()
    {
        Assert.IsTrue(CraftingStation.IsNear(CraftingStationType.None, new Vector3(999f, 0f, 0f)));
    }

    [Test]
    public void IsNear_NoStationExists_False()
    {
        Assert.IsFalse(CraftingStation.IsNear(CraftingStationType.Campfire, Vector3.zero));
    }

    [Test]
    public void IsNear_InsideRadius_True_OutsideFalse()
    {
        MakeStation(Vector3.zero, radius: 3f);
        Assert.IsTrue(CraftingStation.IsNear(CraftingStationType.Campfire, new Vector3(2.9f, 0f, 0f)));
        Assert.IsTrue(CraftingStation.IsNear(CraftingStationType.Campfire, new Vector3(3f, 0f, 0f)));   // 경계 포함
        Assert.IsFalse(CraftingStation.IsNear(CraftingStationType.Campfire, new Vector3(3.1f, 0f, 0f)));
    }

    [Test]
    public void IsNear_UsesNearestOfSeveralStations()
    {
        MakeStation(new Vector3(50f, 0f, 0f));
        MakeStation(new Vector3(1f, 0f, 0f));
        Assert.IsTrue(CraftingStation.IsNear(CraftingStationType.Campfire, Vector3.zero));
    }

    [Test]
    public void Disabled_StationNoLongerCounts()
    {
        CraftingStation station = MakeStation(Vector3.zero);
        TestUtil.Call(station, "OnDisable");
        Assert.IsFalse(CraftingStation.IsNear(CraftingStationType.Campfire, Vector3.zero));
    }

    [Test]
    public void Enabled_Twice_IsRegisteredOnce()
    {
        CraftingStation station = MakeStation(Vector3.zero);
        TestUtil.Call(station, "OnEnable");
        TestUtil.Call(station, "OnDisable");
        Assert.IsFalse(CraftingStation.IsNear(CraftingStationType.Campfire, Vector3.zero));
    }

    [Test]
    public void Recipe_NeedingStation_BlockedAwayFromIt()
    {
        MakeStation(new Vector3(20f, 0f, 0f));
        Assert.IsFalse(crafting.IsStationAvailable(cookRecipe));
        Assert.IsFalse(crafting.CanCraft(cookRecipe));
        Assert.IsFalse(crafting.TryCraft(cookRecipe));
        Assert.AreEqual(5, inv.GetQuantity(berry));       // 재료는 그대로
        Assert.AreEqual(0, inv.GetQuantity(roasted));
    }

    [Test]
    public void Recipe_NeedingStation_WorksNearIt()
    {
        MakeStation(new Vector3(1f, 0f, 0f));
        Assert.IsTrue(crafting.CanCraft(cookRecipe));
        Assert.IsTrue(crafting.TryCraft(cookRecipe));
        Assert.AreEqual(4, inv.GetQuantity(berry));
        Assert.AreEqual(1, inv.GetQuantity(roasted));
    }

    [Test]
    public void Recipe_WithoutStationRequirement_WorksAnywhere()
    {
        Assert.IsTrue(crafting.IsStationAvailable(plainRecipe));
        Assert.IsTrue(crafting.TryCraft(plainRecipe));
    }

    [Test]
    public void WrongStationType_DoesNotSatisfyRecipe()
    {
        // 다른 종류의 작업대는 모닥불 레시피를 만족하지 못한다(종류가 하나뿐이라 None으로 등록해 검증).
        MakeStation(Vector3.zero, CraftingStationType.None);
        Assert.IsFalse(crafting.CanCraft(cookRecipe));
    }
}
