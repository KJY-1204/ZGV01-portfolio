// 메인 메뉴 화면의 동작(이어하기, 새 게임, 옵션, 종료)과 저장 슬롯 고르기를 맡는 UI 클래스다. 구조는 MainMenu.uxml, 모양은 MainMenu.uss와 Hud.uss에 있고 옵션 창은 일시정지 메뉴와 같은 PauseMenu를 쓴다.
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class MainMenuController : MonoBehaviour
{
    // 슬롯 고르기 화면이 무엇을 위해 열렸는지다.
    private enum SlotMode { Continue, NewGame, Host }

    private Button continueButton;
    private Label note;
    private PauseMenu options;

    private VisualElement slotPanel;
    private Label slotTitle, slotNote;
    private readonly Button[] slotButtons = new Button[GameSaveSystem.SlotCount];
    private SlotMode slotMode;
    private Button slotSourceButton;
    // 이어하기/호스트에서 수동 저장 대신 자동 저장 파일을 불러올지다.
    private bool slotUseAuto;
    // 이미 저장이 있는 슬롯에 새 게임을 시작하려면 같은 슬롯을 두 번 눌러야 한다(0이면 대기 중인 슬롯 없음).
    private int overwriteArmedSlot;

    private VisualElement multiPanel;
    private TextField multiIp, multiPort;
    private Label multiStatus;

    private void OnEnable()
    {
        // 게임에서 돌아왔을 때 멈춘 시간, 잠긴 커서가 남지 않게 한다.
        Time.timeScale = 1f;
        AudioListener.pause = false;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;

        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        continueButton = root.Q<Button>("menu-continue");
        note = root.Q<Label>("menu-note");
        root.Q<Label>("menu-version").text = "v" + Application.version;

        continueButton.clicked += () => OpenSlots(SlotMode.Continue);
        root.Q<Button>("menu-new").clicked += () => OpenSlots(SlotMode.NewGame);
        root.Q<Button>("menu-multi").clicked += OpenMulti;
        root.Q<Button>("menu-options").clicked += () => options.OpenOptions();
        root.Q<Button>("menu-quit").clicked += Quit;

        slotPanel = root.Q<VisualElement>("slot-panel");
        slotTitle = root.Q<Label>("slot-title");
        slotNote = root.Q<Label>("slot-note");
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int slot = i + 1;
            slotButtons[i] = root.Q<Button>("slot-" + slot);
            slotButtons[i].clicked += () => PickSlot(slot);
        }
        slotSourceButton = root.Q<Button>("slot-source");
        slotSourceButton.clicked += () => { slotUseAuto = !slotUseAuto; RefreshSlots(); };
        root.Q<Button>("slot-back").clicked += CloseSlots;
        slotPanel.style.display = DisplayStyle.None;

        SetupMulti(root);
        // 멀티 접속이 끊겨 돌아왔다면 이유를 보여 준다.
        if (!MultiplayerSession.Active && MultiplayerSession.Status.Length > 0) note.text = MultiplayerSession.Status;
        options = new PauseMenu(root, () => null, true);

        bool hasSave = GameSaveSystem.HasAnySave;
        continueButton.SetEnabled(hasSave);
        continueButton.text = hasSave ? "이어하기" : "이어하기 (저장 없음)";
    }

    private void OnDisable()
    {
        MultiplayerSession.StatusChanged -= OnMultiStatus;
        options?.Dispose();
        options = null;
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
        if (options != null && options.IsOpen) options.HandleEscape();
        else if (slotPanel != null && slotPanel.style.display == DisplayStyle.Flex) CloseSlots();
        else if (multiPanel != null && multiPanel.style.display == DisplayStyle.Flex) CloseMulti();
    }

    // ---- 멀티플레이 ----

    private void SetupMulti(VisualElement root)
    {
        multiPanel = root.Q<VisualElement>("multi-panel");
        multiIp = root.Q<TextField>("multi-ip");
        multiPort = root.Q<TextField>("multi-port");
        multiStatus = root.Q<Label>("multi-status");
        multiPort.value = MultiplayerSession.DefaultPort.ToString();
        multiPanel.style.display = DisplayStyle.None;

        root.Q<Button>("multi-host").clicked += () => { CloseMulti(); OpenSlots(SlotMode.Host); };
        root.Q<Button>("multi-join").clicked += Join;
        root.Q<Button>("multi-back").clicked += CloseMulti;

        MultiplayerSession.StatusChanged -= OnMultiStatus;
        MultiplayerSession.StatusChanged += OnMultiStatus;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 테스트용 명령줄(-mp-host, -mp-join=주소:포트)이 있으면 한 프레임 뒤 자동으로 시작한다.
        string command = MultiplayerSession.AutoCommand;
        if (!string.IsNullOrEmpty(command)) StartCoroutine(RunAutoCommand(command));
#endif
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private System.Collections.IEnumerator RunAutoCommand(string command)
    {
        yield return null;
        yield return null;
        if (command == "-mp-host")
        {
            GameSession.StartHost(GameSaveSystem.CurrentSlot, MultiplayerSession.DefaultPort);
            yield break;
        }

        string[] parts = command.Substring("-mp-join=".Length).Split(':');
        ushort port = parts.Length > 1 && ushort.TryParse(parts[1], out ushort parsed) ? parsed : MultiplayerSession.DefaultPort;
        GameSession.JoinGame(parts[0], port);
    }
#endif

    private void OpenMulti()
    {
        note.text = "";
        string address = MultiplayerSession.LocalAddress();
        root_AddressLabel().text = address.Length > 0 ? $"내 주소(같은 네트워크): {address}:{MultiplayerSession.DefaultPort}" : "";
        multiStatus.text = MultiplayerSession.Status;
        multiPanel.style.display = DisplayStyle.Flex;
    }

    private Label root_AddressLabel() => multiPanel.Q<Label>("multi-address");

    private void CloseMulti() => multiPanel.style.display = DisplayStyle.None;

    private void Join()
    {
        if (!ushort.TryParse(multiPort.value, out ushort port)) { multiStatus.text = "포트는 숫자로 입력하세요"; return; }
        GameSession.JoinGame(multiIp.value, port);
    }

    private void OnMultiStatus(string text)
    {
        if (multiStatus != null) multiStatus.text = text;
    }

    // ---- 저장 슬롯 ----

    private void OpenSlots(SlotMode mode)
    {
        slotMode = mode;
        slotUseAuto = false;
        overwriteArmedSlot = 0;
        note.text = "";
        slotTitle.text = mode == SlotMode.Continue ? "이어할 슬롯" : mode == SlotMode.Host ? "호스트할 슬롯" : "새 게임을 시작할 슬롯";
        slotNote.text = mode == SlotMode.NewGame ? "저장이 있는 슬롯은 한 번 더 누르면 덮어씁니다"
            : mode == SlotMode.Host ? "저장이 있으면 이어서, 비어 있으면 새로 시작합니다. 세션 동안 이 슬롯에 저장되며 바꾸려면 다시 시작해야 합니다" : "";
        RefreshSlots();
        slotPanel.style.display = DisplayStyle.Flex;
    }

    private void CloseSlots()
    {
        overwriteArmedSlot = 0;
        slotPanel.style.display = DisplayStyle.None;
    }

    private void RefreshSlots()
    {
        // 새 게임에는 저장 종류가 없다. 이어하기/호스트에서 자동 저장이 하나라도 있으면 수동/자동 저장을 전환할 수 있다.
        bool pickable = slotMode != SlotMode.NewGame;
        bool anyAuto = GameSaveSystem.AnyAutoSave;
        if (!anyAuto) slotUseAuto = false;
        slotSourceButton.style.display = pickable ? DisplayStyle.Flex : DisplayStyle.None;
        slotSourceButton.SetEnabled(anyAuto);
        slotSourceButton.text = !anyAuto ? "자동 저장 없음" : slotUseAuto ? "불러올 저장: 자동 저장 (누르면 수동 저장)" : "불러올 저장: 수동 저장 (누르면 자동 저장)";
        bool useAuto = pickable && slotUseAuto;
        for (int i = 0; i < slotButtons.Length; i++)
        {
            SaveSlotInfo info = GameSaveSystem.GetSlotInfo(i + 1, useAuto);
            bool armed = overwriteArmedSlot == i + 1;
            string detail = armed ? "한 번 더 누르면 이 저장이 지워지고 새로 시작합니다" : info.Describe();
            slotButtons[i].text = $"슬롯 {i + 1}{(useAuto ? " · 자동 저장" : "")}\n{detail}";
            // 이어하기에서는 비어 있는 슬롯을 누를 수 없다(호스트는 수동 저장 목록에서만 빈 슬롯으로 새로 시작할 수 있다).
            slotButtons[i].SetEnabled(slotMode == SlotMode.NewGame || (slotMode == SlotMode.Host && !useAuto) || info.exists);
            slotButtons[i].EnableInClassList("slot-button-armed", armed);
        }
    }

    private void PickSlot(int slot)
    {
        if (slotMode == SlotMode.Continue)
        {
            if (!GameSession.StartContinue(slot, slotUseAuto)) slotNote.text = "저장 파일이 없습니다";
            return;
        }

        if (slotMode == SlotMode.Host)
        {
            if (!GameSession.StartHost(slot, MultiplayerSession.DefaultPort, slotUseAuto)) slotNote.text = MultiplayerSession.Status;
            return;
        }

        if (GameSaveSystem.SlotExists(slot) && overwriteArmedSlot != slot)
        {
            overwriteArmedSlot = slot;
            RefreshSlots();
            return;
        }
        GameSession.StartNewGame(slot);
    }

    private static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
