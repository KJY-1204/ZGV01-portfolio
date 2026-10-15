// 나침반에 방향과 거리를 표시할 월드 대상에 붙이는 컴포넌트다. 활성 인스턴스 목록만 들고 있고, 그리는 일은 HUD가 맡는다.
using System.Collections.Generic;
using UnityEngine;

public class CompassMarker : MonoBehaviour
{
    private static readonly List<CompassMarker> active = new List<CompassMarker>();
    public static IReadOnlyList<CompassMarker> Active => active;

    [SerializeField] private Color color = new Color(1f, 0.82f, 0.25f, 1f);
    // 같은 오브젝트의 보관함 수명이 이 초보다 적게 남으면 임박으로 본다(0이면 임박 표시를 쓰지 않는다).
    [SerializeField] private float urgentBelowSeconds = 60f;

    private StorageContainer storage;

    public Color Color => color;

    // 수명이 곧 다하는 대상이면 마커가 깜빡이며 붉게 바뀐다.
    public bool IsUrgent
    {
        get
        {
            if (urgentBelowSeconds <= 0f) return false;
            if (storage == null) storage = GetComponent<StorageContainer>();
            if (storage == null) return false;
            float remaining = storage.RemainingSeconds;
            return remaining >= 0f && remaining < urgentBelowSeconds;
        }
    }

    private void OnEnable() => active.Add(this);
    private void OnDisable() => active.Remove(this);
}
