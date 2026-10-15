// 메인 메뉴 씬(Main_Menu)을 만들고 빌드 설정의 첫 씬으로 등록하는 에디터 도구다. 밤의 야영지 3D 배경, 카메라, 메뉴 UI를 함께 만든다. 이미 있으면 다시 만든다.
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public static class MainMenuSceneBuilder
{
    private const string ScenePath = "Assets/_Project/Scenes/Main/Main_Menu.unity";
    private const string GameScenePath = "Assets/_Project/Scenes/Main/Main_Map.unity";
    private const string UxmlPath = "Assets/_Project/UI/UXML/MainMenu.uxml";
    private const string PanelSettingsPath = "Assets/_Project/UI/Themes/PanelSettings_Hud.asset";
    private const string R = "Assets/RPG_FPS_game_assets_industrial/";

    [MenuItem("ZGV01/Menu/Build Main Menu Scene")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.fieldOfView = 52f;
        camera.farClipPlane = 220f;
        cameraObject.transform.SetPositionAndRotation(new Vector3(0.4f, 1.35f, -0.5f), Quaternion.Euler(3f, -4.5f, 0f));
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<MenuCameraDrift>();
        BuildBackdrop();

        GameObject menu = new GameObject("MainMenu");
        UIDocument document = menu.AddComponent<UIDocument>();
        document.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
        document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
        menu.AddComponent<MainMenuController>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        RegisterInBuildSettings();
        AssetDatabase.SaveAssets();
    }

    // 밤의 모닥불 야영지에 플레이어가 서 있고 멀리서 좀비들이 다가오는 3D 배경이다. 메뉴 글자가 왼쪽에 있으므로 주인공은 오른쪽에 둔다. 게임 로직은 모두 떼어 내고 모양과 불빛만 남긴다.
    private static void BuildBackdrop()
    {
        GameObject root = new GameObject("Backdrop");

        // 하늘, 안개, 주변광
        Material night = AssetDatabase.LoadAssetAtPath<Material>("Assets/SkySeries Freebie/CoriolisNight4k.mat");
        if (night != null) RenderSettings.skybox = night;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.026f;
        // 지평선에서 땅이 하늘색과 이어지도록 하늘 아랫부분 색에 가깝게 맞춘다.
        RenderSettings.fogColor = new Color(0.07f, 0.09f, 0.14f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.06f, 0.08f, 0.13f);

        GameObject moonObject = new GameObject("Moon");
        moonObject.transform.SetParent(root.transform, false);
        moonObject.transform.rotation = Quaternion.Euler(32f, 150f, 0f);
        Light moon = moonObject.AddComponent<Light>();
        moon.type = LightType.Directional;
        moon.color = new Color(0.45f, 0.58f, 0.85f);
        moon.intensity = 0.8f;
        moon.shadows = LightShadows.Soft;
        RenderSettings.sun = moon;

        // 땅: 지도에서 쓰는 흙 지형 레이어의 텍스처를 넓게 깐다.
        TerrainLayer dirt = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/TirgamesAssets/SchoolScene/Environment/TerrainLayers/layer_Ground2_5_2.terrainlayer");
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(root.transform, false);
        ground.transform.localScale = new Vector3(30f, 1f, 30f);
        Object.DestroyImmediate(ground.GetComponent<Collider>());
        Material groundMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Menu_Ground" };
        if (dirt != null && dirt.diffuseTexture != null)
        {
            groundMaterial.SetTexture("_BaseMap", dirt.diffuseTexture);
            groundMaterial.SetTextureScale("_BaseMap", new Vector2(90f, 90f));
        }
        groundMaterial.SetColor("_BaseColor", new Color(0.5f, 0.48f, 0.45f));
        groundMaterial.SetFloat("_Smoothness", 0.05f);
        const string materialPath = "Assets/_Project/Materials/Menu_Ground.mat";
        AssetDatabase.DeleteAsset(materialPath);
        AssetDatabase.CreateAsset(groundMaterial, materialPath);
        ground.GetComponent<MeshRenderer>().sharedMaterial = groundMaterial;

        // 모닥불(불꽃 파티클과 불빛만 남긴다)과 그 옆에 선 플레이어
        GameObject fire = Place("Assets/_Project/Prefabs/Building/Buildable_Campfire.prefab", root.transform, new Vector3(0.9f, 0f, 5f), 0f, 1.5f);
        Light fireLight = fire != null ? fire.GetComponentInChildren<Light>() : null;
        if (fireLight != null)
        {
            fireLight.color = new Color(1f, 0.55f, 0.22f);
            fireLight.intensity = 7f;
            fireLight.range = 16f;
            fireLight.shadows = LightShadows.Soft;
            fireLight.gameObject.AddComponent<MenuLightFlicker>();
        }
        // 불을 바라보며 카메라 쪽으로 몸을 튼 3/4 자세. Idle 애니메이션은 컨트롤러 기본 상태가 맡는다.
        Place("Assets/Survivalist/Prefab/PlayerArmature.prefab", root.transform, new Vector3(2.1f, 0f, 4.2f), 235f, 1f);

        // 주인공 윤곽을 떼어 보이게 하는 뒤쪽 푸른 역광
        GameObject rim = new GameObject("RimLight");
        rim.transform.SetParent(root.transform, false);
        rim.transform.position = new Vector3(3.4f, 2.6f, 6.8f);
        Light rimLight = rim.AddComponent<Light>();
        rimLight.type = LightType.Point;
        rimLight.color = new Color(0.4f, 0.55f, 1f);
        rimLight.intensity = 4f;
        rimLight.range = 9f;

        // 야영지 소품: 불 주변은 가깝게, 왼쪽(메뉴 글자 뒤)은 컨테이너와 바리케이드로 채운다.
        Place(R + "Barrels/Barrel_v2/Barrel_v2_single.prefab", root.transform, new Vector3(-1.2f, 0f, 3.4f), 0f, 1f);
        Place(R + "Barrels/Barrel_v2/Barrel_v2_quadro.prefab", root.transform, new Vector3(4.6f, 0f, 7.4f), 20f, 1f);
        Place(R + "Boxes/Wooden_box_v1/Wooden_box_v1_LD1.prefab", root.transform, new Vector3(3.5f, 0f, 5.8f), 25f, 1f);
        Place(R + "Boxes/Wooden_box_v1/Wooden_box_v1_LD2.prefab", root.transform, new Vector3(4.4f, 0f, 6.3f), -12f, 1f);
        Place(R + "Other_props/Palets/Palet_v1/Palet_v1_set.prefab", root.transform, new Vector3(-0.9f, 0f, 6.6f), 12f, 1f);
        Place(R + "Containers/Cargo_container_v1/Cargo_container_v1_LD1close.prefab", root.transform, new Vector3(-6.5f, 0f, 8f), 14f, 1f);
        Place(R + "Containers/Cargo_container_v1/Cargo_container_v1_LD2close.prefab", root.transform, new Vector3(-7.2f, 0f, 14f), -8f, 1f);
        Place(R + "Dumpsters/Dumpsters_v1/Dumpsters_v1_garbadge.prefab", root.transform, new Vector3(8.4f, 0f, 9f), -32f, 1f);
        Place(R + "Fences/Road_blocks/Road_block_v1/Road_block_v1.prefab", root.transform, new Vector3(-1.5f, 0f, 11f), 8f, 1f);
        Place(R + "Fences/Road_blocks/Road_block_v1/Road_block_v1.prefab", root.transform, new Vector3(2.2f, 0f, 12.5f), -14f, 1f);
        Place(R + "Fences/Concrete_fences/Concrete_fence_v2/Concrete_fence_v2_S.prefab", root.transform, new Vector3(-3f, 0f, 20f), 0f, 1f);
        Place(R + "Fences/Concrete_fences/Concrete_fence_v2/Concrete_fence_v2_S.prefab", root.transform, new Vector3(4f, 0f, 21f), 4f, 1f);

        // 가로등 하나(깜빡이는 불빛)
        GameObject lamp = Place(LightingAssetBuilder.PostLampPath, root.transform, new Vector3(7.8f, 0f, 12f), 0f, 1f);
        if (lamp != null)
        {
            foreach (Light light in lamp.GetComponentsInChildren<Light>(true))
            {
                light.enabled = true;
                light.intensity = Mathf.Max(light.intensity, 3f);
                light.gameObject.AddComponent<MenuLightFlicker>();
            }
        }

        // 지평선을 채우는 나무들. 고정 시드라 다시 만들어도 같은 배치다.
        string[] trees =
        {
            "Assets/_Project/Prefabs/Map/Trees/MapTree_Pine.prefab", "Assets/_Project/Prefabs/Map/Trees/MapTree_Tree1.prefab",
            "Assets/_Project/Prefabs/Map/Trees/MapTree_Tree2.prefab", "Assets/_Project/Prefabs/Map/Trees/MapTree_Tree3.prefab",
        };
        System.Random random = new System.Random(7);
        for (int i = 0; i < 34; i++)
        {
            float x = (float)(random.NextDouble() * 60.0 - 30.0);
            float z = 14f + (float)random.NextDouble() * 28f;
            if (Mathf.Abs(x) < 3f && z < 22f) x += x < 0f ? -5f : 5f; // 좀비 길목은 비워 둔다
            Place(trees[random.Next(trees.Length)], root.transform, new Vector3(x, 0f, z), (float)random.NextDouble() * 360f, 0.9f + (float)random.NextDouble() * 0.5f);
        }

        // 어둠 속에서 다가오는 좀비들. 가까운 것은 안개에 반쯤 가린다.
        string[] zombies = { "Zombie_Walker", "Zombie_Runner", "Zombie_Walker", "Zombie_Mutant", "Zombie_Walker", "Zombie_Walker", "Zombie_Runner", "Zombie_Walker" };
        Vector3[] spots =
        {
            new Vector3(-2.5f, 0f, 15f), new Vector3(1.5f, 0f, 18f), new Vector3(6f, 0f, 16f), new Vector3(-5f, 0f, 24f),
            new Vector3(3f, 0f, 27f), new Vector3(-1f, 0f, 31f), new Vector3(9f, 0f, 24f), new Vector3(-9f, 0f, 19f),
        };
        for (int i = 0; i < zombies.Length; i++)
            Place("Assets/_Project/Prefabs/Characters/" + zombies[i] + ".prefab", root.transform, spots[i], 180f + (i * 37 % 40 - 20), 1f);
    }

    // 프리팹을 놓고 게임 로직 컴포넌트(스크립트, 길찾기, 콜라이더, 소리)를 모두 떼어 낸다.
    private static GameObject Place(string path, Transform parent, Vector3 position, float yaw, float scale)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning("메뉴 배경 프리팹을 찾지 못했다: " + path);
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        instance.transform.localScale *= scale;

        // 서로 의존하는 컴포넌트가 있어 다른 컴포넌트가 필요로 하는 것은 건너뛰고, 의존하는 쪽부터 지워 가며 여러 번 시도한다.
        for (int pass = 0; pass < 6; pass++)
        {
            Component[] all = instance.GetComponentsInChildren<Component>(true);
            foreach (Component component in all)
            {
                if (component == null) continue;
                bool remove = component is MonoBehaviour || component is Camera || component is AudioListener || component is Collider || component is Rigidbody || component is AudioSource
                    || component is UnityEngine.AI.NavMeshAgent || component is UnityEngine.AI.NavMeshObstacle || component is CharacterController;
                if (!remove || IsRequiredByOther(component, all)) continue;
                Object.DestroyImmediate(component);
            }
        }
        return instance;
    }

    // 같은 오브젝트의 다른 컴포넌트가 [RequireComponent]로 이 컴포넌트를 요구하는지 본다.
    private static bool IsRequiredByOther(Component target, Component[] all)
    {
        foreach (Component other in all)
        {
            if (other == null || other == target || other.gameObject != target.gameObject) continue;
            foreach (RequireComponent require in other.GetType().GetCustomAttributes(typeof(RequireComponent), true))
                if (IsAssignable(require.m_Type0, target) || IsAssignable(require.m_Type1, target) || IsAssignable(require.m_Type2, target)) return true;
        }
        return false;
    }

    private static bool IsAssignable(System.Type required, Component target)
    {
        return required != null && required.IsInstanceOfType(target);
    }

    // 메인 메뉴를 0번, 게임 씬을 1번으로 둔다.
    private static void RegisterInBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(ScenePath, true),
            new EditorBuildSettingsScene(GameScenePath, true),
        };
        foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            if (existing.path != ScenePath && existing.path != GameScenePath) scenes.Add(existing);
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
