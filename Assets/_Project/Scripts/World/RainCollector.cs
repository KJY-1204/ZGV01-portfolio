// 빗물 집수통 건축물에 붙는 컴포넌트다. 시간이 지날수록 자기 보관함에 오염된 물을 채우고(용량 상한까지), 플레이어는 보관함을 열어 꺼낸다. 놓이기 전(건축 미리보기)에는 채우지 않는다.
using UnityEngine;

[RequireComponent(typeof(StorageContainer))]
public class RainCollector : MonoBehaviour
{
    [SerializeField] private ItemDefinition waterItem;
    // 물 한 개가 차오르는 시간(초)과 쌓아 둘 수 있는 최대 개수다.
    [SerializeField] private float secondsPerUnit = 60f;
    [SerializeField] private int capacity = 10;

    private StorageContainer storage;
    private BuildableInstance instance;
    private float progress;

    private void Awake()
    {
        storage = GetComponent<StorageContainer>();
        instance = GetComponent<BuildableInstance>();
    }

    private void Update()
    {
        if (waterItem == null) return;
        // 건축 미리보기는 월드 id가 없다.
        if (instance != null && string.IsNullOrEmpty(instance.WorldId)) return;

        if (storage.Contents.GetQuantity(waterItem) >= capacity)
        {
            progress = 0f;
            return;
        }

        progress += Time.deltaTime;
        if (progress < secondsPerUnit) return;

        progress -= secondsPerUnit;
        storage.Contents.Add(waterItem, 1);
    }
}
