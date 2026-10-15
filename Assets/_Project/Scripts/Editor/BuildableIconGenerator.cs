// 건축물 프리팹을 투명 배경으로 렌더해 건축 메뉴용 아이콘 PNG를 만들고 정의에 연결하는 에디터 도구다.
using System.IO;
using UnityEditor;
using UnityEngine;

public static class BuildableIconGenerator
{
    private const string IconDir = "Assets/_Project/UI/Icons/Buildables";
    private const string DefinitionDir = "Assets/_Project/Data/Building";
    private const int Size = 128;
    // 프리팹을 월드 원점에서 멀리 떨어뜨려 다른 오브젝트가 찍히지 않게 한다.
    private static readonly Vector3 Origin = new Vector3(5000f, 0f, 5000f);

    [MenuItem("ZGV01/Building/Generate Missing Icons")]
    public static void GenerateMissing() => Generate(false);

    [MenuItem("ZGV01/Building/Regenerate All Icons")]
    public static void RegenerateAll() => Generate(true);

    private static void Generate(bool overwrite)
    {
        Directory.CreateDirectory(IconDir);
        foreach (string guid in AssetDatabase.FindAssets("t:BuildableDefinition", new[] { DefinitionDir }))
        {
            BuildableDefinition definition = AssetDatabase.LoadAssetAtPath<BuildableDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (definition == null || definition.Prefab == null) continue;

            SerializedObject so = new SerializedObject(definition);
            SerializedProperty icon = so.FindProperty("icon");
            if (icon.objectReferenceValue != null && !overwrite) continue;

            string path = $"{IconDir}/Icon_{definition.BuildableId}.png";
            File.WriteAllBytes(path, Render(definition.Prefab));
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            icon.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
        }
        AssetDatabase.SaveAssets();
    }

    // 위에서 비스듬히 내려다보는 아이소메트릭 각도다.
    private static readonly Quaternion IsometricView = Quaternion.Euler(30f, -35f, 0f);

    private static byte[] Render(GameObject prefab)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        return RenderAndDestroy(instance, IsometricView);
    }

    // 장면에 만든 오브젝트를 view 각도에서 투명 배경 PNG로 렌더하고 오브젝트를 지운다. 아이템 아이콘 도구도 쓴다.
    // margin은 화면 크기를 정하는 여백 배율이다(클수록 대상이 작게 찍힌다).
    // modelYaw는 대상을 Y축으로 미리 돌리는 각도다(조명 방향은 그대로 두고 반대쪽 면을 보이게 할 때 쓴다).
    internal static byte[] RenderAndDestroy(GameObject instance, Quaternion view, float margin = 0.85f, float modelYaw = 0f)
    {
        instance.transform.position = Origin;
        instance.transform.rotation = Quaternion.Euler(0f, modelYaw, 0f);

        Bounds bounds = new Bounds(Origin, Vector3.zero);
        bool any = false;
        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>())
        {
            if (renderer is ParticleSystemRenderer) continue;
            if (!any) { bounds = renderer.bounds; any = true; }
            else bounds.Encapsulate(renderer.bounds);
        }

        GameObject cameraObject = new GameObject("IconCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        camera.orthographicSize = Mathf.Max(0.1f, bounds.extents.magnitude * margin);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;

        Quaternion rotation = view;
        camera.transform.rotation = rotation;
        camera.transform.position = bounds.center - rotation * Vector3.forward * 30f;

        GameObject lightObject = new GameObject("IconLight");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.transform.rotation = Quaternion.Euler(50f, -20f, 0f);

        RenderTexture target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
        camera.targetTexture = target;
        camera.Render();

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = target;
        Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
        texture.Apply();
        byte[] png = texture.EncodeToPNG();
        RenderTexture.active = previous;

        Object.DestroyImmediate(texture);
        Object.DestroyImmediate(target);
        Object.DestroyImmediate(lightObject);
        Object.DestroyImmediate(cameraObject);
        Object.DestroyImmediate(instance);
        return png;
    }
}
