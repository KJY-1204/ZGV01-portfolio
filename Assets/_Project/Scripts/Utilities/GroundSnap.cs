// 평면 좌표의 높이를 활성 지형(Terrain)의 표면 높이로 맞춰 주는 도우미다. 지형이 없는 씬(테스트 바닥)에서는 높이를 그대로 둔다.
using UnityEngine;

public static class GroundSnap
{
    public static Vector3 OnGround(Vector3 position)
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null) return position;
        position.y = terrain.SampleHeight(position) + terrain.transform.position.y;
        return position;
    }
}
