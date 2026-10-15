// 드롭 아이템의 모양을 설정하는 에디터 도구다. 무기 아이템에 월드 모델을 연결하고, 전리품 프리팹의 구체 도형을 없애고 줍기 범위를 정리한다. 여러 번 실행해도 같은 결과가 된다.
using UnityEditor;
using UnityEngine;

public static class LootVisualBuilder
{
    private const string LootPrefabPath = "Assets/_Project/Prefabs/World/LootPickup.prefab";

    [MenuItem("ZGV01/Items/Setup Loot Visuals")]
    public static void Build()
    {
        LinkWeaponModels();
        FixLootPrefab();
        AssetDatabase.SaveAssets();
    }

    // 무기 정의의 모델 프리팹을 같은 무기 아이템의 월드 모델로 연결한다.
    private static void LinkWeaponModels()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:WeaponDefinition"))
        {
            WeaponDefinition weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (weapon == null || weapon.Item == null || weapon.ModelPrefab == null) continue;

            SerializedObject so = new SerializedObject(weapon.Item);
            so.FindProperty("worldPrefab").objectReferenceValue = weapon.ModelPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(weapon.Item);
        }
    }

    // 전리품 프리팹: 구체 메시를 지우고 크기를 1로 되돌려(보이는 것은 LootPickup이 만든다) 줍기 범위를 정한다.
    private static void FixLootPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(LootPrefabPath);
        MeshRenderer renderer = root.GetComponent<MeshRenderer>();
        if (renderer != null) Object.DestroyImmediate(renderer);
        MeshFilter filter = root.GetComponent<MeshFilter>();
        if (filter != null) Object.DestroyImmediate(filter);

        root.transform.localScale = Vector3.one;
        SphereCollider sphere = root.GetComponent<SphereCollider>();
        if (sphere != null)
        {
            sphere.radius = 0.7f;
            sphere.center = new Vector3(0f, 0.5f, 0f);
        }
        PrefabUtility.SaveAsPrefabAsset(root, LootPrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }
}
