// 그래픽 품질 3단계(낮음, 중간, 높음)를 엔진에 적용하는 정적 클래스다. 높음은 원래 설정이고, 낮음/중간은 렌더 스케일, 그림자, 추가 조명, SSAO, 지형 잔디, 커스텀 셰이더 효과를 줄인다.
// 렌더 파이프라인 에셋은 복제본을 써서 원본 에셋이 바뀌지 않게 하고, 끝날 때 원래대로 되돌린다.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class GraphicsPresets
{
    // 커스텀 셰이더가 읽는 전역 값이다. 0=높음(전체 효과), 1=중간, 2=낮음(극최소). 값을 안 넣은 곳(에디터 편집 화면)은 0이라 전체 효과가 나온다.
    private static readonly int CutId = Shader.PropertyToID("_ZGV_Cut");

    // 낮은 단계에서 끄는 효과(자원 반짝임 등)가 읽는다.
    public static int ShaderCut { get; private set; }

    private struct Original
    {
        public float renderScale, shadowDistance, lodBias;
        public int shadowCascades, mainShadowResolution, additionalShadowResolution, maxAdditionalLights;
    }

    private static bool captured;
    private static Original original;
    private static RenderPipelineAsset originalAsset;
    private static UniversalRenderPipelineAsset clone;
    private static bool ssaoWasActive = true;
    private static ScriptableRendererFeature ssao;
    private static int currentPreset = GameSettings.GraphicsHigh;

    // 지형(씬마다 있다)의 원래 풀/나무 값이다.
    private struct TerrainOriginal
    {
        public float detailDensity, detailDistance;
        public int fullLodCount;
    }
    private static readonly Dictionary<Terrain, TerrainOriginal> terrains = new Dictionary<Terrain, TerrainOriginal>();

    // 씬이 열릴 때마다 그 씬의 지형에 현재 품질을 다시 적용한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ReapplyTerrainAfterSceneLoad()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyTerrain(currentPreset);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ApplyTerrain(currentPreset);

    public static void Apply(int preset)
    {
        currentPreset = Mathf.Clamp(preset, GameSettings.GraphicsLow, GameSettings.GraphicsHigh);
        ShaderCut = GameSettings.GraphicsHigh - currentPreset;
        Shader.SetGlobalFloat(CutId, ShaderCut);

        ApplyPipeline(currentPreset);
        ApplyTerrain(currentPreset);
    }

    // ---- 렌더 파이프라인 ----

    private static void ApplyPipeline(int preset)
    {
        if (!captured && !Capture()) return;

        bool low = preset == GameSettings.GraphicsLow, medium = preset == GameSettings.GraphicsMedium;
        UniversalRenderPipelineAsset asset = clone;
        asset.renderScale = low ? Mathf.Min(0.85f, original.renderScale) : original.renderScale;
        asset.shadowDistance = low ? Mathf.Min(20f, original.shadowDistance) : medium ? Mathf.Min(35f, original.shadowDistance) : original.shadowDistance;
        asset.shadowCascadeCount = low ? 1 : medium ? Mathf.Min(2, original.shadowCascades) : original.shadowCascades;
        asset.mainLightShadowmapResolution = low ? Mathf.Min(1024, original.mainShadowResolution) : original.mainShadowResolution;
        asset.additionalLightsShadowmapResolution = low ? Mathf.Min(512, original.additionalShadowResolution) : medium ? Mathf.Min(1024, original.additionalShadowResolution) : original.additionalShadowResolution;
        asset.maxAdditionalLightsCount = low ? Mathf.Min(2, original.maxAdditionalLights) : medium ? Mathf.Min(3, original.maxAdditionalLights) : original.maxAdditionalLights;
        QualitySettings.lodBias = low ? Mathf.Min(0.7f, original.lodBias) : original.lodBias;
        if (ssao != null) ssao.SetActive(!low && ssaoWasActive);
    }

    // 현재 URP 에셋의 원래 값을 기억하고 복제본으로 바꿔 쓴다. URP가 아니면 건너뛴다.
    private static bool Capture()
    {
        originalAsset = GraphicsSettings.currentRenderPipeline;
        UniversalRenderPipelineAsset urp = originalAsset as UniversalRenderPipelineAsset;
        if (urp == null) return false;

        original = new Original
        {
            renderScale = urp.renderScale,
            shadowDistance = urp.shadowDistance,
            lodBias = QualitySettings.lodBias,
            shadowCascades = urp.shadowCascadeCount,
            mainShadowResolution = urp.mainLightShadowmapResolution,
            maxAdditionalLights = urp.maxAdditionalLightsCount,
            additionalShadowResolution = urp.additionalLightsShadowmapResolution,
        };

        clone = UnityEngine.Object.Instantiate(urp);
        QualitySettings.renderPipeline = clone;
        FindSsao(urp);
        captured = true;
        Application.quitting += Restore;
        return true;
    }

    // SSAO 렌더러 기능을 찾는다(렌더러 데이터 목록이 공개돼 있지 않아 리플렉션으로 읽는다. 못 찾으면 SSAO는 건드리지 않는다).
    private static void FindSsao(UniversalRenderPipelineAsset asset)
    {
        try
        {
            FieldInfo list = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", BindingFlags.NonPublic | BindingFlags.Instance);
            if (list == null || !(list.GetValue(asset) is ScriptableRendererData[] datas)) return;
            foreach (ScriptableRendererData data in datas)
            {
                if (data == null) continue;
                foreach (ScriptableRendererFeature feature in data.rendererFeatures)
                {
                    if (feature == null || feature.GetType().Name != "ScreenSpaceAmbientOcclusion") continue;
                    ssao = feature;
                    ssaoWasActive = feature.isActive;
                    return;
                }
            }
        }
        catch (Exception exception) when (exception is TargetException || exception is InvalidCastException)
        {
            ssao = null;
        }
    }

    // 끝날 때 원래 에셋과 값으로 되돌린다(에디터에서 프로젝트 설정이 바뀐 채 남지 않게).
    private static void Restore()
    {
        Application.quitting -= Restore;
        if (!captured) return;

        if (ssao != null) ssao.SetActive(ssaoWasActive);
        QualitySettings.lodBias = original.lodBias;
        QualitySettings.renderPipeline = originalAsset;
        if (clone != null) UnityEngine.Object.Destroy(clone);
        foreach (KeyValuePair<Terrain, TerrainOriginal> pair in terrains)
        {
            if (pair.Key == null) continue;
            pair.Key.detailObjectDensity = pair.Value.detailDensity;
            pair.Key.detailObjectDistance = pair.Value.detailDistance;
            pair.Key.treeMaximumFullLODCount = pair.Value.fullLodCount;
        }
        captured = false;
    }

    // ---- 지형 풀 ----

    private static void ApplyTerrain(int preset)
    {
        bool low = preset == GameSettings.GraphicsLow, medium = preset == GameSettings.GraphicsMedium;
        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            if (!terrains.TryGetValue(terrain, out TerrainOriginal o))
            {
                o = new TerrainOriginal { detailDensity = terrain.detailObjectDensity, detailDistance = terrain.detailObjectDistance, fullLodCount = terrain.treeMaximumFullLODCount };
                terrains[terrain] = o;
            }
            // 낮음은 잔디가 없다. 중간은 절반 밀도로 가까운 곳만 보인다.
            terrain.detailObjectDensity = low ? 0f : medium ? o.detailDensity * 0.5f : o.detailDensity;
            // 원래 거리가 0으로 읽히는 지형은 거리를 건드리지 않고 밀도로만 조절한다.
            if (o.detailDistance > 0f) terrain.detailObjectDistance = low ? 0f : medium ? Mathf.Min(40f, o.detailDistance) : o.detailDistance;
            terrain.treeMaximumFullLODCount = low ? Mathf.Min(20, o.fullLodCount) : medium ? Mathf.Min(50, o.fullLodCount) : o.fullLodCount;
        }
    }
}
