// 건물 안쪽 공간의 월드 경계(축에 맞춘 상자)를 알리는 표식 컴포넌트다. 플레이어가 이 안에 있으면 1인칭 시점으로 바뀐다(IndoorView). 물리 충돌 없이 위치만 비교한다.
using System.Collections.Generic;
using UnityEngine;

public class IndoorZone : MonoBehaviour
{
    [SerializeField] private Vector3 center;
    [SerializeField] private Vector3 size = Vector3.one;

    private static readonly List<IndoorZone> active = new List<IndoorZone>();

    private void OnEnable() => active.Add(this);
    private void OnDisable() => active.Remove(this);

    // 이 월드 위치가 어느 실내 구역 안이면 true다.
    public static bool IsInside(Vector3 position)
    {
        foreach (IndoorZone zone in active)
            if (zone != null && new Bounds(zone.center, zone.size).Contains(position)) return true;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.5f);
        Gizmos.DrawWireCube(center, size);
    }
}
