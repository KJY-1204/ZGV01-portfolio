// 제작 레시피의 정적 정의다. 입력 재료와 결과물을 데이터로 관리한다.
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "RecipeDefinition", menuName = "ZGV01/Items/Recipe Definition")]
public class RecipeDefinition : ScriptableObject
{
    [Serializable]
    public class Ingredient
    {
        public ItemDefinition item;
        public int quantity = 1;
    }

    [SerializeField] private string displayName;
    [SerializeField] private Ingredient[] inputs = Array.Empty<Ingredient>();
    [SerializeField] private ItemDefinition outputItem;
    [SerializeField] private int outputQuantity = 1;
    // None이면 어디서나 제작한다. 그 외에는 해당 작업대 근처에서만 제작한다.
    [SerializeField] private CraftingStationType requiredStation;

    // 제작을 마쳤을 때 알림 문구다. {0}은 결과물 이름으로 바뀐다. 비우면 기본 문구를 쓴다.
    [SerializeField] private string completionMessage;

    public CraftingStationType RequiredStation => requiredStation;

    public string GetCompletionMessage()
    {
        string name = OutputItem != null ? OutputItem.DisplayName : displayName;
        string template = string.IsNullOrEmpty(completionMessage) ? "{0} 제작이 완료되었습니다" : completionMessage;
        return template.Replace("{0}", name);
    }

    public string DisplayName => displayName;
    public Ingredient[] Inputs => inputs;
    public ItemDefinition OutputItem => outputItem;
    public int OutputQuantity => outputQuantity;
}
