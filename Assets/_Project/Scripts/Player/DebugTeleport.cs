// 개발 중(에디터/개발 빌드)에만 동작하는 순간이동 도구다. F1로 창을 열고(F2~F7은 구역, F8은 다음 연못, F9는 광산 입구, F10은 광산 안, PageUp/PageDown은 이전/다음 가로등) 넓은 맵을 즉시 이동하며 확인한다. 씬에 두지 않고 시작할 때 스스로 만든다.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class DebugTeleport : MonoBehaviour
{
    private struct Destination
    {
        public string label;
        public Vector3 position;
    }

    private readonly List<Destination> zones = new List<Destination>();
    private readonly List<NightLight> lamps = new List<NightLight>();
    private bool open;
    private CursorLockMode savedLockState;
    private bool savedCursorVisible;
    private readonly List<Transform> ponds = new List<Transform>();
    private int lampIndex = -1;
    private int pondIndex = -1;
    private Vector2 scroll;
    private string message = "";

    // 첫 씬이 메인 메뉴여도 게임 씬이 열릴 때마다 만들어지게 씬 로드마다 다시 부른다(메뉴 씬에서는 만들지 않는다).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Hook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Create();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Create();

    private static void Create()
    {
        if (GameSession.InMenuScene) return;
        if (FindFirstObjectByType<DebugTeleport>() != null) return;
        new GameObject("DebugTeleport").AddComponent<DebugTeleport>();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.f1Key.wasPressedThisFrame) SetOpen(!open);

        // F2~F7로 구역으로 바로 이동한다(창이 닫혀 있어도 된다).
        if (zones.Count == 0) CollectDestinations();
        for (int i = 0; i < zones.Count && i < 6; i++)
            if (keyboard[(Key)((int)Key.F2 + i)].wasPressedThisFrame) Teleport(zones[i].position, zones[i].label);
        if (keyboard.f8Key.wasPressedThisFrame) GoToPond();
        if (keyboard.f9Key.wasPressedThisFrame) GoToMine(false);
        if (keyboard.f10Key.wasPressedThisFrame) GoToMine(true);
        // 창이 닫혀 있어도 PageUp/PageDown으로 이전/다음 가로등을 오갈 수 있다.
        if (keyboard.pageDownKey.wasPressedThisFrame) GoToLamp(1);
        if (keyboard.pageUpKey.wasPressedThisFrame) GoToLamp(-1);
    }

    // 창이 열려 있는 동안은 마우스 커서를 풀어 버튼을 누를 수 있게 하고, 닫으면 원래대로 돌려 놓는다.
    private void SetOpen(bool value)
    {
        if (open == value) return;
        open = value;
        if (open)
        {
            savedLockState = Cursor.lockState;
            savedCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = savedLockState;
            Cursor.visible = savedCursorVisible;
        }
    }

    // 구역 이름은 맵 소품 묶음(MapProps) 아래 자식 이름이고, 위치는 그 묶음 안 소품 렌더러 전체의 중심이다. 처음 열 때와 맵이 바뀌었을 때 다시 모은다.
    private void CollectDestinations()
    {
        zones.Clear();
        lamps.Clear();
        ponds.Clear();
        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            if (t.name.Contains("Pond") && t.parent != null && t.parent.name != "Mine") ponds.Add(t);
        ponds.Sort((a, b) => a.position.x != b.position.x ? a.position.x.CompareTo(b.position.x) : a.position.z.CompareTo(b.position.z));

        GameObject props = GameObject.Find("MapProps");
        if (props == null) return;

        foreach (Transform group in props.transform)
        {
            if (group.childCount == 0 || group.name == "Field" || group.name == "Roadside" || group.name == "RoadLamps" || group.name == "Mine") continue;
            // 묶음 아래 소품 렌더러 전체의 중심을 구역 중심으로 본다(하위 묶음의 피벗은 원점이라 위치로 평균내면 쏠린다).
            Bounds bounds = new Bounds();
            bool any = false;
            foreach (Renderer renderer in group.GetComponentsInChildren<Renderer>())
            {
                if (!any) { bounds = renderer.bounds; any = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            if (any) zones.Add(new Destination { label = group.name, position = bounds.center });
        }

        // 하이어라키 순서(구역 가로등, 도로 가로등 순)로 모아 이전/다음 순서가 일정하다.
        // 실내 조명(항상 켜짐)은 가로등 순회에서 뺀다.
        foreach (NightLight lamp in props.GetComponentsInChildren<NightLight>(true))
            if (!lamp.AlwaysOn) lamps.Add(lamp);
    }

    private void OnGUI()
    {
        if (!open) return;
        if (zones.Count == 0 && lamps.Count == 0) CollectDestinations();

        // 왼쪽 HUD와 위쪽 퀘스트 패널을 피해 오른쪽 가운데에 둔다.
        GUILayout.BeginArea(new Rect(Screen.width - 312f, 340f, 300f, Mathf.Max(200f, Screen.height - 440f)), GUI.skin.box);
        GUILayout.Label("디버그 순간이동 (F1 닫기, F2~F7 구역, F8 연못, F9/F10 광산)");
        scroll = GUILayout.BeginScrollView(scroll);

        foreach (Destination zone in zones)
            if (GUILayout.Button(zone.label)) Teleport(zone.position, zone.label);

        GUILayout.Space(8f);
        if (GUILayout.Button($"연못 다음 ({(pondIndex < 0 ? "-" : (pondIndex + 1).ToString())}/{ponds.Count}, F8)")) GoToPond();
        if (GUILayout.Button("광산 입구 (F9)")) GoToMine(false);
        if (GUILayout.Button("광산 안 큰 홀 (F10)")) GoToMine(true);

        GUILayout.Space(8f);
        GUILayout.Label($"가로등 {(lampIndex < 0 ? "-" : (lampIndex + 1).ToString())} / {lamps.Count}  (PageUp / PageDown)");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("< 이전")) GoToLamp(-1);
        if (GUILayout.Button("다음 >")) GoToLamp(1);
        GUILayout.EndHorizontal();
        if (GUILayout.Button("맵 다시 읽기")) CollectDestinations();

        if (!string.IsNullOrEmpty(message)) GUILayout.Label(message);
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void GoToPond()
    {
        if (ponds.Count == 0) CollectDestinations();
        if (ponds.Count == 0) { message = "연못을 찾지 못했다"; return; }
        pondIndex = (pondIndex + 1) % ponds.Count;
        Transform pond = ponds[pondIndex];
        Vector3 offset = Quaternion.Euler(0f, 35f, 0f) * Vector3.back * 6f;
        Teleport(pond.position + offset, $"연못 {pondIndex + 1}/{ponds.Count}", pond.position);
    }

    // 광산 입구 광장(MinePortal 20, 452 앞)과 큰 홀이다. 홀은 산 지면 아래라 지형 높이로 맞추지 않고 광장 바닥 높이를 쓴다.
    private void GoToMine(bool inside)
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null) return;
        float floorY = terrain.SampleHeight(new Vector3(20f, 0f, 440f)) + terrain.transform.position.y;
        if (!inside) { Teleport(new Vector3(20f, 0f, 432f), "광산 입구", new Vector3(20f, 0f, 452f)); return; }
        Teleport(new Vector3(20f, floorY, 468f), "광산 안", new Vector3(20f, floorY, 490f), false);
    }

    private void GoToLamp(int step)
    {
        if (lamps.Count == 0) CollectDestinations();
        if (lamps.Count == 0) return;

        lampIndex = (lampIndex + step + lamps.Count) % lamps.Count;
        NightLight lamp = lamps[lampIndex];
        if (lamp == null) return;

        // 가로등 옆(발치에서 4m)에 서서 가로등을 바라본다.
        Vector3 offset = Quaternion.Euler(0f, 35f, 0f) * Vector3.back * 4f;
        Teleport(lamp.transform.position + offset, $"가로등 {lampIndex + 1}/{lamps.Count} ({FaultLabel(lamp)})", lamp.transform.position);
    }

    private static string FaultLabel(NightLight lamp)
    {
        switch (lamp.Fault)
        {
            case LampFault.Dead: return "꺼짐";
            case LampFault.Flicker: return "깜빡임";
            default: return "정상";
        }
    }

    private void Teleport(Vector3 position, string label, Vector3? lookAt = null, bool snapToTerrain = true)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        player.transform.position = (snapToTerrain ? GroundSnap.OnGround(position) : position) + Vector3.up * 0.3f;
        if (lookAt.HasValue)
        {
            Vector3 toTarget = lookAt.Value - player.transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.01f) player.transform.rotation = Quaternion.LookRotation(toTarget);
        }
        if (controller != null) controller.enabled = true;
        message = "이동: " + label;
    }
}
#endif
