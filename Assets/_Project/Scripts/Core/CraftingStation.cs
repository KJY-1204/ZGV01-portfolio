// 모닥불처럼 레시피가 요구하는 제작 작업대다. 플레이어가 반경 안에 있을 때만 해당 종류의 레시피를 제작할 수 있다.
using System.Collections.Generic;
using UnityEngine;

public enum CraftingStationType
{
    None,
    Campfire,
}

public class CraftingStation : MonoBehaviour
{
    private static readonly List<CraftingStation> Active = new List<CraftingStation>();

    [SerializeField] private CraftingStationType type = CraftingStationType.Campfire;
    // 이 거리(m) 안에서 제작할 수 있다.
    [SerializeField] private float radius = 3f;

    public CraftingStationType Type => type;
    public float Radius => radius;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry()
    {
        Active.Clear();
    }

    private void OnEnable()
    {
        if (!Active.Contains(this)) Active.Add(this);
    }

    private void OnDisable()
    {
        Active.Remove(this);
    }

    // None은 작업대가 필요 없으므로 항상 참이다.
    public static bool IsNear(CraftingStationType required, Vector3 position)
    {
        if (required == CraftingStationType.None) return true;

        foreach (CraftingStation station in Active)
        {
            if (station == null || station.type != required) continue;
            if ((station.transform.position - position).sqrMagnitude <= station.radius * station.radius) return true;
        }
        return false;
    }
}
