// 개발 빌드/에디터 전용 성능 측정 도구로, 대표 부하 상황을 자동으로 만들고 프레임 시간·CPU 구간·GC·드로우콜을 [perf] 로그로 남긴다.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;

// 실행 인자: -perf-scenario=idle|world|horde|base|mp  -perf-seconds=N(기본 30)  -perf-warmup=N(기본 5)
//           -perf-count=N(horde는 좀비 수 기본 40, base는 건축물 수 기본 300)  -perf-quit
// 에디터에서는 PerfProbe.Begin("horde", 20, 5, 40)처럼 직접 부른다.
public class PerfProbe : MonoBehaviour
{
    private const float FrameGuardSeconds = 1f;
    private const int FrameGuardMinFrames = 5;
    private const int GuardRetries = 30;
    private const float WorldSpeed = 10f;
    private const float WorldHalfSize = 150f;

    private static PerfProbe running;

    private string scenario;
    private float seconds;
    private float warmup;
    private int count;
    private bool quitWhenDone;
    private Vector3 origin;
    private float lastGroundY;

    // 빌드 인자로 켠다. 첫 씬이 메인 메뉴여도 게임 씬이 열리면 시작하게 씬 로드마다 확인한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Hook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryStartFromArgs();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TryStartFromArgs();

    private static void TryStartFromArgs()
    {
        if (running != null) return;
        string name = null;
        float seconds = 30f, warmup = 5f;
        int count = -1;
        bool quit = false, multiplayer = false;
        foreach (string arg in Environment.GetCommandLineArgs())
        {
            if (arg == "-mp-host" || arg.StartsWith("-mp-join=")) multiplayer = true;
            if (arg.StartsWith("-perf-scenario=")) name = arg.Substring("-perf-scenario=".Length);
            else if (arg.StartsWith("-perf-seconds=")) float.TryParse(arg.Substring("-perf-seconds=".Length), out seconds);
            else if (arg.StartsWith("-perf-warmup=")) float.TryParse(arg.Substring("-perf-warmup=".Length), out warmup);
            else if (arg.StartsWith("-perf-count=")) int.TryParse(arg.Substring("-perf-count=".Length), out count);
            else if (arg == "-perf-quit") quit = true;
        }
        if (string.IsNullOrEmpty(name)) return;
#if !UNITY_EDITOR
        Application.runInBackground = true; // 포커스를 못 받아도 멈추지 않게 한다.
#endif
        // 메인 메뉴에서 시작했으면 저장을 건드리지 않고 게임 씬을 바로 연다. 멀티플레이 자동 시작 인자가 있으면 그 흐름에 맡기고, 게임 씬이 열리면 다시 시작한다.
        if (GameSession.InMenuScene)
        {
            if (!multiplayer) SceneManager.LoadScene(GameSession.GameScene);
            return;
        }
        Begin(name, seconds, warmup, count, quit);
    }

    public static void Begin(string scenario, float seconds = 30f, float warmup = 5f, int count = -1, bool quitWhenDone = false)
    {
        if (running != null) { Debug.Log("[perf] 이미 측정 중이라 무시합니다"); return; }
        // 에디터가 Play가 아닐 때 만들면 오브젝트가 씬에 저장되므로 Play 중에만 시작하고, 저장되지 않게 표시한다.
        if (!Application.isPlaying) { Debug.LogWarning("[perf] Play 모드에서만 측정할 수 있습니다"); return; }
        GameObject go = new GameObject("PerfProbe") { hideFlags = HideFlags.DontSave };
        PerfProbe probe = go.AddComponent<PerfProbe>();
        probe.scenario = scenario;
        probe.seconds = Mathf.Max(1f, seconds);
        probe.warmup = Mathf.Max(0f, warmup);
        probe.count = count;
        probe.quitWhenDone = quitWhenDone;
        running = probe;
        DontDestroyOnLoad(probe.gameObject);
        probe.StartCoroutine(probe.Run());
    }

    private IEnumerator Run()
    {
        // 플레이어가 생길 때까지 기다린다(멀티플레이 참가자는 접속 후 생긴다).
        GameObject player = null;
        while (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            yield return null;
        }

        // 에디터가 백그라운드이거나 씬 로딩 직후이면 프레임이 거의 진행되지 않아 수치가 무효다. 1초 동안 프레임이 충분히 진행되는지 보고,
        // 안 되면 최대 GuardRetries번까지 다시 본다(로딩이 끝나기를 기다린다). 끝내 안 되면 측정하지 않는다.
        int advanced = 0;
        for (int attempt = 0; attempt < GuardRetries; attempt++)
        {
            int frameStart = Time.frameCount;
            yield return new WaitForSecondsRealtime(FrameGuardSeconds);
            advanced = Time.frameCount - frameStart;
            if (advanced >= FrameGuardMinFrames) break;
        }
        if (advanced < FrameGuardMinFrames)
        {
            Debug.LogWarning($"[perf] 무효: {FrameGuardSeconds}초 동안 {advanced}프레임만 진행됨({GuardRetries}번 확인, 에디터가 백그라운드인지 확인, editor_focus). 측정하지 않습니다");
            Finish();
            yield break;
        }

        // vsync가 켜져 있으면 모니터 주사율에서 프레임이 막혀 여유를 볼 수 없다. 플레이어에서는 측정 동안만 끈다(에디터는 설정을 건드리지 않는다).
#if !UNITY_EDITOR
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;
#endif
        origin = player.transform.position;
        lastGroundY = origin.y;
        Setup(player);
        Debug.Log($"[perf] 시작: scenario={scenario} role={Role()} seconds={seconds} warmup={warmup} count={count} 시작 후 1초 프레임={advanced}");

        Health health = player.GetComponent<Health>();
        float end = Time.realtimeSinceStartup + warmup;
        float walked = 0f;
        while (Time.realtimeSinceStartup < end)
        {
            walked = StepWorld(player, walked);
            KeepAlive(health);
            yield return null;
        }

        yield return Measure(player, health, walked);
        // world 시나리오는 끝나면 출발점으로 돌려 놓아 다음 측정에 영향을 주지 않는다.
        if (scenario == "world" && player != null) Teleport(player, origin);
        Finish();
    }

    private IEnumerator Measure(GameObject player, Health health, float walked)
    {
        List<float> frames = new List<float>();
        Dictionary<string, Sampler> samplers = CreateSamplers();
        int zombies = FindObjectsByType<ZombieAI>(FindObjectsSortMode.None).Length;

        List<string> spikes = new List<string>();
        float measureStart = Time.realtimeSinceStartup;
        int gcBefore = System.GC.CollectionCount(0);
        float end = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < end)
        {
            yield return null;
            walked = StepWorld(player, walked);
            KeepAlive(health);
            float ms = Time.unscaledDeltaTime * 1000f;
            frames.Add(ms);
            foreach (Sampler s in samplers.Values) s.Take();
            RecordSpike(spikes, samplers, ms, Time.realtimeSinceStartup - measureStart, player, gcBefore);
            gcBefore = System.GC.CollectionCount(0);
        }
        LogSpikes(spikes, frames);

        float avg = FrameTimeStats.Average(frames);
        float worst = FrameTimeStats.WorstOnePercentAverage(frames);
        StringBuilder log = new StringBuilder();
        log.Append($"[perf] 결과: scenario={scenario} role={Role()} frames={frames.Count} zombies={zombies}");
        log.Append($" frameMs avg={avg:F2} ({FrameTimeStats.ToFps(avg):F1}fps) worst1%={worst:F2} ({FrameTimeStats.ToFps(worst):F1}fps)");
        foreach (KeyValuePair<string, Sampler> pair in samplers)
            log.Append($" | {pair.Key}={pair.Value.Describe()}");
        Debug.Log(log.ToString());
        foreach (Sampler s in samplers.Values) s.Dispose();
    }

    private void Finish()
    {
        running = null;
        if (quitWhenDone) Application.Quit();
        Destroy(gameObject);
    }

    // 좀비에게 맞아 플레이어가 죽으면 AI와 화면 상태가 달라져 측정이 왜곡되므로 측정 동안 체력을 채워 둔다.
    private static void KeepAlive(Health health)
    {
        if (health == null) return;
        if (health.IsDead) health.Revive();
        else if (health.CurrentHealth < health.MaxHealth) health.Heal(health.MaxHealth);
    }

    // 지형이 여러 타일이거나 도로·건물이 있어도 맞게, 위에서 아래로 쏜 레이로 플레이어 자신을 뺀 가장 높은 표면을 찾는다.
    private static bool TryFindGround(Vector3 flat, Transform player, out float groundY)
    {
        groundY = 0f;
        bool found = false;
        foreach (RaycastHit hit in Physics.RaycastAll(new Vector3(flat.x, flat.y + 300f, flat.z), Vector3.down, 1000f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.IsChildOf(player)) continue;
            if (!found || hit.point.y > groundY) groundY = hit.point.y;
            found = true;
        }
        return found;
    }

    private static void Teleport(GameObject player, Vector3 position)
    {
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        player.transform.position = position;
        if (controller != null) controller.enabled = true;
    }

    // 끊긴 프레임(SpikeMs 이상)의 시각, 길이, 위치와 구간별 시간을 모은다. 기록은 SpikeLimit개까지만 남긴다.
    private const float SpikeMs = 20f;
    private const int SpikeLimit = 80;

    private static void RecordSpike(List<string> spikes, Dictionary<string, Sampler> samplers, float ms, float t, GameObject player, int gcBefore)
    {
        if (ms < SpikeMs || spikes.Count >= SpikeLimit) return;
        Vector3 p = player != null ? player.transform.position : Vector3.zero;
        StringBuilder sb = new StringBuilder();
        sb.Append($"t={t:F1}s {ms:F1}ms pos=({p.x:F0},{p.z:F0}) gcCollect={System.GC.CollectionCount(0) - gcBefore}");
        foreach (string key in SpikeKeys)
            if (samplers.TryGetValue(key, out Sampler s)) sb.Append($" {key}={s.Last:F1}");
        spikes.Add(sb.ToString());
    }

    private static readonly string[] SpikeKeys = { "updateMs", "lateUpdateMs", "physicsMs", "animMs", "urpMainCamMs", "terrainTreesMs", "updateRenderersMs", "skinMeshMs", "uiToolkitMs", "gcKB", "drawCalls" };

    // 프레임 시간 분포(몇 프레임이 기준을 넘었는지)와 끊긴 프레임 목록을 로그로 남긴다.
    private static void LogSpikes(List<string> spikes, List<float> frames)
    {
        int over17 = 0, over20 = 0, over33 = 0, over50 = 0;
        foreach (float f in frames)
        {
            if (f > 16.7f) over17++;
            if (f > 20f) over20++;
            if (f > 33.3f) over33++;
            if (f > 50f) over50++;
        }
        Debug.Log($"[perf] 분포: frames={frames.Count} >16.7ms={over17} >20ms={over20} >33.3ms={over33} >50ms={over50}");
        foreach (string line in spikes) Debug.Log("[perf] 끊김 " + line);
    }

    private static string Role() => MultiplayerSession.IsHost ? "host" : MultiplayerSession.IsClientOnly ? "client" : "single";

    private void Setup(GameObject player)
    {
        switch (scenario)
        {
            case "horde":
                if (GameClock.Current != null) GameClock.Current.SetHour(0f); // 낮에는 좀비가 불타 죽는다.
                ZombieSpawner spawner = FindFirstObjectByType<ZombieSpawner>();
                int want = count > 0 ? count : 40;
                int made = spawner != null ? spawner.DebugSpawnAround(player.transform.position, want, 8f, 35f) : 0;
                Debug.Log($"[perf] 좀비 {made}/{want}마리 생성");
                break;
            case "base":
                BuildingSystem building = FindFirstObjectByType<BuildingSystem>();
                int placed = building != null ? PlaceBuildables(building, player.transform.position, count > 0 ? count : 300) : 0;
                Debug.Log($"[perf] 건축물 {placed}개 배치");
                break;
        }
    }

    // 건축 규칙(재료, 받침)을 거치지 않고 정의의 프리팹을 격자로 놓는다. 렌더/물리 비용만 보려는 용도다.
    private static int PlaceBuildables(BuildingSystem building, Vector3 center, int total)
    {
        BuildableDefinition[] defs = building.Buildables;
        if (defs == null || defs.Length == 0) return 0;
        Transform parent = new GameObject("PerfBase").transform;
        int side = Mathf.CeilToInt(Mathf.Sqrt(total));
        int placed = 0;
        for (int i = 0; i < total; i++)
        {
            BuildableDefinition def = defs[i % defs.Length];
            if (def == null || def.Prefab == null) continue;
            Vector3 pos = center + new Vector3((i % side - side / 2) * 3f, 0f, (i / side + 4) * 3f);
            Instantiate(def.Prefab, GroundSnap.OnGround(pos), Quaternion.identity, parent);
            placed++;
        }
        return placed;
    }

    // world 시나리오에서만 플레이어를 출발점 둘레 정사각형 경로로 달리는 속도로 옮겨 지형/풀/나무 스트리밍을 일으킨다. 이동 거리를 돌려준다.
    private float StepWorld(GameObject player, float walked)
    {
        if (scenario != "world") return walked;
        walked += WorldSpeed * Time.deltaTime;
        float perimeter = WorldHalfSize * 8f;
        float t = walked % perimeter / (WorldHalfSize * 2f); // 0~4: 변 번호와 변 위 비율
        int edge = Mathf.FloorToInt(t);
        float f = t - edge;
        Vector2[] corners = { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(1, 1), new Vector2(-1, 1) };
        Vector2 a = corners[edge % 4], b = corners[(edge + 1) % 4];
        Vector2 p = Vector2.Lerp(a, b, f) * WorldHalfSize;
        Vector3 flat = origin + new Vector3(p.x, 0f, p.y);
        Vector3 target = new Vector3(flat.x, lastGroundY, flat.z);
        if (TryFindGround(flat, player.transform, out float groundY)) lastGroundY = groundY;
        target.y = lastGroundY + 0.3f;
        Teleport(player, target);
        return walked;
    }

    private static Dictionary<string, Sampler> CreateSamplers()
    {
        return new Dictionary<string, Sampler>
        {
            { "mainMs", new Sampler(ProfilerCategory.Internal, "Main Thread", 1e-6f) },
            { "updateMs", new Sampler(ProfilerCategory.Scripts, "Update.ScriptRunBehaviourUpdate", 1e-6f) },
            { "lateUpdateMs", new Sampler(ProfilerCategory.Scripts, "PreLateUpdate.ScriptRunBehaviourLateUpdate", 1e-6f) },
            { "physicsMs", new Sampler(ProfilerCategory.Physics, "FixedUpdate.PhysicsFixedUpdate", 1e-6f) },
            { "animMs", new Sampler(ProfilerCategory.Animation, "Animators.Update", 1e-6f) },
            // 메인 스레드 안에서 어디에 시간이 쓰이는지 가르는 항목들이다(카테고리/이름은 에디터의 마커 목록에서 확인한 값).
            { "urpMainCamMs", new Sampler(ProfilerCategory.Scripts, "UniversalRenderPipeline.RenderSingleCameraInternal: Main Camera", 1e-6f) },
            { "cullMs", new Sampler(ProfilerCategory.Render, "Culling", 1e-6f) },
            { "drawSrpMs", new Sampler(ProfilerCategory.Render, "RenderLoop.DrawSRPBatcher", 1e-6f) },
            { "mainShadowMs", new Sampler(ProfilerCategory.Scripts, "MainLightShadowCasterPass", 1e-6f) },
            { "addShadowMs", new Sampler(ProfilerCategory.Scripts, "AdditionalLightsShadowCasterPass", 1e-6f) },
            { "terrainTreesMs", new Sampler(ProfilerCategory.Render, "Terrain.Trees.Render", 1e-6f) },
            { "updateRenderersMs", new Sampler(new ProfilerCategory("PlayerLoop"), "PostLateUpdate.UpdateAllRenderers", 1e-6f) },
            { "skinMeshMs", new Sampler(new ProfilerCategory("PlayerLoop"), "PostLateUpdate.UpdateAllSkinnedMeshes", 1e-6f) },
            { "uiToolkitMs", new Sampler(new ProfilerCategory("PlayerLoop"), "PostLateUpdate.UIElementsRepaintPanels", 1e-6f) },
            { "vsyncWaitMs", new Sampler(new ProfilerCategory("VSync"), "TimeUpdate.WaitForLastPresentationAndUpdateTime", 1e-6f) },
            { "waitPresentMs", new Sampler(ProfilerCategory.Render, "Gfx.WaitForPresentOnGfxThread", 1e-6f) },
            { "playerLoopMs", new Sampler(new ProfilerCategory("PlayerLoop"), "PlayerLoop", 1e-6f) },
            { "gcKB", new Sampler(ProfilerCategory.Memory, "GC Allocated In Frame", 1f / 1024f) },
            { "drawCalls", new Sampler(ProfilerCategory.Render, "Draw Calls Count", 1f) },
            { "batches", new Sampler(ProfilerCategory.Render, "Batches Count", 1f) },
        };
    }

    // 프로파일러 항목 하나를 프레임마다 읽어 평균과 최댓값을 모은다. 읽을 수 없는 항목은 n/a로 표시한다.
    private class Sampler : IDisposable
    {
        private readonly ProfilerRecorder recorder;
        private readonly float scale;
        private double sum;
        private float max;
        private int n;

        public Sampler(ProfilerCategory category, string name, float scale)
        {
            this.scale = scale;
            recorder = ProfilerRecorder.StartNew(category, name, 1);
        }

        // 가장 최근에 읽은 프레임의 값이다(끊긴 프레임을 기록할 때 쓴다).
        public float Last { get; private set; }

        public void Take()
        {
            if (!recorder.Valid) return;
            float v = recorder.LastValue * scale;
            Last = v;
            sum += v;
            if (v > max) max = v;
            n++;
        }

        public string Describe() => n == 0 ? "n/a" : $"{sum / n:F2}(max {max:F2})";

        public void Dispose() => recorder.Dispose();
    }
}
#endif
