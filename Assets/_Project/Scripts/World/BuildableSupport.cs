// 건축물이 서로를 받치는 관계(벽에 붙은 가구/사다리, 위층 조각)를 판단하고, 받침이 사라질 때 무너뜨리는 규칙을 모은 정적 도우미다.
using System.Collections.Generic;
using UnityEngine;

public static class BuildableSupport
{
    // 위층으로 보는 기준: 콜라이더 바닥이 이 높이보다 위에 있는 조각만 아래 조각에 의존한다(지면의 벽/가구/바닥은 땅이 받친다).
    private const float UpperLevelMinY = 0.5f;

    // target이 사라지면 받침을 잃는 건축물들이다. removed에 든 건축물은 이미 사라지는 중이라 받침으로 세지 않는다.
    // 벽에 붙은 조각은 그 벽이 사라지면 항상 잃고, 위층 조각은 다른 받침이 없을 때만 잃는다.
    public static List<BuildableInstance> FindOrphans(BuildableInstance target, ICollection<BuildableInstance> removed)
    {
        List<BuildableInstance> orphans = new List<BuildableInstance>();

        foreach (BuildableInstance other in Object.FindObjectsByType<BuildableInstance>(FindObjectsSortMode.None))
        {
            if (!IsPlaced(other) || other == target || removed.Contains(other)) continue;
            if (other.SupportId == target.WorldId) orphans.Add(other);
        }

        if (!BuildableInstance.TryGetColliderBounds(target.gameObject, out Bounds bounds)) return orphans;

        Vector3 center = new Vector3(bounds.center.x, bounds.max.y + 0.01f, bounds.center.z);
        Vector3 halfExtents = new Vector3(Mathf.Max(0.05f, bounds.extents.x - 0.05f), 0.09f, Mathf.Max(0.05f, bounds.extents.z - 0.05f));
        foreach (Collider collider in Physics.OverlapBox(center, halfExtents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
        {
            BuildableInstance other = collider.GetComponentInParent<BuildableInstance>();
            if (!IsPlaced(other) || other == target || removed.Contains(other) || orphans.Contains(other)) continue;
            if (collider.bounds.min.y <= UpperLevelMinY) continue;
            if (HasOtherSupport(other, target, removed)) continue;
            orphans.Add(other);
        }
        return orphans;
    }

    // target 말고도 piece 바로 아래에서 받쳐 주는 건축물이 있는지 본다.
    private static bool HasOtherSupport(BuildableInstance piece, BuildableInstance target, ICollection<BuildableInstance> removed)
    {
        if (!BuildableInstance.TryGetColliderBounds(piece.gameObject, out Bounds bounds)) return false;

        Vector3 center = new Vector3(bounds.center.x, bounds.min.y - 0.05f, bounds.center.z);
        Vector3 halfExtents = new Vector3(Mathf.Max(0.05f, bounds.extents.x - 0.05f), 0.04f, Mathf.Max(0.05f, bounds.extents.z - 0.05f));
        foreach (Collider collider in Physics.OverlapBox(center, halfExtents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
        {
            BuildableInstance support = collider.GetComponentInParent<BuildableInstance>();
            if (!IsPlaced(support) || support == piece || support == target || removed.Contains(support)) continue;
            return true;
        }
        return false;
    }

    // root가 부서질 때 받침을 잃는 건축물을 연쇄로 무너뜨린다(root 자체는 호출한 쪽이 없앤다). 무너진 것은 환불하지 않는다.
    public static void Collapse(BuildableInstance root)
    {
        if (root == null) return;

        HashSet<BuildableInstance> removed = new HashSet<BuildableInstance> { root };
        List<BuildableInstance> victims = new List<BuildableInstance>();
        Queue<BuildableInstance> queue = new Queue<BuildableInstance>();
        // 연쇄 단계(깊이)마다 조금씩 늦게 무너져 위에서 아래로 차례로 무너지는 것처럼 보인다.
        Dictionary<BuildableInstance, int> depth = new Dictionary<BuildableInstance, int> { { root, 0 } };
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            BuildableInstance current = queue.Dequeue();
            foreach (BuildableInstance orphan in FindOrphans(current, removed))
            {
                if (!removed.Add(orphan)) continue;
                victims.Add(orphan);
                depth[orphan] = depth[current] + 1;
                queue.Enqueue(orphan);
            }
        }

        foreach (BuildableInstance victim in victims)
        {
            float delay = 0.18f * depth[victim] + Random.Range(0f, 0.12f);
            BuildableHealth health = victim.GetComponent<BuildableHealth>();
            if (health != null) health.Collapse(delay);
            else BuildableDebris.Crumble(victim, delay);
        }
    }

    private static bool IsPlaced(BuildableInstance instance)
    {
        return instance != null && !string.IsNullOrEmpty(instance.WorldId) && instance.gameObject.activeInHierarchy;
    }
}
