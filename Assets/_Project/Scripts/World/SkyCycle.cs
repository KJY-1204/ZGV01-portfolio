// GameClock의 시각(해의 고도)에 맞춰 밤/노을/낮 큐브맵 하늘을 서서히 섞고, 호를 그리며 이동하는 해와 달 원반, 흐르는 구름과 별, 태양 색을 갱신하는 컴포넌트다. 노을은 태양 방위에 맞춘다.
using UnityEngine;

public class SkyCycle : MonoBehaviour
{
    [SerializeField] private Shader skyShader;
    // 그래픽 품질이 낮음일 때 쓰는 단순 하늘 셰이더(내장 Skybox/Cubemap)다. 낮 큐브맵 한 장에 시각별 색조만 입히고 해/달 원반, 구름/별 흐름은 없다.
    [SerializeField] private Shader simpleSkyShader;
    [SerializeField] private Cubemap nightSky;
    [SerializeField] private Cubemap glowSky;
    [SerializeField] private Cubemap daySky;
    // 태양(방향성 조명)이다. 비어 있으면 RenderSettings.sun을 쓴다.
    [SerializeField] private Light sun;
    // 큐브맵에 구워진 노을 빛의 방위(도, 0이 +Z)다. 회전할 때 이 값을 기준으로 맞춘다.
    [SerializeField] private float glowBakedAzimuth = -9f;
    // 밤 큐브맵에 구워진 달의 방위/고도(도)다. 진짜 달은 원반으로 따로 그리므로 이 자리를 가린다.
    [SerializeField] private float nightBakedAzimuth = -3f;
    [SerializeField] private float nightBakedElevation = 24.5f;
    // 밤 하늘(별과 구름)이 돌아가는 속도(게임 한 시간당 도)다.
    [SerializeField] private float nightDriftPerHour = 7.5f;
    // 낮 하늘 구름이 흐르는 속도(게임 한 시간당 도)다.
    [SerializeField] private float dayDriftPerHour = 3f;
    // 하루(0~1)에 걸친 태양 색이다. 직렬화하면 씬에 기본(흰색) 값이 저장돼 코드 값이 덮이므로 상수로 둔다.
    private static readonly Gradient SunColor = DefaultSunColor();
    // 환경광(하늘에서 오는 빛)을 다시 계산하는 간격(현실 초)이다. DynamicGI.UpdateEnvironment가 한 번에 10ms 넘게 걸려(개발 빌드 측정)
    // 0.75초마다 부르면 약 1%의 프레임이 끊겨서 3초로 늘렸다. 하루가 1200초라 3초는 게임 시간으로 약 4분이다.
    [SerializeField] private float ambientRefreshSeconds = 3f;

    // 밤 하늘 노출과, 해가 낮을 때 낮 하늘에 입히는 주황빛이다.
    private const float NightExposure = 0.8f;
    private static readonly Color WarmDayTint = new Color(1f, 0.76f, 0.58f);

    // 낮음 품질의 단순 하늘 색조다(밤은 어두운 푸른빛, 노을은 주황빛).
    private static readonly Color NightTint = new Color(0.04f, 0.06f, 0.14f);
    private static readonly Color GlowTint = new Color(1f, 0.55f, 0.35f);

    private Material sky;
    private Material cheapSky;
    private bool cheapActive;
    private float nextAmbientRefresh;

    private static Gradient DefaultSunColor()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.55f, 0.62f, 0.9f), 0f),
                new GradientColorKey(new Color(0.55f, 0.62f, 0.9f), 0.2f),
                new GradientColorKey(new Color(1f, 0.62f, 0.38f), 0.245f),
                new GradientColorKey(new Color(1f, 0.88f, 0.74f), 0.31f),
                new GradientColorKey(new Color(1f, 0.97f, 0.92f), 0.5f),
                new GradientColorKey(new Color(1f, 0.88f, 0.7f), 0.69f),
                new GradientColorKey(new Color(1f, 0.52f, 0.3f), 0.775f),
                // 그라디언트의 색 키는 최대 8개다. 마지막 키 뒤는 그 색이 이어진다.
                new GradientColorKey(new Color(0.55f, 0.62f, 0.9f), 0.85f),
            },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        return gradient;
    }

    private void Awake()
    {
        if (skyShader == null) return;
        // 에셋을 건드리지 않도록 실행 중 복제본 재질을 쓴다.
        sky = new Material(skyShader) { name = "Sky_DayNight (runtime)", hideFlags = HideFlags.HideAndDontSave };
        if (simpleSkyShader != null) cheapSky = new Material(simpleSkyShader) { name = "Sky_Simple (runtime)", hideFlags = HideFlags.HideAndDontSave };
        cheapActive = WantsCheapSky();
        RenderSettings.skybox = cheapActive ? cheapSky : sky;
        if (sun == null) sun = RenderSettings.sun;
        Apply(GameClock.Current != null ? GameClock.Current.Hour : 12f);
        DynamicGI.UpdateEnvironment();
    }

    private void OnDestroy()
    {
        if (sky != null) Destroy(sky);
        if (cheapSky != null) Destroy(cheapSky);
    }

    // 그래픽 품질이 낮음이고 단순 하늘을 만들 수 있으면 참이다.
    private bool WantsCheapSky() => cheapSky != null && GraphicsPresets.ShaderCut >= 2;

    private void Update()
    {
        if (sky == null || GameClock.Current == null) return;

        // 옵션에서 품질이 바뀌면 하늘 셰이더를 바꿔 끼운다.
        bool wantCheap = WantsCheapSky();
        if (wantCheap != cheapActive)
        {
            cheapActive = wantCheap;
            RenderSettings.skybox = cheapActive ? cheapSky : sky;
        }
        Apply(GameClock.Current.Hour);

        if (Time.unscaledTime >= nextAmbientRefresh)
        {
            nextAmbientRefresh = Time.unscaledTime + ambientRefreshSeconds;
            DynamicGI.UpdateEnvironment();
        }
    }

    // 시각 hour의 하늘 혼합과 회전, 태양 색을 적용한다. 세 하늘의 비중은 시계가 아니라 해의 고도로 정해 서서히 이어지게 한다.
    public void Apply(float hour)
    {
        if (sky == null) return;

        float elevation = SkyBlendWeights.ElevationDegrees(hour);
        SkyBlendWeights.Weights weights = SkyBlendWeights.At(elevation);
        if (cheapActive)
        {
            ApplyCheap(hour, elevation, weights);
            return;
        }

        float sunAzimuth = 0f;
        Vector3 toSun = Vector3.up;
        if (sun != null)
        {
            toSun = -sun.transform.forward;
            sunAzimuth = Mathf.Atan2(toSun.x, toSun.z) * Mathf.Rad2Deg;
        }

        sky.SetTexture("_TexN", nightSky);
        sky.SetTexture("_TexG", glowSky);
        sky.SetTexture("_TexD", daySky);
        sky.SetVector("_Weights", new Vector4(weights.night, weights.glow, weights.day, 0f));
        sky.SetVector("_Exposures", new Vector4(NightExposure, SkyBlendWeights.GlowExposure(elevation), SkyBlendWeights.DayExposure(elevation), 0f));
        // 밤 하늘(별과 구름)은 자정을 지나도 이어지게 -4~6으로 세고, 노을은 태양 방위에 맞추고, 낮 구름은 천천히 흐른다.
        sky.SetVector("_Rotations", new Vector4((hour >= 12f ? hour - 24f : hour) * nightDriftPerHour, sunAzimuth - glowBakedAzimuth, hour * dayDriftPerHour, 0f));
        sky.SetColor("_DayTint", Color.Lerp(Color.white, WarmDayTint, SkyBlendWeights.DayWarmth(elevation)));

        // 구워진 달은 밤 하늘에서 가리고, 진짜 달은 해의 반대편에 원반으로 그린다.
        float bakedElevation = nightBakedElevation * Mathf.Deg2Rad, bakedAzimuth = nightBakedAzimuth * Mathf.Deg2Rad;
        sky.SetVector("_MoonMask", new Vector4(
            Mathf.Cos(bakedElevation) * Mathf.Sin(bakedAzimuth), Mathf.Sin(bakedElevation), Mathf.Cos(bakedElevation) * Mathf.Cos(bakedAzimuth), 1f));

        // 해는 지평선 근처에서 조금 더 크게 보인다.
        Color sunColor = SunColor.Evaluate(Mathf.Repeat(hour, 24f) / 24f);
        sky.SetVector("_SunDirInt", new Vector4(toSun.x, toSun.y, toSun.z, SkyBlendWeights.SunDisc(elevation)));
        sky.SetColor("_SunColor", sunColor);
        sky.SetFloat("_SunRadius", 0.045f * (1f + 0.5f * SkyBlendWeights.DayWarmth(elevation)));
        sky.SetVector("_MoonDirInt", new Vector4(-toSun.x, -toSun.y, -toSun.z, SkyBlendWeights.MoonDisc(elevation)));

        if (sun != null) sun.color = sunColor;
    }

    // 낮음 품질: 낮 큐브맵 한 장에 밤/노을/낮 비중으로 만든 색조를 곱한다(세 장을 섞지도, 해/달 원반을 그리지도 않는다). 태양 색은 그대로 갱신한다.
    private void ApplyCheap(float hour, float elevation, SkyBlendWeights.Weights weights)
    {
        Color dayTint = Color.Lerp(Color.white, WarmDayTint, SkyBlendWeights.DayWarmth(elevation)) * SkyBlendWeights.DayExposure(elevation);
        Color tint = NightTint * weights.night + GlowTint * (SkyBlendWeights.GlowExposure(elevation) * 0.7f * weights.glow) + dayTint * weights.day;
        // 내장 Skybox/Cubemap의 _Tint는 회색 0.5가 변화 없음이다.
        cheapSky.SetTexture("_Tex", daySky);
        cheapSky.SetColor("_Tint", tint * 0.5f);
        cheapSky.SetFloat("_Exposure", 1f);
        cheapSky.SetFloat("_Rotation", hour * dayDriftPerHour);

        Color sunColor = SunColor.Evaluate(Mathf.Repeat(hour, 24f) / 24f);
        if (sun != null) sun.color = sunColor;
    }
}
