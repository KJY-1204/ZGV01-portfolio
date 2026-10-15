// 고철 더미(채집 시 고철 조각을 주는 자원 노드)의 정의와 프리팹을 만들고, 열린 맵 씬에 겹치지 않게 추가 배치하는 에디터 도구다.
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ScrapNodeBuilder
{
    public const string PrefabPath = "Assets/_Project/Prefabs/World/Resource_ScrapPile.prefab";
    private const string DefinitionPath = "Assets/_Project/Data/World/ResourceNodeDef_ScrapPile.asset";
    private const string ScrapItemPath = "Assets/_Project/Data/Items/Item_ScrapMetal.asset";
    private const string VendorPath = "Assets/RPG_FPS_game_assets_industrial/Dumpsters/Dumpsters_v1/Dumpsters_v1_garbadge.prefab";
    private const string SoundDir = "Assets/Devion Games/Third Person Controller/Example/Sounds/";
    private const string AirBurst = "Assets/Devion Games/Inventory System/Examples/Sounds/Air Burst.ogg";
    private const string PuffPath = "Assets/_Project/Prefabs/Effects/Effect_DepletePuff.prefab";

    // 구역 안 개수다. 구역 밖은 도로 둘레에 흩뿌린다.
    private static readonly Dictionary<MapLayout.ZoneKind, int> ZoneCounts = new Dictionary<MapLayout.ZoneKind, int>
    {
        { MapLayout.ZoneKind.Camp, 4 }, { MapLayout.ZoneKind.Warehouse, 12 }, { MapLayout.ZoneKind.Factory, 16 },
        { MapLayout.ZoneKind.School, 5 }, { MapLayout.ZoneKind.Yard, 10 }, { MapLayout.ZoneKind.Quarry, 10 },
    };
    private const int RoadsideCount = 30;

    [MenuItem("ZGV01/World/Build Scrap Pile Node")]
    public static void Build()
    {
        ResourceNodeDefinition definition = BuildDefinition();
        BuildPrefab(definition);
        AssetDatabase.SaveAssets();
    }

    // 맵을 다시 만들지 않고, 열린 씬의 "Nodes" 아래에 고철 더미를 더한다. 이미 있으면 다시 만들지 않는다.
    [MenuItem("ZGV01/World/Place Scrap Piles In Scene")]
    public static void PlaceInScene()
    {
        Build();
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Terrain terrain = Object.FindFirstObjectByType<Terrain>();
        if (terrain == null) throw new System.InvalidOperationException("지형이 없다. 맵 씬을 연다.");

        GameObject nodesObject = GameObject.Find("Nodes");
        Transform nodes = nodesObject != null ? nodesObject.transform : new GameObject("Nodes").transform;
        Transform group = nodes.Find("ScrapPiles");
        if (group != null) Object.DestroyImmediate(group.gameObject);
        group = new GameObject("ScrapPiles").transform;
        group.SetParent(nodes, false);

        System.Random random = new System.Random(7731);
        Vector2[] road = MapLayout.SampleRoad(12f);
        int total = 0;
        foreach (MapLayout.Zone zone in MapLayout.Zones)
        {
            int want = ZoneCounts[zone.kind], placed = 0;
            for (int attempt = 0; attempt < want * 40 && placed < want; attempt++)
            {
                Vector2 xz = new Vector2(
                    zone.center.x + Range(random, -zone.half.x + 8f, zone.half.x - 8f),
                    zone.center.y + Range(random, -zone.half.y + 8f, zone.half.y - 8f));
                if (NearAny(road, xz, MapLayout.RoadHalfWidth + 2f)) continue;
                if (TryPlace(terrain, prefab, group, xz, random)) placed++;
            }
            total += placed;
        }

        int roadside = 0;
        for (int attempt = 0; attempt < 6000 && roadside < RoadsideCount; attempt++)
        {
            Vector2 anchor = road[random.Next(road.Length)];
            float angle = Range(random, 0f, Mathf.PI * 2f);
            Vector2 xz = anchor + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Range(random, MapLayout.RoadHalfWidth + 3f, MapLayout.RoadHalfWidth + 22f);
            if (Mathf.Abs(xz.x) > 470f || Mathf.Abs(xz.y) > 470f || InAnyZone(xz) || MapLayout.InMine(xz.x, xz.y, 10f)) continue;
            Vector2 n = MapLayout.ToNormalized(xz.x, xz.y);
            if (terrain.terrainData.GetSteepness(n.x, n.y) > 25f) continue;
            if (TryPlace(terrain, prefab, group, xz, random)) roadside++;
        }
        total += roadside;

        Debug.Log($"고철 더미 배치 완료: {total}개(길가 {roadside}개)");
        EditorSceneManager.MarkSceneDirty(group.gameObject.scene);
        EditorSceneManager.SaveScene(group.gameObject.scene);
    }

    private static bool TryPlace(Terrain terrain, GameObject prefab, Transform parent, Vector2 xz, System.Random random)
    {
        float ground = terrain.SampleHeight(new Vector3(xz.x, 0f, xz.y)) + terrain.transform.position.y;
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.transform.SetPositionAndRotation(new Vector3(xz.x, ground - 0.05f, xz.y), Quaternion.Euler(0f, Range(random, 0f, 360f), 0f));

        // 이미 있는 소품, 건물, 노드와 겹치면 취소한다(지형 콜라이더와 자기 자신은 무시).
        Physics.SyncTransforms();
        BoxCollider own = instance.GetComponent<BoxCollider>();
        Vector3 center = own.transform.TransformPoint(own.center + Vector3.up * 0.15f);
        Vector3 half = Vector3.Scale(own.size * 0.5f, own.transform.lossyScale) + new Vector3(0.6f, -0.1f, 0.6f);
        bool blocked = false;
        foreach (Collider hit in Physics.OverlapBox(center, half, instance.transform.rotation, ~0, QueryTriggerInteraction.Collide))
        {
            if (hit is TerrainCollider || hit.transform.IsChildOf(instance.transform)) continue;
            blocked = true;
            break;
        }
        if (blocked) Object.DestroyImmediate(instance);
        return !blocked;
    }

    private static bool InAnyZone(Vector2 xz)
    {
        foreach (MapLayout.Zone zone in MapLayout.Zones)
            if (MapLayout.InZone(zone, xz.x, xz.y, 10f)) return true;
        return false;
    }

    private static bool NearAny(Vector2[] points, Vector2 xz, float distance)
    {
        float sqr = distance * distance;
        foreach (Vector2 point in points)
            if ((point - xz).sqrMagnitude < sqr) return true;
        return false;
    }

    private static float Range(System.Random random, float min, float max) => min + (float)random.NextDouble() * (max - min);

    private static ResourceNodeDefinition BuildDefinition()
    {
        ResourceNodeDefinition definition = AssetDatabase.LoadAssetAtPath<ResourceNodeDefinition>(DefinitionPath);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<ResourceNodeDefinition>();
            AssetDatabase.CreateAsset(definition, DefinitionPath);
        }

        SerializedObject so = new SerializedObject(definition);
        so.FindProperty("yieldItem").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemDefinition>(ScrapItemPath);
        so.FindProperty("yieldPerGather").intValue = 1;
        so.FindProperty("maxYield").intValue = 3;
        so.FindProperty("respawnSeconds").floatValue = 240f;
        so.FindProperty("impactSurface").enumValueIndex = (int)ImpactSurface.Metal;
        ApplySounds(so, "gatherSounds", new[] { SoundDir + "Hammering.mp3" }, 0.85f, new Vector2(1.35f, 1.6f));
        ApplySounds(so, "depleteSounds", new[] { SoundDir + "Land.wav", AirBurst }, 0.85f, new Vector2(0.9f, 1.05f));
        so.FindProperty("depleteEffect").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PuffPath);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static void ApplySounds(SerializedObject so, string field, string[] files, float volume, Vector2 pitch)
    {
        SerializedProperty group = so.FindProperty(field);
        SerializedProperty clips = group.FindPropertyRelative("clips");
        clips.arraySize = files.Length;
        for (int i = 0; i < files.Length; i++)
            clips.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(files[i]);
        group.FindPropertyRelative("volume").floatValue = volume;
        group.FindPropertyRelative("pitchRange").vector2Value = pitch;
    }

    // 벤더 모델을 바닥에 앉히고 루트에 박스 콜라이더와 노드를 단다(ResourceNodeAssetBuilder와 같은 구조).
    private static void BuildPrefab(ResourceNodeDefinition definition)
    {
        GameObject root = new GameObject("Resource_ScrapPile");
        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VendorPath), root.transform);

        Bounds bounds = new Bounds();
        bool any = false;
        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
        {
            if (renderer is ParticleSystemRenderer) continue;
            if (!any) { bounds = renderer.bounds; any = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        model.transform.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
        foreach (Collider vendorCollider in model.GetComponentsInChildren<Collider>()) vendorCollider.enabled = false;

        BoxCollider box = root.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, bounds.size.y * 0.5f, 0f);
        box.size = bounds.size;

        ResourceNode node = root.AddComponent<ResourceNode>();
        SerializedObject so = new SerializedObject(node);
        so.FindProperty("definition").objectReferenceValue = definition;
        so.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.SaveAssets();
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
    }
}
