// 스폰할 좀비 프리팹과 가중치를 데이터로 관리하는 에셋이다. 구역마다 다른 표를 쓸 수 있다.
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ZombieSpawnTable", menuName = "ZGV01/AI/Zombie Spawn Table")]
public class ZombieSpawnTable : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public GameObject prefab;
        public float weight = 1f;
        // 이 시각(0~24시) 구간에서만 나온다. 시작이 끝보다 크면 밤을 가로지르는 구간(예: 20~6)이다. 0~24면 항상 나온다.
        public float activeFromHour = 0f;
        public float activeToHour = 24f;

        public bool IsActiveAt(float hour)
        {
            if (activeFromHour <= 0f && activeToHour >= 24f) return true;
            return activeFromHour <= activeToHour
                ? hour >= activeFromHour && hour < activeToHour
                : hour >= activeFromHour || hour < activeToHour;
        }
    }

    [SerializeField] private Entry[] entries = Array.Empty<Entry>();

    // 시각에 맞는 항목이다(원거리 궁수 좀비도 멀티플레이에서 화살을 동기화하므로 그대로 나온다).
    private static bool IsUsable(Entry entry, float hour)
    {
        return entry != null && entry.prefab != null && entry.IsActiveAt(hour);
    }

    // hour 시각에 나올 수 있는 종류 중에서 가중치로 고른다.
    public bool TryPick(float hour, out GameObject prefab)
    {
        prefab = null;
        float total = 0f;
        foreach (Entry entry in entries)
            if (IsUsable(entry, hour)) total += Mathf.Max(0f, entry.weight);
        if (total <= 0f) return false;

        float roll = UnityEngine.Random.value * total;
        float cumulative = 0f;
        foreach (Entry entry in entries)
        {
            if (!IsUsable(entry, hour)) continue;
            cumulative += Mathf.Max(0f, entry.weight);
            if (roll <= cumulative)
            {
                prefab = entry.prefab;
                return true;
            }
        }
        return false;
    }
}
