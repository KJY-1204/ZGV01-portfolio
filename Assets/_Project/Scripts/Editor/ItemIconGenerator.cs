// 아이템 아이콘 PNG를 만들어 아이템 정의에 연결하는 에디터 도구다. 총은 모델을, 나머지는 기본 도형으로 만든 작은 모델을 투명 배경으로 렌더한다.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ItemIconGenerator
{
    private const string IconDir = "Assets/_Project/UI/Icons/Items";
    private const string ItemDir = "Assets/_Project/Data/Items";
    private const string WeaponDir = "Assets/_Project/Data/Weapons";

    // 총은 옆모습이 보이도록 거의 정면에서 약간만 내려다본다. 소품은 비스듬히 내려다본다.
    private static readonly Quaternion PropView = Quaternion.Euler(28f, -30f, 0f);
    // 소품은 가장자리에 붙지 않게 여백을 조금 더 둔다.
    private const float Margin = 1.0f;
    private const float WeaponMargin = 1.08f;

    [MenuItem("ZGV01/Items/Generate Missing Item Icons")]
    public static void GenerateMissing() => Generate(false);

    [MenuItem("ZGV01/Items/Regenerate All Item Icons")]
    public static void RegenerateAll() => Generate(true);

    private static void Generate(bool overwrite)
    {
        Directory.CreateDirectory(IconDir);

        foreach (KeyValuePair<string, Func<GameObject>> prop in Props)
        {
            ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(ItemDir + "/" + prop.Key + ".asset");
            if (item == null) { Debug.LogWarning("아이템 에셋이 없다: " + prop.Key); continue; }
            Apply(item, overwrite, () => BuildableIconGenerator.RenderAndDestroy(prop.Value(), PropView, Margin));
        }

        foreach (string guid in AssetDatabase.FindAssets("t:WeaponDefinition", new[] { WeaponDir }))
        {
            WeaponDefinition weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (weapon == null || weapon.Item == null || weapon.ModelPrefab == null) continue;
            Apply(weapon.Item, overwrite, () =>
            {
                GameObject model = WeaponModel(weapon);
                SideViewFor(model, out Quaternion view, out float modelYaw);
                return BuildableIconGenerator.RenderAndDestroy(model, view, WeaponMargin, modelYaw);
            });
        }

        AssetDatabase.SaveAssets();
    }

    private static void Apply(ItemDefinition item, bool overwrite, Func<byte[]> render)
    {
        SerializedObject so = new SerializedObject(item);
        SerializedProperty icon = so.FindProperty("icon");
        if (icon.objectReferenceValue != null && !overwrite) return;

        string path = $"{IconDir}/Icon_{item.StableId}.png";
        File.WriteAllBytes(path, render());
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
        EditorUtility.SetDirty(item);
    }

    // 총의 얇은 쪽(폭)을 바라보는 옆모습 시점을 고른다. 모델마다 길이 축이 X(소총)이거나 Z(권총)로 다르다.
    private static void SideViewFor(GameObject model, out Quaternion view, out float modelYaw)
    {
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        bool any = false;
        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
        {
            if (renderer is ParticleSystemRenderer) continue;
            if (!any) { bounds = renderer.bounds; any = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        // 소총 모델은 총구가 -X 쪽이라 모델을 180도 돌려 권총처럼 총구가 오른쪽을 향하게 맞춘다(조명은 그대로).
        bool longAlongX = bounds.size.x >= bounds.size.z;
        view = Quaternion.Euler(14f, longAlongX ? 0f : -90f, 0f);
        modelYaw = longAlongX ? 180f : 0f;
    }

    // 총 모델에 장착할 때와 같은 재질을 입힌다(소총 모델은 기본 재질이 흰색이다).
    private static GameObject WeaponModel(WeaponDefinition weapon)
    {
        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(weapon.ModelPrefab);
        if (weapon.ModelMaterial != null)
        {
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
            {
                if (renderer is ParticleSystemRenderer) continue;
                renderer.sharedMaterial = weapon.ModelMaterial;
            }
        }
        // 총구 화염 같은 이펙트는 아이콘에 찍히지 않게 한다.
        foreach (ParticleSystem system in model.GetComponentsInChildren<ParticleSystem>()) system.gameObject.SetActive(false);
        return model;
    }

    // ---- 소품 모델(기본 도형) ----

    private static readonly Dictionary<string, Func<GameObject>> Props = new Dictionary<string, Func<GameObject>>
    {
        { "Item_Wood", Wood },
        { "Item_ScrapMetal", ScrapMetal },
        { "Item_BasicBuildingMaterial", BasicMaterial },
        { "Item_RefinedMaterial", RefinedMaterial },
        { "Item_Food_Berries", () => Berries(new Color(0.62f, 0.10f, 0.22f)) },
        { "Item_Food_RoastedBerries", () => Berries(new Color(0.30f, 0.12f, 0.08f)) },
        { "Item_Food_CannedBeans", () => Can(new Color(0.78f, 0.28f, 0.16f), false) },
        { "Item_Food_HeatedBeans", () => Can(new Color(0.95f, 0.55f, 0.12f), true) },
        { "Item_Food_RawMeat", () => Meat(new Color(0.80f, 0.25f, 0.28f), new Color(0.95f, 0.62f, 0.62f)) },
        { "Item_Food_CookedMeat", () => Meat(new Color(0.50f, 0.27f, 0.13f), new Color(0.72f, 0.45f, 0.22f)) },
        { "Item_Lantern", Lantern },
        { "Item_Armor_LeatherCap", () => Helmet(new Color(0.55f, 0.35f, 0.18f), false) },
        { "Item_Armor_LeatherVest", () => Chest(new Color(0.55f, 0.35f, 0.18f), false) },
        { "Item_Armor_LeatherLeggings", () => Legs(new Color(0.55f, 0.35f, 0.18f), false) },
        { "Item_Armor_IronHelmet", () => Helmet(new Color(0.62f, 0.65f, 0.70f), true) },
        { "Item_Armor_IronPlate", () => Chest(new Color(0.62f, 0.65f, 0.70f), true) },
        { "Item_Armor_IronGreaves", () => Legs(new Color(0.62f, 0.65f, 0.70f), true) },
        { "Item_Resource_Leather", Leather },
        { "Item_Medical_Bandage", Bandage },
        { "Item_Drink_Water", () => Bottle(new Color(0.55f, 0.82f, 0.95f)) },
        { "Item_Drink_DirtyWater", () => Bottle(new Color(0.42f, 0.33f, 0.18f)) },
        { "Item_Ammo_Pistol", () => Ammo(3, 0.13f, 0.34f, new Color(0.80f, 0.60f, 0.20f), new Color(0.72f, 0.42f, 0.22f)) },
        { "Item_Ammo_Rifle", () => Ammo(3, 0.12f, 0.62f, new Color(0.78f, 0.58f, 0.18f), new Color(0.55f, 0.55f, 0.58f)) },
        { "Item_Ammo_Heavy", () => Ammo(2, 0.20f, 0.95f, new Color(0.72f, 0.52f, 0.16f), new Color(0.22f, 0.22f, 0.24f)) },
    };

    private static Material Mat(Color color, float smoothness = 0.35f, float metallic = 0f)
    {
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { hideFlags = HideFlags.HideAndDontSave };
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_Metallic", metallic);
        return material;
    }

    // 도형 하나를 부모 아래에 만든다. 위치는 바닥 기준 중심점, 크기는 지름/높이 같은 월드 크기다.
    private static GameObject Prim(PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Vector3 euler, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        go.transform.localEulerAngles = euler;
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
    }

    // Unity 실린더는 높이 2, 지름 1이다. 지름 d, 높이 h로 쓰려고 높이를 절반으로 넣는다.
    private static Vector3 Cyl(float diameter, float height) => new Vector3(diameter, height * 0.5f, diameter);

    private static GameObject Wood()
    {
        GameObject root = new GameObject("Icon_Wood");
        Material bark = Mat(new Color(0.40f, 0.26f, 0.14f), 0.15f);
        Material cut = Mat(new Color(0.78f, 0.62f, 0.40f), 0.1f);
        Vector3 along = new Vector3(0f, 0f, 90f);   // 실린더를 X축 방향으로 눕힌다
        foreach (float z in new[] { -0.17f, 0.17f })
        {
            Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.16f, z), Cyl(0.30f, 0.72f), along, bark);
            Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0.365f, 0.16f, z), Cyl(0.26f, 0.02f), along, cut);
        }
        Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.42f, 0f), Cyl(0.30f, 0.72f), along, bark);
        Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0.365f, 0.42f, 0f), Cyl(0.26f, 0.02f), along, cut);
        return root;
    }

    private static GameObject ScrapMetal()
    {
        GameObject root = new GameObject("Icon_Scrap");
        // 겹쳐 쌓인 찌그러진 판 두 장, 눕힌 파이프, 볼트를 밝은 강철색과 녹색으로 칠해 고철 더미로 읽히게 한다.
        Material steel = Mat(new Color(0.72f, 0.75f, 0.78f), 0.55f, 0.85f);
        Material rust = Mat(new Color(0.66f, 0.36f, 0.18f), 0.3f, 0.5f);
        Material dark = Mat(new Color(0.42f, 0.44f, 0.47f), 0.5f, 0.85f);
        Prim(PrimitiveType.Cube, root.transform, new Vector3(0f, 0.03f, 0f), new Vector3(0.75f, 0.06f, 0.55f), new Vector3(0f, 18f, 0f), steel);
        Prim(PrimitiveType.Cube, root.transform, new Vector3(0.05f, 0.11f, -0.02f), new Vector3(0.55f, 0.05f, 0.4f), new Vector3(4f, -32f, -7f), rust);
        Prim(PrimitiveType.Cylinder, root.transform, new Vector3(-0.08f, 0.22f, 0.1f), Cyl(0.16f, 0.62f), new Vector3(0f, 25f, 90f), dark);
        Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0.2f, 0.2f, -0.1f), Cyl(0.11f, 0.18f), Vector3.zero, steel);
        Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0.2f, 0.3f, -0.1f), Cyl(0.18f, 0.04f), Vector3.zero, steel);
        return root;
    }

    private static GameObject BasicMaterial()
    {
        GameObject root = new GameObject("Icon_BasicMaterial");
        Material stone = Mat(new Color(0.62f, 0.58f, 0.52f), 0.1f);
        Material stoneDark = Mat(new Color(0.50f, 0.47f, 0.42f), 0.1f);
        Vector3 brick = new Vector3(0.5f, 0.2f, 0.26f);
        for (int row = 0; row < 3; row++)
            for (int col = 0; col < 2; col++)
            {
                float offset = row % 2 == 0 ? 0f : 0.25f;
                Prim(PrimitiveType.Cube, root.transform, new Vector3(-0.26f + col * 0.52f + offset - 0.12f, 0.1f + row * 0.21f, 0f), brick, Vector3.zero, (row + col) % 2 == 0 ? stone : stoneDark);
            }
        return root;
    }

    private static GameObject RefinedMaterial()
    {
        GameObject root = new GameObject("Icon_RefinedMaterial");
        Material slab = Mat(new Color(0.74f, 0.78f, 0.82f), 0.55f, 0.55f);
        Material edge = Mat(new Color(0.46f, 0.52f, 0.60f), 0.5f, 0.6f);
        for (int i = 0; i < 3; i++)
        {
            Prim(PrimitiveType.Cube, root.transform, new Vector3(0f, 0.07f + i * 0.15f, 0f), new Vector3(0.7f, 0.13f, 0.42f), new Vector3(0f, i * 8f - 8f, 0f), i % 2 == 0 ? slab : edge);
        }
        return root;
    }

    private static GameObject Berries(Color color)
    {
        GameObject root = new GameObject("Icon_Berries");
        Material berry = Mat(color, 0.7f);
        Material leaf = Mat(new Color(0.22f, 0.48f, 0.18f), 0.3f);
        Prim(PrimitiveType.Sphere, root.transform, new Vector3(-0.14f, 0.13f, 0f), Vector3.one * 0.27f, Vector3.zero, berry);
        Prim(PrimitiveType.Sphere, root.transform, new Vector3(0.14f, 0.13f, 0.03f), Vector3.one * 0.27f, Vector3.zero, berry);
        Prim(PrimitiveType.Sphere, root.transform, new Vector3(0f, 0.13f, -0.2f), Vector3.one * 0.27f, Vector3.zero, berry);
        Prim(PrimitiveType.Sphere, root.transform, new Vector3(0f, 0.35f, -0.06f), Vector3.one * 0.27f, Vector3.zero, berry);
        Prim(PrimitiveType.Sphere, root.transform, new Vector3(0.04f, 0.5f, -0.06f), new Vector3(0.24f, 0.04f, 0.14f), new Vector3(0f, 30f, 20f), leaf);
        return root;
    }

    // 손잡이가 달린 랜턴이다: 금속 받침과 뚜껑 사이로 따뜻하게 빛나는 유리통.
    private static GameObject Lantern()
    {
        GameObject root = new GameObject("Icon_Lantern");
        Material metal = Mat(new Color(0.2f, 0.2f, 0.22f), 0.45f, 0.8f);
        Material glass = Mat(new Color(1f, 0.74f, 0.3f), 0.85f);
        Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.04f, 0f), Cyl(0.52f, 0.1f), Vector3.zero, metal);
        Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.34f, 0f), Cyl(0.42f, 0.5f), Vector3.zero, glass);
        Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.66f, 0f), Cyl(0.56f, 0.1f), Vector3.zero, metal);
        Prim(PrimitiveType.Cube, root.transform, new Vector3(0f, 0.9f, 0f), new Vector3(0.34f, 0.04f, 0.04f), Vector3.zero, metal);
        Prim(PrimitiveType.Cube, root.transform, new Vector3(-0.17f, 0.78f, 0f), new Vector3(0.04f, 0.26f, 0.04f), Vector3.zero, metal);
        Prim(PrimitiveType.Cube, root.transform, new Vector3(0.17f, 0.78f, 0f), new Vector3(0.04f, 0.26f, 0.04f), Vector3.zero, metal);
        return root;
    }

    // 방어구 아이콘 모델이다. 색으로 가죽(갈색)과 철(회색)을 구분하고, 철은 금속 재질에 보강 장식을 더한다.
    private static Material ArmorMaterial(Color color, bool iron) => iron ? Mat(color, 0.55f, 0.85f) : Mat(color, 0.25f);

    private static GameObject Helmet(Color color, bool iron)
    {
        GameObject root = new GameObject("Icon_Helmet");
        Material body = ArmorMaterial(color, iron);
        Material trim = ArmorMaterial(color * 0.65f, iron);
        // 돔(위쪽 반구처럼 보이게 아래를 챙으로 가린다)과 챙, 철제는 앞에 코 가리개를 단다.
        Prim(PrimitiveType.Sphere, root.transform, new Vector3(0f, 0.3f, 0f), new Vector3(0.62f, 0.52f, 0.62f), Vector3.zero, body);
        Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.1f, 0f), Cyl(0.7f, 0.07f), Vector3.zero, trim);
        if (iron)
        {
            Prim(PrimitiveType.Cube, root.transform, new Vector3(0f, 0.28f, 0.31f), new Vector3(0.07f, 0.34f, 0.04f), Vector3.zero, trim);
            Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.55f, 0f), Cyl(0.09f, 0.05f), Vector3.zero, trim);
        }
        return root;
    }

    private static GameObject Chest(Color color, bool iron)
    {
        GameObject root = new GameObject("Icon_Chest");
        Material body = ArmorMaterial(color, iron);
        Material trim = ArmorMaterial(color * 0.65f, iron);
        // 몸통과 두 어깨끈, 허리 띠. 철제는 가슴판과 어깨 보호대가 도드라진다.
        Prim(PrimitiveType.Cube, root.transform, new Vector3(0f, 0.38f, 0f), new Vector3(0.66f, 0.7f, 0.32f), Vector3.zero, body);
        Prim(PrimitiveType.Cube, root.transform, new Vector3(-0.22f, 0.78f, 0f), new Vector3(0.14f, 0.1f, 0.26f), Vector3.zero, trim);
        Prim(PrimitiveType.Cube, root.transform, new Vector3(0.22f, 0.78f, 0f), new Vector3(0.14f, 0.1f, 0.26f), Vector3.zero, trim);
        Prim(PrimitiveType.Cube, root.transform, new Vector3(0f, 0.1f, 0f), new Vector3(0.7f, 0.1f, 0.36f), Vector3.zero, trim);
        if (iron)
        {
            Prim(PrimitiveType.Cube, root.transform, new Vector3(0f, 0.46f, 0.17f), new Vector3(0.4f, 0.4f, 0.05f), Vector3.zero, trim);
            Prim(PrimitiveType.Sphere, root.transform, new Vector3(-0.38f, 0.74f, 0f), new Vector3(0.24f, 0.2f, 0.28f), Vector3.zero, body);
            Prim(PrimitiveType.Sphere, root.transform, new Vector3(0.38f, 0.74f, 0f), new Vector3(0.24f, 0.2f, 0.28f), Vector3.zero, body);
        }
        return root;
    }

    private static GameObject Legs(Color color, bool iron)
    {
        GameObject root = new GameObject("Icon_Legs");
        Material body = ArmorMaterial(color, iron);
        Material trim = ArmorMaterial(color * 0.65f, iron);
        // 두 다리와 무릎 보호대, 허리 띠. 철제는 정강이 보호대가 더 크다.
        foreach (float x in new[] { -0.19f, 0.19f })
        {
            Prim(PrimitiveType.Cylinder, root.transform, new Vector3(x, 0.4f, 0f), Cyl(iron ? 0.3f : 0.27f, 0.8f), Vector3.zero, body);
            Prim(PrimitiveType.Sphere, root.transform, new Vector3(x, 0.45f, 0.08f), new Vector3(0.28f, 0.22f, 0.26f), Vector3.zero, trim);
        }
        Prim(PrimitiveType.Cube, root.transform, new Vector3(0f, 0.8f, 0f), new Vector3(0.7f, 0.1f, 0.3f), Vector3.zero, trim);
        return root;
    }

    // 접어 포갠 가죽 두 장과 말아 둔 가죽 한 통이다.
    private static GameObject Leather()
    {
        GameObject root = new GameObject("Icon_Leather");
        Material tan = Mat(new Color(0.62f, 0.40f, 0.20f), 0.3f);
        Material dark = Mat(new Color(0.46f, 0.28f, 0.14f), 0.3f);
        Prim(PrimitiveType.Cube, root.transform, new Vector3(0f, 0.05f, 0f), new Vector3(0.78f, 0.1f, 0.58f), new Vector3(0f, 12f, 0f), dark);
        Prim(PrimitiveType.Cube, root.transform, new Vector3(0.03f, 0.14f, 0.02f), new Vector3(0.7f, 0.08f, 0.5f), new Vector3(0f, -10f, 0f), tan);
        Prim(PrimitiveType.Cylinder, root.transform, new Vector3(-0.05f, 0.3f, 0.02f), Cyl(0.22f, 0.62f), new Vector3(0f, 25f, 90f), tan);
        Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0.24f, 0.3f, -0.07f), Cyl(0.2f, 0.02f), new Vector3(0f, 25f, 90f), dark);
        return root;
    }

    // 말아 둔 흰 붕대에 붉은 띠를 두른다.
    private static GameObject Bandage()
    {
        GameObject root = new GameObject("Icon_Bandage");
        Material cloth = Mat(new Color(0.95f, 0.94f, 0.90f), 0.2f);
        Material red = Mat(new Color(0.82f, 0.14f, 0.16f), 0.3f);
        Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.2f, 0f), Cyl(0.5f, 0.4f), Vector3.zero, cloth);
        Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.2f, 0f), Cyl(0.52f, 0.12f), Vector3.zero, red);
        Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.405f, 0f), Cyl(0.18f, 0.02f), Vector3.zero, red);
        Prim(PrimitiveType.Cube, root.transform, new Vector3(0.38f, 0.04f, 0.1f), new Vector3(0.5f, 0.05f, 0.22f), new Vector3(0f, 20f, 0f), cloth);
        return root;
    }

    // 뼈가 붙은 고기 덩이다. 살코기 두 덩이를 겹치고 한쪽에 뼈를 낸다.
    private static GameObject Meat(Color meat, Color fat)
    {
        GameObject root = new GameObject("Icon_Meat");
        Material flesh = Mat(meat, 0.6f);
        Material marbling = Mat(fat, 0.5f);
        Material bone = Mat(new Color(0.93f, 0.90f, 0.82f), 0.3f);
        Prim(PrimitiveType.Sphere, root.transform, new Vector3(0f, 0.19f, 0f), new Vector3(0.7f, 0.38f, 0.5f), new Vector3(0f, 20f, 0f), flesh);
        Prim(PrimitiveType.Sphere, root.transform, new Vector3(-0.06f, 0.33f, 0.04f), new Vector3(0.46f, 0.22f, 0.34f), new Vector3(0f, 20f, 0f), marbling);
        Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0.43f, 0.22f, -0.14f), Cyl(0.1f, 0.4f), new Vector3(0f, 20f, 80f), bone);
        Prim(PrimitiveType.Sphere, root.transform, new Vector3(0.62f, 0.23f, -0.21f), Vector3.one * 0.17f, Vector3.zero, bone);
        Prim(PrimitiveType.Sphere, root.transform, new Vector3(0.6f, 0.23f, -0.07f), Vector3.one * 0.15f, Vector3.zero, bone);
        return root;
    }

    private static GameObject Can(Color label, bool steaming)
    {
        GameObject root = new GameObject("Icon_Can");
        Material metal = Mat(new Color(0.72f, 0.74f, 0.77f), 0.7f, 0.9f);
        Material band = Mat(label, 0.3f);
        Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.25f, 0f), Cyl(0.46f, 0.5f), Vector3.zero, metal);
        Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.25f, 0f), Cyl(0.475f, 0.3f), Vector3.zero, band);
        Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.505f, 0f), Cyl(0.40f, 0.02f), Vector3.zero, metal);
        if (steaming)
        {
            Material steam = Mat(new Color(0.95f, 0.95f, 0.97f), 0.1f);
            Prim(PrimitiveType.Sphere, root.transform, new Vector3(-0.07f, 0.62f, 0f), Vector3.one * 0.13f, Vector3.zero, steam);
            Prim(PrimitiveType.Sphere, root.transform, new Vector3(0.07f, 0.74f, 0.02f), Vector3.one * 0.10f, Vector3.zero, steam);
            Prim(PrimitiveType.Sphere, root.transform, new Vector3(-0.03f, 0.84f, 0f), Vector3.one * 0.08f, Vector3.zero, steam);
        }
        return root;
    }

    private static GameObject Bottle(Color liquid)
    {
        GameObject root = new GameObject("Icon_Bottle");
        Material body = Mat(liquid, 0.9f);
        Material cap = Mat(new Color(0.18f, 0.2f, 0.24f), 0.4f);
        Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.27f, 0f), Cyl(0.34f, 0.54f), Vector3.zero, body);
        Prim(PrimitiveType.Sphere, root.transform, new Vector3(0f, 0.54f, 0f), new Vector3(0.34f, 0.2f, 0.34f), Vector3.zero, body);
        Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.68f, 0f), Cyl(0.15f, 0.2f), Vector3.zero, body);
        Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.8f, 0f), Cyl(0.19f, 0.08f), Vector3.zero, cap);
        return root;
    }

    // 탄피(황동)와 탄두를 세워 둔 탄약 묶음이다. count개를 삼각형/한 줄로 배치한다.
    private static GameObject Ammo(int count, float diameter, float length, Color brass, Color tip)
    {
        GameObject root = new GameObject("Icon_Ammo");
        Material caseMat = Mat(brass, 0.75f, 0.9f);
        Material tipMat = Mat(tip, 0.7f, 0.8f);
        float caseLength = length * 0.62f;
        float tipLength = length * 0.38f;
        for (int i = 0; i < count; i++)
        {
            float x = (i - (count - 1) * 0.5f) * (diameter * 1.35f);
            float z = (i % 2 == 0 ? 0f : 0.08f);
            Prim(PrimitiveType.Cylinder, root.transform, new Vector3(x, caseLength * 0.5f, z), Cyl(diameter, caseLength), Vector3.zero, caseMat);
            Prim(PrimitiveType.Cylinder, root.transform, new Vector3(x, caseLength * 0.04f, z), Cyl(diameter * 1.12f, caseLength * 0.08f), Vector3.zero, caseMat);
            Prim(PrimitiveType.Capsule, root.transform, new Vector3(x, caseLength + tipLength * 0.38f, z), new Vector3(diameter * 0.92f, tipLength * 0.5f, diameter * 0.92f), Vector3.zero, tipMat);
        }
        return root;
    }
}
