// 플레이어(프리팹과 씬 인스턴스)에 ItemDropper를 붙이고 전리품 프리팹을 연결하는 에디터 도구다. 이미 있으면 참조만 맞춘다.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ItemDropperBuilder
{
    private const string PlayerPrefabPath = "Assets/Survivalist/Prefab/PlayerArmature.prefab";
    private const string LootPrefabPath = "Assets/_Project/Prefabs/World/LootPickup.prefab";

    [MenuItem("ZGV01/Items/Add Item Dropper To Player")]
    public static void Apply()
    {
        GameObject loot = AssetDatabase.LoadAssetAtPath<GameObject>(LootPrefabPath);
        if (loot == null) throw new System.InvalidOperationException("전리품 프리팹을 찾지 못했다: " + LootPrefabPath);

        GameObject prefab = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            Configure(prefab, loot);
            PrefabUtility.SaveAsPrefabAsset(prefab, PlayerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }

        // 씬의 플레이어가 프리팹과 따로 풀려 있을 수 있어 같은 설정을 한 번 더 한다.
        GameObject scenePlayer = GameObject.FindGameObjectWithTag("Player");
        if (scenePlayer != null)
        {
            Configure(scenePlayer, loot);
            EditorUtility.SetDirty(scenePlayer);
            EditorSceneManager.MarkSceneDirty(scenePlayer.scene);
            EditorSceneManager.SaveScene(scenePlayer.scene);
        }
        AssetDatabase.SaveAssets();
    }

    private static void Configure(GameObject player, GameObject lootPrefab)
    {
        ItemDropper dropper = player.GetComponent<ItemDropper>();
        if (dropper == null) dropper = player.AddComponent<ItemDropper>();
        SerializedObject so = new SerializedObject(dropper);
        so.FindProperty("lootPickupPrefab").objectReferenceValue = lootPrefab;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
