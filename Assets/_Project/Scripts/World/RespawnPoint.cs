// 침대 같은 부활 지점 건축물에 붙는 컴포넌트다. 부활할 자리를 알려 주고, 부서지거나 철거돼 사라질 때 알린다.
using System;
using System.Collections.Generic;
using UnityEngine;

public class RespawnPoint : MonoBehaviour
{
    // 상호작용 안내가 가까운 침대를 찾을 수 있게 활성 인스턴스를 모아 둔다.
    private static readonly List<RespawnPoint> active = new List<RespawnPoint>();
    public static IReadOnlyList<RespawnPoint> Active => active;

    // 침대 경계에서 앞쪽(로컬 +Z)으로 이만큼 떨어진 바닥에 부활한다.
    [SerializeField] private float exitDistance = 0.7f;

    // 부서짐, 받침 붕괴, 철거로 사라질 때 알린다(저장 불러오기로 바뀌는 경우에는 알리지 않는다).
    public event Action Removed;

    private void OnEnable() => active.Add(this);
    private void OnDisable() => active.Remove(this);

    public void NotifyRemoved() => Removed?.Invoke();

    // 침대 앞쪽 바닥의 부활 위치다. 침대 경계를 앞 방향으로 지나서 exitDistance만큼 더 간 자리다.
    public Vector3 SpawnPosition
    {
        get
        {
            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;

            float reach = 0.8f;
            if (BuildableInstance.TryGetColliderBounds(gameObject, out Bounds bounds))
                reach = Mathf.Abs(forward.x) * bounds.extents.x + Mathf.Abs(forward.z) * bounds.extents.z;

            Vector3 position = transform.position + forward * (reach + exitDistance);
            position.y = transform.position.y;
            return position;
        }
    }

    // 부활할 때 바라볼 방향이다(침대에서 멀어지는 쪽).
    public Quaternion SpawnRotation
    {
        get
        {
            Vector3 forward = transform.forward;
            forward.y = 0f;
            return forward.sqrMagnitude > 0.001f ? Quaternion.LookRotation(forward) : Quaternion.identity;
        }
    }
}
