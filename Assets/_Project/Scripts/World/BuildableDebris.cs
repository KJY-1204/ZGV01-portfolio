// 건축물이 부서지거나 무너질 때의 연출이다. 모델과 같은 재질의 파편 조각을 튀기고 먼지를 일으킨 뒤 원래 오브젝트를 없앤다.
using UnityEngine;

public static class BuildableDebris
{
    private static Material smokeMaterial;
    private static bool smokeSearched;

    // delay 뒤에 인스턴스를 무너뜨린다. 0이면 바로 처리한다. 이미 예약된 인스턴스는 한 번만 처리한다.
    public static void Crumble(BuildableInstance instance, float delay)
    {
        if (instance == null) return;
        // 부서지든 철거되든 보관함 내용물은 그 자리에 떨어뜨린다(한 번만 처리된다).
        StorageContainer storage = instance.GetComponent<StorageContainer>();
        if (storage != null) storage.DropContents();
        // 부활 지점(침대)이 사라지면 지정한 플레이어에게 알린다.
        RespawnPoint respawnPoint = instance.GetComponent<RespawnPoint>();
        if (respawnPoint != null) respawnPoint.NotifyRemoved();
        if (instance.GetComponent<BuildableCrumble>() != null) return;

        instance.gameObject.AddComponent<BuildableCrumble>().Begin(delay);
    }

    // 경계 상자 안에서 파편 조각을 만들어 사방으로 튀긴다(부피에 비례해 6~18개).
    public static void SpawnPieces(Bounds bounds, Material material)
    {
        float volume = bounds.size.x * bounds.size.y * bounds.size.z;
        int count = Mathf.Clamp(Mathf.RoundToInt(volume * 3f), 6, 18);

        for (int i = 0; i < count; i++)
        {
            GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.name = "Debris";
            Vector3 point = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                Random.Range(bounds.min.z, bounds.max.z));
            piece.transform.SetPositionAndRotation(point, Random.rotation);
            piece.transform.localScale = new Vector3(Random.Range(0.12f, 0.32f), Random.Range(0.1f, 0.26f), Random.Range(0.12f, 0.32f));

            MeshRenderer renderer = piece.GetComponent<MeshRenderer>();
            if (material != null) renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            Rigidbody body = piece.AddComponent<Rigidbody>();
            body.mass = 1.5f;
            Vector3 outward = point - bounds.center;
            outward.y = 0f;
            body.linearVelocity = outward.normalized * Random.Range(0.8f, 2.8f) + Vector3.up * Random.Range(1f, 3.2f);
            body.angularVelocity = Random.insideUnitSphere * 8f;

            piece.AddComponent<DebrisPiece>();
        }
    }

    // 먼지구름 한 번. 파티클 재질은 탄착 이펙트의 연기 재질을 빌려 쓴다(프로젝트에 추적되는 에셋).
    public static void SpawnDust(Bounds bounds, int burst)
    {
        Material smoke = FindSmokeMaterial();
        if (smoke == null) return;

        GameObject go = new GameObject("DebrisDust");
        // 비활성 상태에서 모듈을 설정한 뒤 켜야 기본 설정으로 먼저 재생되지 않는다.
        go.SetActive(false);
        go.transform.position = bounds.center;

        ParticleSystem particles = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 1.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.9f, 2.0f);
        main.startColor = new Color(0.62f, 0.6f, 0.56f, 0.5f);
        main.gravityModifier = -0.02f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 64;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Clamp(burst, 4, 40)) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = bounds.size;

        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;

        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.5f));

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = smoke;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        go.SetActive(true);
        particles.Play();
        Object.Destroy(go, 3f);
    }

    // 모델에서 가장 큰 렌더러의 재질을 파편에 쓴다.
    public static Material PickMaterial(GameObject root)
    {
        Material best = null;
        float bestSize = 0f;
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
        {
            if (renderer is ParticleSystemRenderer || renderer.sharedMaterial == null) continue;
            float size = renderer.bounds.size.sqrMagnitude;
            if (size <= bestSize) continue;
            bestSize = size;
            best = renderer.sharedMaterial;
        }
        return best;
    }

    // 탄착 이펙트(콘크리트)의 "Directional Smoke" 입자 재질이다.
    private static Material FindSmokeMaterial()
    {
        if (smokeMaterial != null || smokeSearched) return smokeMaterial;
        smokeSearched = true;

        ImpactEffectLibrary library = ImpactEffectLibrary.Instance;
        GameObject effect = library != null ? library.GetEffect(ImpactSurface.Concrete) : null;
        if (effect == null) return null;

        foreach (ParticleSystemRenderer renderer in effect.GetComponentsInChildren<ParticleSystemRenderer>(true))
        {
            if (renderer.sharedMaterial != null && renderer.sharedMaterial.name.Contains("Smoke"))
            {
                smokeMaterial = renderer.sharedMaterial;
                break;
            }
        }
        return smokeMaterial;
    }
}
