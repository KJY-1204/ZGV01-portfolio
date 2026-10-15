// 멀티플레이용 에셋(접속자 아바타 프리팹, NetworkManager가 든 NetworkRoot 프리팹)을 만드는 에디터 도구다. 아바타는 플레이어 프리팹의 모양만 복사하고 게임 로직 컴포넌트는 모두 떼어 낸다.
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEngine;

public static class NetworkAssetBuilder
{
    private const string PlayerPrefabPath = "Assets/Survivalist/Prefab/PlayerArmature.prefab";
    private const string AvatarPath = "Assets/_Project/Prefabs/Network/NetworkPlayerAvatar.prefab";
    private const string RootPath = "Assets/_Project/Resources/NetworkRoot.prefab";
    private const string WorldPath = "Assets/_Project/Prefabs/Network/NetworkWorld.prefab";
    private const string LootPath = "Assets/_Project/Prefabs/World/LootPickup.prefab";
    private static readonly string[] ZombiePaths =
    {
        "Assets/_Project/Prefabs/Characters/Zombie_Walker.prefab",
        "Assets/_Project/Prefabs/Characters/Zombie_Runner.prefab",
        "Assets/_Project/Prefabs/Characters/Zombie_Mutant.prefab",
        "Assets/_Project/Prefabs/Characters/Zombie_Archer.prefab",
    };
    private static readonly string[] AnimalPaths =
    {
        "Assets/_Project/Prefabs/Animals/Animal_Pig.prefab",
        "Assets/_Project/Prefabs/Animals/Animal_Sheep.prefab",
        "Assets/_Project/Prefabs/Animals/Animal_Cattle.prefab",
        "Assets/_Project/Prefabs/Animals/Animal_Horse.prefab",
        "Assets/_Project/Prefabs/Animals/Animal_Leopard.prefab",
        "Assets/_Project/Prefabs/Animals/Animal_Tiger.prefab",
    };

    [MenuItem("ZGV01/Network/Build Network Assets")]
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs/Network")) AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Network");
        NetworkObject avatar = BuildAvatar();
        var spawnables = new System.Collections.Generic.List<NetworkObject>();
        foreach (string path in ZombiePaths) spawnables.Add(MakeZombieNetworked(path));
        foreach (string path in AnimalPaths) spawnables.Add(MakeAnimalNetworked(path));
        spawnables.Add(MakeLootNetworked());
        NetworkObject world = BuildWorld();
        BuildRoot(avatar, spawnables.ToArray(), world);
        AssetDatabase.SaveAssets();
    }

    // 에디터 호스트와 함께 시험할 두 번째 인스턴스(개발 빌드)를 프로젝트 밖 폴더에 만든다. 실행할 때 -mp-join=127.0.0.1:7777을 주면 메인 메뉴에서 자동으로 참가한다.
    [MenuItem("ZGV01/Network/Build Test Client")]
    public static void BuildTestClient()
    {
        string[] scenes = System.Array.ConvertAll(System.Array.FindAll(EditorBuildSettings.scenes, s => s.enabled), s => s.path);
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "E:/Unity/ZGV01_TestBuild/ZGV01.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development,
        };
        UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log("테스트 클라이언트 빌드 결과: " + report.summary.result + " (" + report.summary.totalErrors + " 오류)");
    }

    private static NetworkObject BuildAvatar()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            root.name = "NetworkPlayerAvatar";
            root.tag = "Untagged";

            // 서로 의존하는 컴포넌트가 있어 한 번에 안 지워지면 여러 번 시도한다. 애니메이터와 렌더러만 남긴다.
            for (int pass = 0; pass < 4; pass++)
            {
                foreach (Component component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component == null) continue;
                    bool remove = component is MonoBehaviour || component is Collider || component is Rigidbody || component is AudioSource
                        || component is CharacterController || component is Camera || component is AudioListener;
                    if (remove) Object.DestroyImmediate(component);
                }
            }

            Animator animator = root.GetComponent<Animator>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // 소유자 권위: 소유자 쪽 값을 다른 접속자에게 보낸다(위치와 좌우 회전만).
            NetworkObject networkObject = root.AddComponent<NetworkObject>();
            NetworkTransform networkTransform = root.AddComponent<NetworkTransform>();
            networkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
            networkTransform.SyncScaleX = networkTransform.SyncScaleY = networkTransform.SyncScaleZ = false;
            networkTransform.SyncRotAngleX = false;
            networkTransform.SyncRotAngleZ = false;
            networkTransform.Interpolate = true;

            NetworkAnimator networkAnimator = root.AddComponent<NetworkAnimator>();
            networkAnimator.AuthorityMode = NetworkAnimator.AuthorityModes.Owner;
            SerializedObject animatorSo = new SerializedObject(networkAnimator);
            animatorSo.FindProperty("m_Animator").objectReferenceValue = animator;
            animatorSo.ApplyModifiedPropertiesWithoutUndo();

            root.AddComponent<NetworkPlayerAvatar>();
            root.AddComponent<NetworkCombat>();
            // 서버가 참가자의 인벤토리 원본을 들고 제작을 처리한다(Inventory와 CraftingSystem은 서버에서만 쓴다).
            root.AddComponent<Inventory>();
            root.AddComponent<CraftingSystem>();
            root.AddComponent<NetworkInventory>();

            // 손에 든 무기의 모습: 플레이어와 같은 설정(자세 오프셋, 손 모양, 무기 목록)의 무기 컴포넌트를 보기 전용으로 쓴다. 다른 접속자 쪽에서만 AvatarWeaponSync가 보기 전용 모드로 만든다.
            WeaponEquipment playerWeapons = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath).GetComponent<WeaponEquipment>();
            WeaponEquipment avatarWeapons = root.AddComponent<WeaponEquipment>();
            if (playerWeapons != null) EditorUtility.CopySerialized(playerWeapons, avatarWeapons);
            root.AddComponent<AvatarWeaponSync>();

            // 맨손 공격 모습: 플레이어와 같은 곡선/시간 설정의 펀치 컴포넌트를 보기 전용으로 쓴다(피해 없이 팔과 상체 동작만 재생한다).
            UnarmedPunch playerPunch = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath).GetComponent<UnarmedPunch>();
            UnarmedPunch avatarPunch = root.AddComponent<UnarmedPunch>();
            if (playerPunch != null) EditorUtility.CopySerialized(playerPunch, avatarPunch);
            root.AddComponent<AvatarActionSync>();

            // 몸 충돌체: 플레이어 CharacterController와 같은 크기의 캡슐을 새 Avatar 레이어에 둔다. 다른 접속자의 아바타만 켜고(AvatarActionSync) 사망하면 눕힌다.
            CapsuleCollider body = root.AddComponent<CapsuleCollider>();
            body.direction = 1;
            body.height = 1.8f;
            body.radius = 0.28f;
            body.center = new Vector3(0f, 0.93f, 0f);
            int avatarLayer = EnsureAvatarLayer();
            if (avatarLayer >= 0) root.layer = avatarLayer;

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, AvatarPath);
            return saved.GetComponent<NetworkObject>();
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // 프로젝트에 Avatar 물리 레이어가 없으면 비어 있는 첫 사용자 레이어(10번부터)에 만든다. 레이어 번호를 돌려준다(만들지 못하면 -1).
    private static int EnsureAvatarLayer()
    {
        int existing = LayerMask.NameToLayer(AvatarBody.LayerName);
        if (existing >= 0) return existing;

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0) return -1;
        SerializedObject tagManager = new SerializedObject(assets[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        for (int i = 10; i < layers.arraySize; i++)
        {
            SerializedProperty slot = layers.GetArrayElementAtIndex(i);
            if (!string.IsNullOrEmpty(slot.stringValue)) continue;
            slot.stringValue = AvatarBody.LayerName;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            return i;
        }
        return -1;
    }

    // 좀비 프리팹에 서버 권위 위치 동기화와 사건 동기화를 단다(이미 있으면 그대로 둔다). 게임 로직 컴포넌트는 그대로다.
    private static NetworkObject MakeZombieNetworked(string path)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            if (root.GetComponent<NetworkObject>() == null) root.AddComponent<NetworkObject>();
            NetworkTransform networkTransform = root.GetComponent<NetworkTransform>();
            if (networkTransform == null) networkTransform = root.AddComponent<NetworkTransform>();
            networkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Server;
            networkTransform.SyncScaleX = networkTransform.SyncScaleY = networkTransform.SyncScaleZ = false;
            networkTransform.SyncRotAngleX = false;
            networkTransform.SyncRotAngleZ = false;
            networkTransform.Interpolate = true;
            if (root.GetComponent<NetworkZombie>() == null) root.AddComponent<NetworkZombie>();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            return saved.GetComponent<NetworkObject>();
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // 동물/맹수 프리팹을 네트워크 오브젝트로 만든다(서버 권위 위치 + 상태/사건 동기화).
    private static NetworkObject MakeAnimalNetworked(string path)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            if (root.GetComponent<NetworkObject>() == null) root.AddComponent<NetworkObject>();
            NetworkTransform networkTransform = root.GetComponent<NetworkTransform>();
            if (networkTransform == null) networkTransform = root.AddComponent<NetworkTransform>();
            networkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Server;
            networkTransform.SyncScaleX = networkTransform.SyncScaleY = networkTransform.SyncScaleZ = false;
            networkTransform.SyncRotAngleX = false;
            networkTransform.SyncRotAngleZ = false;
            networkTransform.Interpolate = true;
            if (root.GetComponent<NetworkAnimal>() == null) root.AddComponent<NetworkAnimal>();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            return saved.GetComponent<NetworkObject>();
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // 전리품 프리팹을 네트워크 오브젝트로 만든다(서버가 소환하면 모든 접속자에게 보인다).
    private static NetworkObject MakeLootNetworked()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(LootPath);
        try
        {
            if (root.GetComponent<NetworkObject>() == null) root.AddComponent<NetworkObject>();
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, LootPath);
            return saved.GetComponent<NetworkObject>();
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static NetworkObject BuildWorld()
    {
        GameObject root = new GameObject("NetworkWorld");
        try
        {
            root.AddComponent<NetworkObject>();
            root.AddComponent<WorldSync>();
            root.AddComponent<BuildSync>();
            root.AddComponent<StorageSync>();
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, WorldPath);
            return saved.GetComponent<NetworkObject>();
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void BuildRoot(NetworkObject avatar, NetworkObject[] spawnables, NetworkObject world)
    {
        GameObject root = new GameObject("NetworkRoot");
        try
        {
            NetworkManager manager = root.AddComponent<NetworkManager>();
            UnityTransport transport = root.AddComponent<UnityTransport>();
            manager.NetworkConfig.NetworkTransport = transport;
            // 플레이어 오브젝트는 게임 씬이 열린 뒤 MultiplayerSession이 직접 소환한다.
            manager.NetworkConfig.PlayerPrefab = null;
            manager.RunInBackground = true;

            MultiplayerSession session = root.AddComponent<MultiplayerSession>();
            SerializedObject so = new SerializedObject(session);
            so.FindProperty("avatarPrefab").objectReferenceValue = avatar;
            SerializedProperty list = so.FindProperty("spawnablePrefabs");
            list.arraySize = spawnables.Length;
            for (int i = 0; i < spawnables.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = spawnables[i];
            so.FindProperty("worldPrefab").objectReferenceValue = world;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, RootPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
