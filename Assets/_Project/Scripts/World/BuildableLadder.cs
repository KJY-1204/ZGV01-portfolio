// 설치된 사다리의 오르기 경로(기울기를 따라가는 위치), 꼭대기에서 내려설 위치를 알려주는 데이터 컴포넌트다. 로컬 +Z가 벽 쪽이다.
using System.Collections.Generic;
using UnityEngine;

public class BuildableLadder : MonoBehaviour
{
    // 상호작용 안내가 가까운 대상을 빠르게 찾을 수 있게 활성 인스턴스를 모아 둔다.
    private static readonly List<BuildableLadder> active = new List<BuildableLadder>();
    public static IReadOnlyList<BuildableLadder> Active => active;

    private void OnEnable() => active.Add(this);
    private void OnDisable() => active.Remove(this);

    [SerializeField] private float height = 3.31f;
    // 사다리가 벽에 기대 있어서 발치와 꼭대기의 Z가 다르다(로컬 좌표).
    [SerializeField] private float baseLocalZ;
    [SerializeField] private float topLocalZ;
    // 이 높이까지 오르면 위로 올라선 것으로 본다(탑 발판 높이).
    [SerializeField] private float exitHeight = 3f;
    // 올라선 뒤 벽 쪽으로 밀어 넣는 거리다.
    [SerializeField] private float exitForward = 0.9f;
    // 몸이 사다리 앞쪽으로 떨어져 서는 거리다.
    [SerializeField] private float standOff = 0.45f;

    public float Height => height;
    public float ExitHeight => exitHeight;

    // 사다리 위 높이 y(로컬)에 서는 월드 위치다.
    public Vector3 PointAt(float y)
    {
        float t = height > 0f ? Mathf.Clamp01(y / height) : 0f;
        float z = Mathf.Lerp(baseLocalZ, topLocalZ, t) - standOff;
        return transform.TransformPoint(new Vector3(0f, y, z));
    }

    public Vector3 TopExitPosition()
    {
        return transform.TransformPoint(new Vector3(0f, exitHeight + 0.05f, topLocalZ + exitForward));
    }

    public Vector3 BottomExitPosition()
    {
        return transform.TransformPoint(new Vector3(0f, 0f, baseLocalZ - standOff - 0.25f));
    }
}
