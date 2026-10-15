// Tree Creator 셰이더 나무(URP에서 분홍색)의 메시를 그대로 쓰고 URP Lit 재질을 입힌 맵용 나무 프리팹을 만드는 에디터 도구다. 외부 에셋은 수정하지 않는다.
using UnityEditor;
using UnityEngine;

public static class MapTreeAdapterBuilder
{
    public const string TreeDir = "Assets/_Project/Prefabs/Map/Trees";
    private const string MaterialDir = "Assets/_Project/Materials/Map";

    private struct Source
    {
        public string id, prefabPath;
    }

    // Tirgames 활엽수 3종과 Devion 소나무 1종이다. 모두 껍질(0번)과 잎(1번) 서브메시를 가진 구운 메시다.
    private static readonly Source[] Sources =
    {
        new Source { id = "Tree1", prefabPath = "Assets/TirgamesAssets/SchoolScene/Environment/Trees/Tree1.prefab" },
        new Source { id = "Tree2", prefabPath = "Assets/TirgamesAssets/SchoolScene/Environment/Trees/Tree2.prefab" },
        new Source { id = "Tree3", prefabPath = "Assets/TirgamesAssets/SchoolScene/Environment/Trees/Tree3.prefab" },
        new Source { id = "Pine", prefabPath = "Assets/Devion Games/Inventory System/Examples/Models/Trees/Pine/Prefabs/Pine.prefab" },
    };

    public static string[] TreeIds
    {
        get
        {
            string[] ids = new string[Sources.Length];
            for (int i = 0; i < ids.Length; i++) ids[i] = Sources[i].id;
            return ids;
        }
    }

    public static string DecorPath(string id) => $"{TreeDir}/MapTree_{id}.prefab";
    public static string GatherPath(string id) => $"{TreeDir}/Resource_Tree_{id}.prefab";

    [MenuItem("ZGV01/Map/Build Tree Adapters")]
    public static void Build()
    {
        System.IO.Directory.CreateDirectory(TreeDir);
        System.IO.Directory.CreateDirectory(MaterialDir);
        ResourceNodeDefinition treeNode = AssetDatabase.LoadAssetAtPath<ResourceNodeDefinition>("Assets/_Project/Data/World/ResourceNodeDef_Tree.asset");

        foreach (Source source in Sources)
        {
            GameObject original = AssetDatabase.LoadAssetAtPath<GameObject>(source.prefabPath);
            if (original == null)
            {
                Debug.LogWarning($"나무 원본을 찾지 못했다: {source.prefabPath}");
                continue;
            }

            MeshFilter filter = original.GetComponent<MeshFilter>();
            MeshRenderer sourceRenderer = original.GetComponent<MeshRenderer>();
            Material[] sourceMaterials = sourceRenderer.sharedMaterials;
            Material bark = BuildBark(source.id, sourceMaterials[0]);
            Material leaves = BuildLeaves(source.id, sourceMaterials.Length > 1 ? sourceMaterials[1] : sourceMaterials[0]);

            // 장식용(지형 나무로 심는다)과 채집용(월드 오브젝트, 채집하면 줄었다가 재생) 두 가지를 만든다.
            SavePrefab(DecorPath(source.id), original, filter.sharedMesh, bark, leaves, null);
            SavePrefab(GatherPath(source.id), original, filter.sharedMesh, bark, leaves, treeNode);
        }
        AssetDatabase.SaveAssets();
    }

    private static Material BuildBark(string id, Material source)
    {
        Material material = LoadOrCreateMaterial($"{MaterialDir}/MapTree_{id}_Bark.mat");
        material.SetTexture("_BaseMap", source.GetTexture("_MainTex"));
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Smoothness", 0.08f);
        material.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material BuildLeaves(string id, Material source)
    {
        Material material = LoadOrCreateMaterial($"{MaterialDir}/MapTree_{id}_Leaves.mat");
        material.SetTexture("_BaseMap", source.GetTexture("_MainTex"));
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Smoothness", 0.05f);
        material.SetFloat("_Metallic", 0f);
        // 잎 텍스처의 알파로 오려 내고 양면을 그린다.
        material.SetFloat("_AlphaClip", 1f);
        material.SetFloat("_Cutoff", source.HasProperty("_Cutoff") ? source.GetFloat("_Cutoff") : 0.5f);
        material.SetFloat("_Cull", 0f);
        material.EnableKeyword("_ALPHATEST_ON");
        material.SetOverrideTag("RenderType", "TransparentCutout");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material LoadOrCreateMaterial(string path)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    // 원본의 충돌체 치수를 그대로 옮겨 어댑터 프리팹을 만든다. 같은 경로에 다시 만들어도 GUID가 유지되도록 덮어쓴다.
    private static void SavePrefab(string path, GameObject original, Mesh mesh, Material bark, Material leaves, ResourceNodeDefinition gatherDefinition)
    {
        GameObject root = new GameObject(System.IO.Path.GetFileNameWithoutExtension(path));
        root.AddComponent<MeshFilter>().sharedMesh = mesh;
        root.AddComponent<MeshRenderer>().sharedMaterials = new[] { bark, leaves };

        CapsuleCollider sourceCollider = original.GetComponent<CapsuleCollider>();
        if (sourceCollider != null)
        {
            CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
            capsule.center = sourceCollider.center;
            capsule.radius = sourceCollider.radius;
            capsule.height = sourceCollider.height;
            capsule.direction = sourceCollider.direction;
        }

        if (gatherDefinition != null)
        {
            // 채집 노드(나무)는 정의의 표면(나무 소리)을 쓰고 채집하면 사라졌다 재생한다.
            ResourceNode node = root.AddComponent<ResourceNode>();
            SerializedObject so = new SerializedObject(node);
            so.FindProperty("definition").objectReferenceValue = gatherDefinition;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }
}
