// 좀비 프리팹의 몸/옷 재질을 ZGV01/ZombieLit 복사본으로 바꾸고 ZombieShaderFX를 붙이는 에디터 도구다. 원본 에셋 재질은 건드리지 않고 프로젝트 쪽에 복사본을 만든다.
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ZombieShaderFxBuilder
{
    private const string PrefabDir = "Assets/_Project/Prefabs/Characters/";
    private const string MaterialDir = "Assets/_Project/Materials/Zombie/";
    private static readonly string[] Prefabs = { "Zombie_Walker", "Zombie_Runner", "Zombie_Archer", "Zombie_Mutant" };

    [MenuItem("ZGV01/Zombie/Apply Shader FX")]
    public static void Apply()
    {
        Shader shader = Shader.Find("ZGV01/ZombieLit");
        if (shader == null) throw new System.InvalidOperationException("셰이더 ZGV01/ZombieLit을 찾지 못했다.");
        Directory.CreateDirectory(MaterialDir);

        foreach (string name in Prefabs)
        {
            string path = PrefabDir + name + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) { Debug.LogWarning("좀비 프리팹이 없다: " + path); continue; }

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (SkinnedMeshRenderer renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    Material[] materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++) materials[i] = FxCopy(materials[i], shader);
                    renderer.sharedMaterials = materials;
                }
                if (root.GetComponent<ZombieShaderFX>() == null) root.AddComponent<ZombieShaderFX>();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("좀비 셰이더 효과 적용 완료");
    }

    // Lit 재질이면 같은 값(텍스처, 키워드 포함)을 가진 ZombieLit 복사본을 돌려준다. 이미 복사본이면 그대로 둔다.
    private static Material FxCopy(Material source, Shader shader)
    {
        if (source == null || source.shader == shader) return source;
        if (source.shader.name != "Universal Render Pipeline/Lit") return source;

        string path = MaterialDir + source.name + "_FX.mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Material copy = new Material(source) { name = source.name + "_FX" };
        copy.shader = shader;
        AssetDatabase.CreateAsset(copy, path);
        return copy;
    }
}
