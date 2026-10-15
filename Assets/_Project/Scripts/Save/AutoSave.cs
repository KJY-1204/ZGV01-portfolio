// 게임 씬에서 약 10분마다 현재 저장 슬롯에 자동 저장하는 컴포넌트다. 주기와 조건은 AutoSaveRule이 정하고 저장은 수동 저장과 같은 코드를 쓴다. 게임 씬이 열릴 때 자동으로 만들어진다.
using UnityEngine;
using UnityEngine.SceneManagement;

public class AutoSave : MonoBehaviour
{
    // 저장할 수 있는 상태인지 이 간격(초)마다 확인한다(매 프레임 플레이어를 찾지 않는다).
    private const float CheckInterval = 2f;

    private float interval = AutoSaveRule.IntervalSeconds;
    private float lastSaved;
    private float nextCheck;
    private GameObject player;

    // 시험에서 주기를 줄일 수 있다.
    public float Interval
    {
        get => interval;
        set => interval = Mathf.Max(1f, value);
    }

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
        if (FindFirstObjectByType<AutoSave>() != null) return;
        new GameObject("AutoSave").AddComponent<AutoSave>();
    }

    private void Awake()
    {
        lastSaved = Time.realtimeSinceStartup;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 테스트용: -mp-autosave-seconds=N이면 주기를 N초로 줄인다.
        foreach (string arg in System.Environment.GetCommandLineArgs())
            if (arg.StartsWith("-mp-autosave-seconds=") && float.TryParse(arg.Substring("-mp-autosave-seconds=".Length), out float seconds)) Interval = seconds;
#endif
    }

    private void Update()
    {
        float now = Time.realtimeSinceStartup;
        if (!AutoSaveRule.IsDue(now, lastSaved, interval) || now < nextCheck) return;
        nextCheck = now + CheckInterval;

        if (player == null) player = GameObject.FindGameObjectWithTag("Player");
        Health health = player != null ? player.GetComponent<Health>() : null;
        // 조건이 안 맞으면 타이머를 새로 시작하지 않고 맞을 때까지 기다렸다 저장한다(죽어 있으면 되살아난 뒤).
        if (!AutoSaveRule.CanSave(GameSession.InMenuScene, MultiplayerSession.IsClientOnly, player != null, health != null && health.IsDead)) return;

        lastSaved = now;
        SaveLoadDebugController.SaveNow(player, true);
    }
}
