// 대상 오브젝트의 메시를 역면 외곽선 셰이더로 다시 그려 테두리를 만드는 도우미다. 상호작용 대상과 철거 대상 강조가 함께 쓴다.
using System.Collections.Generic;
using UnityEngine;

public class OutlineDrawer
{
    private static readonly int CenterId = Shader.PropertyToID("_OutlineCenter");
    private static readonly int ExtentsId = Shader.PropertyToID("_OutlineExtents");
    private static readonly int WidthId = Shader.PropertyToID("_OutlineWidth");
    private const int CachePurgeThreshold = 64;

    private readonly Dictionary<Component, List<(MeshFilter filter, Renderer renderer)>> meshCache = new Dictionary<Component, List<(MeshFilter, Renderer)>>();
    private readonly MaterialPropertyBlock block = new MaterialPropertyBlock();

    // target 아래의 모든 메시를 material로 width(월드 단위)만큼 부풀려 이번 프레임에 그린다.
    public void Draw(Component target, Material material, float width)
    {
        if (target == null) return;

        if (!meshCache.TryGetValue(target, out List<(MeshFilter filter, Renderer renderer)> meshes))
        {
            if (meshCache.Count >= CachePurgeThreshold) PurgeDestroyed();

            meshes = new List<(MeshFilter, Renderer)>();
            foreach (MeshFilter filter in target.GetComponentsInChildren<MeshFilter>())
            {
                Renderer renderer = filter.GetComponent<Renderer>();
                if (renderer != null && filter.sharedMesh != null) meshes.Add((filter, renderer));
            }
            meshCache[target] = meshes;
        }

        foreach ((MeshFilter filter, Renderer renderer) in meshes)
        {
            if (filter == null || renderer == null || !renderer.enabled) continue;

            Bounds bounds = renderer.bounds;
            block.SetVector(CenterId, bounds.center);
            block.SetVector(ExtentsId, bounds.extents);
            block.SetFloat(WidthId, width);

            Mesh mesh = filter.sharedMesh;
            for (int sub = 0; sub < mesh.subMeshCount; sub++)
                Graphics.DrawMesh(mesh, renderer.localToWorldMatrix, material, renderer.gameObject.layer, null, sub, block);
        }
    }

    // 파괴된 대상의 캐시 항목을 지운다.
    private void PurgeDestroyed()
    {
        List<Component> dead = new List<Component>();
        foreach (Component key in meshCache.Keys)
            if (key == null) dead.Add(key);
        foreach (Component key in dead) meshCache.Remove(key);
    }
}
