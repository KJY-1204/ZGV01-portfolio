// 설치된 건축물이 NavMesh에서 길을 막도록 장애물을 만들고, 문은 완전히 닫혀 있을 때만 막게 하는 컴포넌트다.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BuildableNavBlocker : MonoBehaviour
{
    private readonly List<NavMeshObstacle> obstacles = new List<NavMeshObstacle>();
    private BuildableDoor door;
    private bool blocking = true;

    // 좀비가 걸어 다니는 지면 높이에서 길을 막는 분류만 장애물로 만든다(바닥, 지붕, 가구는 지나다닌다).
    public static bool Blocks(BuildableCategory category)
    {
        return category == BuildableCategory.Wall || category == BuildableCategory.Window
            || category == BuildableCategory.Door || category == BuildableCategory.Defense;
    }

    // 건축물이 놓인 뒤 한 번 호출한다. 콜라이더의 월드 경계마다 상자 장애물을 만든다(조각은 90도 단위로만 돌아서 정확하다).
    public void Build()
    {
        if (obstacles.Count > 0 || GetComponent<BuildableLadder>() != null) return;

        door = GetComponent<BuildableDoor>();
        Physics.SyncTransforms();

        bool any = false;
        Bounds combined = new Bounds();
        foreach (Collider col in GetComponentsInChildren<Collider>())
        {
            if (col.isTrigger) continue;
            if (door != null)
            {
                // 문은 틀과 문짝을 합친 하나의 장애물로 둔다.
                if (!any) { combined = col.bounds; any = true; }
                else combined.Encapsulate(col.bounds);
            }
            else
            {
                AddObstacle(col.bounds);
            }
        }
        if (door != null && any) AddObstacle(combined);
    }

    private void AddObstacle(Bounds bounds)
    {
        GameObject holder = new GameObject("NavObstacle");
        holder.transform.SetParent(transform, true);
        holder.transform.SetPositionAndRotation(bounds.center, Quaternion.identity);

        NavMeshObstacle obstacle = holder.AddComponent<NavMeshObstacle>();
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.size = bounds.size;
        obstacle.carving = true;
        obstacle.carveOnlyStationary = true;
        obstacles.Add(obstacle);
    }

    private void LateUpdate()
    {
        if (door == null) return;

        bool shouldBlock = door.IsFullyClosed;
        if (shouldBlock == blocking) return;

        blocking = shouldBlock;
        foreach (NavMeshObstacle obstacle in obstacles) obstacle.enabled = shouldBlock;
    }
}
