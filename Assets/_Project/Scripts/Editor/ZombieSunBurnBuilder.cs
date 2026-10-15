// 좀비 프리팹에 햇빛 불타 죽음(ZombieSunBurn)과 불꽃 이펙트 프리팹을 붙이는 에디터 도구다. 여러 번 실행해도 같은 결과가 되며, 좀비 빌더가 새 프리팹을 만들 때도 부른다.
using UnityEditor;
using UnityEngine;

public static class ZombieSunBurnBuilder
{
    private const string EffectPath = "Assets/_Project/Prefabs/Effects/Effect_ZombieBurn.prefab";
    private const string FlameMaterialPath = "Assets/_Project/Materials/Cooking/Mat_Flame.mat";
    private static readonly string[] ZombiePrefabs =
    {
        "Assets/_Project/Prefabs/Characters/Zombie_Walker.prefab",
        "Assets/_Project/Prefabs/Characters/Zombie_Runner.prefab",
        "Assets/_Project/Prefabs/Characters/Zombie_Mutant.prefab",
        "Assets/_Project/Prefabs/Characters/Zombie_Archer.prefab",
    };

    [MenuItem("ZGV01/Zombie/Add Sun Burn")]
    public static void Build()
    {
        foreach (string path in ZombiePrefabs)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            Attach(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }
        AssetDatabase.SaveAssets();
    }

    // 좀비 루트에 ZombieSunBurn을 붙이고 불꽃 이펙트를 연결한다.
    public static void Attach(GameObject zombie)
    {
        ZombieSunBurn burn = zombie.GetComponent<ZombieSunBurn>();
        if (burn == null) burn = zombie.AddComponent<ZombieSunBurn>();
        SerializedObject so = new SerializedObject(burn);
        so.FindProperty("burnEffect").objectReferenceValue = LoadOrBuildEffect();
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // 기존 불꽃 재질을 쓰는 이펙트 프리팹이다. 키 1.8m 체격 기준이고 좀비 체격에 맞춰 키워 쓴다.
    private static GameObject LoadOrBuildEffect()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(EffectPath);
        if (existing != null) return existing;

        System.IO.Directory.CreateDirectory("Assets/_Project/Prefabs/Effects");
        Material flame = AssetDatabase.LoadAssetAtPath<Material>(FlameMaterialPath);
        GameObject root = new GameObject("Effect_ZombieBurn");
        GameObject body = new GameObject("Flame");
        body.transform.SetParent(root.transform, false);

        ParticleSystem system = body.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = system.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.5f, 0.1f, 0.9f), new Color(1f, 0.8f, 0.3f, 0.9f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 80;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = 45f;

        // 발치에서 몸통 위로 올라가는 불길이다.
        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 12f;
        shape.radius = 0.35f;
        shape.position = new Vector3(0f, -0.7f, 0f);
        shape.rotation = new Vector3(-90f, 0f, 0f);

        ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.25f)));

        ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(new Color(1f, 0.85f, 0.4f), 0f), new GradientColorKey(new Color(1f, 0.3f, 0.05f), 0.6f), new GradientColorKey(new Color(0.3f, 0.05f, 0f), 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;

        ParticleSystemRenderer particleRenderer = body.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sharedMaterial = flame;
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, EffectPath);
        Object.DestroyImmediate(root);
        return saved;
    }
}
