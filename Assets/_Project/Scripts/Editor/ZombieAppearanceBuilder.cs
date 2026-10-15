// 좀비 종류별 외형 변형(크기 범위, 색조 팔레트)을 기존 프리팹에 제자리에서 적용하는 에디터 도구다. 프리팹을 지우고 다시 만들지 않아 스폰 표 참조가 유지된다.
using UnityEditor;
using UnityEngine;

public static class ZombieAppearanceBuilder
{
    private const string PrefabDir = "Assets/_Project/Prefabs/Characters/";

    private struct LookSpec
    {
        public Color body;
        public Color clothes;
        public float weight;

        public LookSpec(Color body, Color clothes, float weight)
        {
            this.body = body;
            this.clothes = clothes;
            this.weight = weight;
        }
    }

    private static Color C(float r, float g, float b) => new Color(r, g, b, 1f);

    [MenuItem("ZGV01/Zombie/Apply Appearances")]
    public static void Apply()
    {
        // 걷는 좀비: 보통 / 창백 / 병든 초록 / 멍든 어두움 / 피투성이 / 얼어붙은 파랑 / 누렇게 뜬 색
        ApplyTo("Zombie_Walker", new Vector2(0.92f, 1.12f), new[]
        {
            new LookSpec(C(1f, 1f, 1f), C(1f, 1f, 1f), 3f),
            new LookSpec(C(0.85f, 0.95f, 0.9f), C(0.9f, 0.9f, 0.95f), 2f),
            new LookSpec(C(0.75f, 1f, 0.7f), C(0.7f, 0.85f, 0.65f), 2f),
            new LookSpec(C(0.65f, 0.6f, 0.62f), C(0.55f, 0.5f, 0.5f), 2f),
            new LookSpec(C(1.15f, 0.72f, 0.68f), C(1f, 0.7f, 0.65f), 1.5f),
            new LookSpec(C(0.75f, 0.88f, 1.05f), C(0.6f, 0.72f, 0.95f), 1.5f),
            new LookSpec(C(1.05f, 1f, 0.6f), C(0.9f, 0.85f, 0.6f), 1f),
        });

        // 달리는 좀비: 마르고 작게, 창백하거나 열이 오른 색
        ApplyTo("Zombie_Runner", new Vector2(0.84f, 0.98f), new[]
        {
            new LookSpec(C(0.8f, 0.82f, 0.85f), C(0.7f, 0.7f, 0.75f), 3f),
            new LookSpec(C(1.15f, 0.7f, 0.65f), C(0.9f, 0.6f, 0.55f), 2f),
            new LookSpec(C(1.05f, 1f, 0.6f), C(0.85f, 0.8f, 0.55f), 1f),
            new LookSpec(C(0.6f, 0.55f, 0.58f), C(0.5f, 0.45f, 0.45f), 1f),
        });

        // 궁수 좀비: 흙빛 계열
        ApplyTo("Zombie_Archer", new Vector2(0.95f, 1.08f), new[]
        {
            new LookSpec(C(1f, 1f, 1f), C(1f, 1f, 1f), 3f),
            new LookSpec(C(0.9f, 0.9f, 0.9f), C(0.45f, 0.4f, 0.38f), 2f),
            new LookSpec(C(0.85f, 1f, 0.8f), C(0.6f, 0.7f, 0.55f), 1f),
            new LookSpec(C(0.9f, 0.95f, 0.95f), C(0.8f, 0.75f, 0.7f), 1f),
        });

        // 변이체: 몸집 차이가 크다(작은 개체부터 거대한 개체까지). 옷이 없어 몸 색조만 쓴다.
        ApplyTo("Zombie_Mutant", new Vector2(0.88f, 1.3f), new[]
        {
            new LookSpec(C(1f, 1f, 1f), C(1f, 1f, 1f), 2f),
            new LookSpec(C(0.55f, 0.55f, 0.6f), C(1f, 1f, 1f), 2f),
            new LookSpec(C(1.2f, 0.7f, 0.65f), C(1f, 1f, 1f), 2f),
            new LookSpec(C(0.75f, 1f, 0.7f), C(1f, 1f, 1f), 2f),
            new LookSpec(C(0.95f, 0.75f, 1.1f), C(1f, 1f, 1f), 1f),
            new LookSpec(C(1.3f, 0.9f, 0.6f), C(1f, 1f, 1f), 1f),
        });

        AssetDatabase.SaveAssets();
    }

    private static void ApplyTo(string prefabName, Vector2 scaleRange, LookSpec[] looks)
    {
        string path = PrefabDir + prefabName + ".prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);

        ZombieAppearance appearance = root.GetComponent<ZombieAppearance>();
        if (appearance == null) appearance = root.AddComponent<ZombieAppearance>();

        SerializedObject so = new SerializedObject(appearance);
        so.FindProperty("scaleRange").vector2Value = scaleRange;

        SerializedProperty list = so.FindProperty("looks");
        list.arraySize = looks.Length;
        for (int i = 0; i < looks.Length; i++)
        {
            SerializedProperty entry = list.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("body").colorValue = looks[i].body;
            entry.FindPropertyRelative("clothes").colorValue = looks[i].clothes;
            entry.FindPropertyRelative("weight").floatValue = looks[i].weight;
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }
}
