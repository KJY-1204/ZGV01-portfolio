// RecipeDefinition의 제작 완료 알림 문구 규칙을 검증하는 EditMode 테스트다.
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class RecipeDefinitionTests
{
    private readonly List<Object> created = new List<Object>();

    private RecipeDefinition Recipe(string outputName, string message, string displayName = "레시피")
    {
        ItemDefinition output = null;
        if (outputName != null)
        {
            output = TestItems.Item("out", 5);
            TestUtil.SetString(output, "displayName", outputName);
            created.Add(output);
        }
        RecipeDefinition recipe = TestItems.Recipe(output, 1);
        created.Add(recipe);
        TestUtil.SetString(recipe, "displayName", displayName);
        TestUtil.SetString(recipe, "completionMessage", message);
        return recipe;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Object o in created) Object.DestroyImmediate(o);
        created.Clear();
    }

    [Test]
    public void EmptyMessage_UsesDefaultWithOutputName()
    {
        Assert.AreEqual("물병 제작이 완료되었습니다", Recipe("물병", "").GetCompletionMessage());
        Assert.AreEqual("물병 제작이 완료되었습니다", Recipe("물병", null).GetCompletionMessage());
    }

    [Test]
    public void Template_ReplacesPlaceholderWithOutputName()
    {
        Assert.AreEqual("구운 열매 요리가 완성되었습니다", Recipe("구운 열매", "{0} 요리가 완성되었습니다").GetCompletionMessage());
    }

    [Test]
    public void Template_WithoutPlaceholder_IsUsedAsIs()
    {
        Assert.AreEqual("물이 정제되었습니다", Recipe("물병", "물이 정제되었습니다").GetCompletionMessage());
    }

    [Test]
    public void Template_ReplacesEveryPlaceholder()
    {
        Assert.AreEqual("물병, 물병", Recipe("물병", "{0}, {0}").GetCompletionMessage());
    }

    [Test]
    public void Template_WithOtherBraces_DoesNotThrow()
    {
        Assert.AreEqual("물병 {x}", Recipe("물병", "{0} {x}").GetCompletionMessage());
    }

    [Test]
    public void NoOutputItem_FallsBackToRecipeName()
    {
        Assert.AreEqual("물 정수 제작이 완료되었습니다", Recipe(null, "", "물 정수").GetCompletionMessage());
    }
}
