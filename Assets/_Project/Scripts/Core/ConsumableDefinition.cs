// 먹거나 마셔서 허기/갈증/체력을 회복하는 소비 아이템의 정적 정의다.
using UnityEngine;

[CreateAssetMenu(fileName = "ConsumableDefinition", menuName = "ZGV01/Items/Consumable Definition")]
public class ConsumableDefinition : ItemDefinition
{
    [SerializeField] private float hungerRestore;
    [SerializeField] private float thirstRestore;
    [SerializeField] private float healthRestore;

    public float HungerRestore => hungerRestore;
    public float ThirstRestore => thirstRestore;
    public float HealthRestore => healthRestore;
}
