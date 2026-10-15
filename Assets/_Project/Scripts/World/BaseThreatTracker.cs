// 거점(설치된 건축물) 유무와 게임 시작부터 흐른 시간을 지켜보다가 좀비가 정찰할지 밤새 플레이어를 쫓을지 알려 주는 컴포넌트다. 씬에 두지 않고 시작할 때 스스로 만든다.
using UnityEngine;
using UnityEngine.SceneManagement;

public class BaseThreat : MonoBehaviour
{
    private static BaseThreat current;

    // 이 값들은 시계가 없는 씬(시험 씬)에서는 false라 좀비가 예전처럼 움직인다.
    public static bool Patrolling => current != null && current.patrolling;
    public static bool Hunting => current != null && current.hunting;
    public static bool HasBase => current != null && current.hasBase;
    // 게임 시작부터 흐른 게임 시간(시)이다. 저장하고 불러올 때 쓴다.
    public static float ElapsedGameHours => current != null ? current.elapsedGameHours : 0f;

    private bool hasBase;
    private bool patrolling;
    private bool hunting;
    private float elapsedGameHours;

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
        if (FindFirstObjectByType<BaseThreat>() != null) return;
        new GameObject("BaseThreat").AddComponent<BaseThreat>();
    }

    private void Awake() => current = this;

    private void OnDestroy()
    {
        if (current == this) current = null;
    }

    // 저장에서 불러온 값으로 되돌린다(음수면 필드가 없는 옛 세이브라 그대로 둔다).
    public static void Restore(float savedElapsedGameHours)
    {
        if (current == null || savedElapsedGameHours < 0f) return;
        current.elapsedGameHours = savedElapsedGameHours;
    }

    private void Update()
    {
        GameClock clock = GameClock.Current;
        if (clock == null || clock.DayLengthSeconds <= 0f)
        {
            patrolling = false;
            hunting = false;
            return;
        }

        hasBase = BuildableInstance.AnyBasePiece;
        float deltaGameHours = Time.deltaTime * 24f / clock.DayLengthSeconds;
        elapsedGameHours = ZombieThreatRule.AdvanceElapsedHours(elapsedGameHours, deltaGameHours);
        patrolling = ZombieThreatRule.Patrols(hasBase, elapsedGameHours);
        hunting = ZombieThreatRule.Hunts(clock.IsNight, hasBase, elapsedGameHours);
    }
}

