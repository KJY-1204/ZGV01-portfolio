// 연못 프리팹의 물(Pool) 재질을 ZGV01/PondWater로 바꾸는 에디터 도구다. 원본 연못 프리팹을 다시 만들면 이 도구를 다시 실행한다.
using System.IO;
using UnityEditor;
using UnityEngine;

public static class PondWaterBuilder
{
    private const string PrefabPath = "Assets/_Project/Prefabs/World/Resource_Pond.prefab";
    private const string MaterialPath = "Assets/_Project/Materials/Cooking/Mat_PondWater_Shader.mat";

    [MenuItem("ZGV01/Materials/Apply Pond Water Shader")]
    public static void Apply()
    {
        Shader shader = Shader.Find("ZGV01/PondWater");
        if (shader == null) throw new System.InvalidOperationException("셰이더 ZGV01/PondWater를 찾지 못했다.");
        Directory.CreateDirectory(Path.GetDirectoryName(MaterialPath));

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Transform pool = root.transform.Find("Pool");
            if (pool == null) throw new System.InvalidOperationException("연못 프리팹에 Pool이 없다.");
            pool.GetComponent<MeshRenderer>().sharedMaterial = material;
            // 물은 그림자를 받지도 드리우지도 않는다.
            pool.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Collider collider = pool.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("연못 물 셰이더 적용 완료");
    }
}
