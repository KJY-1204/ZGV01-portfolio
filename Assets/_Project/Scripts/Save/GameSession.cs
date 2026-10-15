// 메인 메뉴와 게임 씬 사이의 이동(새 게임, 이어하기, 멀티 호스트/참가, 메인 메뉴로 돌아가기)을 맡는 정적 도우미다. 이어하기는 게임 씬이 열린 뒤 저장 파일을 불러온다.
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameSession
{
    public const string MenuScene = "Main_Menu";
    public const string GameScene = "Main_Map";

    // 지금 씬이 메인 메뉴인지다(게임 전용 컴포넌트를 만들지 않는 데 쓴다).
    public static bool InMenuScene => SceneManager.GetActiveScene().name == MenuScene;

    private static bool continuePending;

    // 고른 슬롯에서 새로 시작한다. 그 슬롯에 있던 저장은 지운다(덮어쓰기는 호출하는 쪽이 확인한다).
    public static void StartNewGame(int slot)
    {
        GameSaveSystem.CurrentSlot = slot;
        GameSaveSystem.LoadFromAuto = false;
        GameSaveSystem.DeleteSlot(slot);
        ParticipantStore.Clear();
        continuePending = false;
        EnterGameScene();
    }

    // 고른 슬롯의 저장을 이어한다. 저장 파일이 없으면 아무것도 하지 않고 거짓을 돌려준다.
    public static bool StartContinue(int slot, bool fromAuto = false)
    {
        GameSaveSystem.CurrentSlot = slot;
        GameSaveSystem.LoadFromAuto = fromAuto;
        if (!GameSaveSystem.HasSave) return false;
        continuePending = true;
        EnterGameScene();
        return true;
    }

    // 멀티플레이 호스트로 시작한다. 고른 슬롯에 저장이 있으면 이어서, 없으면 새로 시작한다.
    public static bool StartHost(int slot, ushort port, bool fromAuto = false)
    {
        GameSaveSystem.CurrentSlot = slot;
        GameSaveSystem.LoadFromAuto = fromAuto;
        continuePending = GameSaveSystem.HasSave;
        // 저장이 없으면 새로 시작이라 이전 세션의 참가자 기록을 비운다(저장이 있으면 불러올 때 바뀐다).
        if (!continuePending) ParticipantStore.Clear();
        if (!MultiplayerSession.StartHost(port)) return false;

        Time.timeScale = 1f;
        AudioListener.pause = false;
        HookGameSceneLoaded();
        MultiplayerSession.LoadGameScene();
        return true;
    }

    // 호스트의 게임에 참가한다. 월드는 접속하면 호스트가 정한 씬으로 따라 들어간다(저장은 호스트만 쓴다).
    public static bool JoinGame(string address, ushort port)
    {
        continuePending = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        return MultiplayerSession.StartClient(address, port);
    }

    public static void ReturnToMenu()
    {
        // 멀티 접속을 끊고, 일시정지로 멈춘 시간과 소리를 풀고 나간다.
        MultiplayerSession.Shutdown();
        Time.timeScale = 1f;
        AudioListener.pause = false;
        continuePending = false;
        SceneManager.LoadScene(MenuScene);
    }

    private static void EnterGameScene()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        HookGameSceneLoaded();
        SceneManager.LoadScene(GameScene);
    }

    private static void HookGameSceneLoaded()
    {
        SceneManager.sceneLoaded -= OnGameSceneLoaded;
        SceneManager.sceneLoaded += OnGameSceneLoaded;
    }

    private static void OnGameSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != GameScene) return;
        SceneManager.sceneLoaded -= OnGameSceneLoaded;
        if (!continuePending) return;

        continuePending = false;
        new GameObject("ContinueLoader").AddComponent<ContinueLoader>();
    }

    // 모든 컴포넌트가 시작을 마친 뒤(몇 프레임 뒤) 플레이어에게 저장 파일을 적용하고 사라진다.
    private class ContinueLoader : MonoBehaviour
    {
        private IEnumerator Start()
        {
            for (int i = 0; i < 3; i++) yield return null;
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) SaveLoadDebugController.LoadNow(player, true);
            else Debug.LogWarning("이어하기: 플레이어를 찾지 못했다.");
            Destroy(gameObject);
        }
    }
}
