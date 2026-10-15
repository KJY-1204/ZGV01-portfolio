// 풀이 자라지 않아야 하는 자리(연못, 웅덩이 등)를 알리는 표식 컴포넌트다. GrassField가 이 원 안에는 풀을 그리지 않는다.
using System.Collections.Generic;
using UnityEngine;

public class GrassClearing : MonoBehaviour
{
    [SerializeField] private float radius = 3f;

    private static readonly List<GrassClearing> active = new List<GrassClearing>();
    public static IReadOnlyList<GrassClearing> Active => active;

    public float Radius => radius;

    private void OnEnable() => active.Add(this);
    private void OnDisable() => active.Remove(this);

    // 이 점(월드 xz)이 비워진 자리 안인지다.
    public static bool Contains(Vector3 position)
    {
        foreach (GrassClearing clearing in active)
        {
            if (clearing == null) continue;
            Vector3 offset = position - clearing.transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude < clearing.radius * clearing.radius) return true;
        }
        return false;
    }
}
