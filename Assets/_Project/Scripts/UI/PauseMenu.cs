// Esc로 여는 일시정지 메뉴와 옵션 창(소리, 화면, 조작)의 동작을 맡는 UI 클래스다. 구조는 Hud.uxml, 모양은 Hud.uss에 있다. 열려 있는 동안 게임 시간을 멈추고, 설정은 바꾸는 즉시 적용한다.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UIElements;

public class PauseMenu
{
    private readonly VisualElement panel;
    private readonly VisualElement mainView;
    private readonly VisualElement optionsView;
    private readonly Label title;
    private readonly Label note;
    private readonly Func<GameObject> getPlayer;
    // 메인 메뉴에서 옵션 창만 쓰는 모드다(시간을 멈추지 않고, 뒤로를 누르면 창이 닫힌다).
    private readonly bool standalone;

    private readonly Dictionary<Button, VisualElement> tabs = new Dictionary<Button, VisualElement>();

    // 키 설정 페이지: 목록, 안내 문구, 각 키의 버튼과 지금 키를 기다리는 중인 동작이다.
    private ScrollView keysList;
    private Label keysHint;
    private readonly Dictionary<GameKey, Button> keyButtons = new Dictionary<GameKey, Button>();
    private GameKey? listeningKey;
    private IDisposable listener;
    private int escapeConsumedFrame = -1;
    private const string KeysHintDefault = "키를 누르면 바꿀 수 있습니다. 같은 범위에서 겹치면 서로 바뀝니다. (Esc: 취소)";

    private Slider master, effects, voices, footsteps, fov, uiScale, sensitivityX, sensitivityY;
    private DropdownField graphics, resolution, window;
    private Toggle vSync, invertY;

    // HUD 탭의 요소별 표시 토글(켜짐 = 보임)이다.
    private readonly Dictionary<HudElement, Toggle> hudToggles = new Dictionary<HudElement, Toggle>();
    private static readonly (HudElement element, string name)[] HudToggleNames =
    {
        (HudElement.Compass, "opt-hud-compass"), (HudElement.Clock, "opt-hud-clock"), (HudElement.Survival, "opt-hud-survival"),
        (HudElement.Reticle, "opt-hud-reticle"), (HudElement.Ammo, "opt-hud-ammo"), (HudElement.InteractPrompt, "opt-hud-prompt"),
        (HudElement.PickupFeed, "opt-hud-pickup"), (HudElement.Notices, "opt-hud-notices"), (HudElement.ControlHints, "opt-hud-hints"),
    };

    private readonly List<Vector2Int> resolutionChoices = new List<Vector2Int>();
    // 창 모드 선택지(표시 이름과 FullScreenMode 값)다.
    private static readonly (string label, FullScreenMode mode)[] WindowChoices =
    {
        ("전체 화면(창)", FullScreenMode.FullScreenWindow),
        ("독점 전체 화면", FullScreenMode.ExclusiveFullScreen),
        ("창 모드", FullScreenMode.Windowed),
    };

    // 그래픽 품질 선택지(GameSettings.GraphicsLow/Medium/High 순서)다.
    private static readonly string[] GraphicsLabels = { "낮음", "중간", "높음" };

    // 메인 메뉴로 나가기는 저장하지 않은 진행이 사라지므로 두 번 눌러야 한다.
    private Button menuButton;
    private bool menuConfirm;
    private const string MenuButtonLabel = "메인 메뉴";

    private bool refreshing;
    private bool dirty;
    private float previousTimeScale = 1f;
    private bool previousAudioPause;
    private bool freezesTime;

    public bool IsOpen { get; private set; }
    // 열리거나 닫힐 때 알린다(커서 잠금 갱신용).
    public event Action<bool> OpenChanged;

    public PauseMenu(VisualElement root, Func<GameObject> playerGetter, bool standalone = false)
    {
        getPlayer = playerGetter;
        this.standalone = standalone;
        panel = root.Q<VisualElement>("pause-panel");
        mainView = root.Q<VisualElement>("pause-main");
        optionsView = root.Q<VisualElement>("options-view");
        title = root.Q<Label>("pause-title");
        note = root.Q<Label>("pause-note");
        if (panel == null || mainView == null || optionsView == null) return;

        BindMainButtons(root);
        BindOptions(root);
        panel.style.display = DisplayStyle.None;
    }

    private void BindMainButtons(VisualElement root)
    {
        root.Q<Button>("pause-resume").clicked += Close;
        root.Q<Button>("pause-options").clicked += ShowOptions;
        root.Q<Button>("pause-save").clicked += Save;
        root.Q<Button>("pause-load").clicked += Load;
        menuButton = root.Q<Button>("pause-menu");
        menuButton.clicked += OnMenuButton;
        root.Q<Button>("pause-quit").clicked += Quit;
    }

    private void BindOptions(VisualElement root)
    {
        tabs[root.Q<Button>("tab-audio")] = root.Q<VisualElement>("options-audio");
        tabs[root.Q<Button>("tab-display")] = root.Q<VisualElement>("options-display");
        tabs[root.Q<Button>("tab-controls")] = root.Q<VisualElement>("options-controls");
        tabs[root.Q<Button>("tab-keys")] = root.Q<VisualElement>("options-keys");
        tabs[root.Q<Button>("tab-hud")] = root.Q<VisualElement>("options-hud");
        keysList = root.Q<ScrollView>("keys-list");
        keysHint = root.Q<Label>("keys-hint");
        foreach (KeyValuePair<Button, VisualElement> tab in tabs)
        {
            Button button = tab.Key;
            button.clicked += () => SelectTab(button);
        }

        master = root.Q<Slider>("opt-master");
        effects = root.Q<Slider>("opt-effects");
        voices = root.Q<Slider>("opt-voices");
        footsteps = root.Q<Slider>("opt-footsteps");
        fov = root.Q<Slider>("opt-fov");
        uiScale = root.Q<Slider>("opt-ui-scale");
        sensitivityX = root.Q<Slider>("opt-sensitivity-x");
        sensitivityY = root.Q<Slider>("opt-sensitivity-y");
        graphics = root.Q<DropdownField>("opt-graphics");
        resolution = root.Q<DropdownField>("opt-resolution");
        window = root.Q<DropdownField>("opt-window");
        vSync = root.Q<Toggle>("opt-vsync");
        invertY = root.Q<Toggle>("opt-invert");

        // 슬라이더 값은 사용자 화면 단위(퍼센트)로 보여 주고 저장은 비율로 한다.
        master.RegisterValueChangedCallback(e => Change(s => s.masterVolume = e.newValue / 100f));
        effects.RegisterValueChangedCallback(e => Change(s => s.effectsVolume = e.newValue / 100f));
        voices.RegisterValueChangedCallback(e => Change(s => s.voicesVolume = e.newValue / 100f));
        footsteps.RegisterValueChangedCallback(e => Change(s => s.footstepsVolume = e.newValue / 100f));
        fov.RegisterValueChangedCallback(e => Change(s => s.fovScale = e.newValue / 100f));
        uiScale.RegisterValueChangedCallback(e => Change(s => s.uiScale = e.newValue / 100f));
        sensitivityX.RegisterValueChangedCallback(e => Change(s => s.mouseSensitivityX = GameSettings.DisplayToSensitivity(e.newValue)));
        sensitivityY.RegisterValueChangedCallback(e => Change(s => s.mouseSensitivityY = GameSettings.DisplayToSensitivity(e.newValue)));
        graphics.RegisterValueChangedCallback(_ => Change(s => s.graphicsPreset = graphics.index));
        vSync.RegisterValueChangedCallback(e => Change(s => s.vSync = e.newValue));
        invertY.RegisterValueChangedCallback(e => Change(s => s.invertY = e.newValue));
        foreach ((HudElement element, string name) in HudToggleNames)
        {
            Toggle toggle = root.Q<Toggle>(name);
            hudToggles[element] = toggle;
            HudElement captured = element;
            toggle.RegisterValueChangedCallback(e => Change(s => s.SetHudHidden(captured, !e.newValue)));
        }
        resolution.RegisterValueChangedCallback(_ => Change(s =>
        {
            int index = resolution.index;
            if (index < 0 || index >= resolutionChoices.Count) return;
            s.screenWidth = resolutionChoices[index].x;
            s.screenHeight = resolutionChoices[index].y;
            // 해상도를 정하면 창 모드도 지금 값으로 같이 고정한다.
            if (s.fullScreenMode < 0) s.fullScreenMode = (int)Screen.fullScreenMode;
        }));
        window.RegisterValueChangedCallback(_ => Change(s =>
        {
            int index = window.index;
            if (index >= 0 && index < WindowChoices.Length) s.fullScreenMode = (int)WindowChoices[index].mode;
        }));

        root.Q<Button>("opt-reset").clicked += ResetToDefaults;
        root.Q<Button>("opt-back").clicked += ShowMain;
    }

    // ---- 열기/닫기 ----

    public void Open()
    {
        if (IsOpen || panel == null) return;

        IsOpen = true;
        // 멀티플레이 중에는 월드가 모두에게 계속 흐르므로 시간과 소리를 멈추지 않는다.
        freezesTime = !MultiplayerSession.Active;
        previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        previousAudioPause = AudioListener.pause;
        if (freezesTime)
        {
            Time.timeScale = 0f;
            AudioListener.pause = true;
        }
        panel.style.display = DisplayStyle.Flex;
        SetNote(MultiplayerNote());
        ShowMain();
        OpenChanged?.Invoke(true);
    }

    public void Close()
    {
        if (!IsOpen) return;

        StopListening();
        PersistIfDirty();
        IsOpen = false;
        if (freezesTime)
        {
            Time.timeScale = previousTimeScale;
            AudioListener.pause = previousAudioPause;
        }
        panel.style.display = DisplayStyle.None;
        OpenChanged?.Invoke(false);
    }

    private void OnMenuButton()
    {
        if (!menuConfirm)
        {
            menuConfirm = true;
            // 호스트가 나가면 세션이 끝나 모든 참가자가 나간다.
            menuButton.text = MultiplayerSession.IsHost ? "한 번 더 누르면 세션이 끝나 모두 나갑니다 (저장 안 됨)" : "한 번 더 누르면 나갑니다 (저장 안 됨)";
            return;
        }
        GameSession.ReturnToMenu();
    }

    private void ResetMenuConfirm()
    {
        menuConfirm = false;
        if (menuButton != null) menuButton.text = MenuButtonLabel;
    }

    // 메인 메뉴에서 옵션 창을 바로 연다. 키 설정은 게임 씬의 입력에 연결돼야 하므로 탭을 숨긴다.
    public void OpenOptions()
    {
        if (!standalone || IsOpen || panel == null) return;

        IsOpen = true;
        panel.style.display = DisplayStyle.Flex;
        foreach (KeyValuePair<Button, VisualElement> tab in tabs)
            if (tab.Value.name == "options-keys") tab.Key.style.display = DisplayStyle.None;
        ShowOptions();
        OpenChanged?.Invoke(true);
    }

    // Esc: 옵션 안이면 메뉴로 돌아가고, 메뉴에서는 게임으로 돌아간다.
    public void HandleEscape()
    {
        if (!IsOpen) return;
        // 키를 기다리는 중의 Esc는 취소만 한다(같은 프레임에 이미 취소됐어도 옵션을 닫지 않는다).
        if (listeningKey != null || escapeConsumedFrame == Time.frameCount)
        {
            StopListening();
            return;
        }
        if (optionsView.style.display == DisplayStyle.Flex) ShowMain();
        else Close();
    }

    // 객체가 사라질 때(씬 전환) 게임 시간이 멈춘 채 남지 않게 되돌린다.
    public void Dispose()
    {
        listener?.Dispose();
        listener = null;
        if (!IsOpen) return;
        if (freezesTime)
        {
            Time.timeScale = previousTimeScale;
            AudioListener.pause = previousAudioPause;
        }
        IsOpen = false;
    }

    // ---- 화면 전환 ----

    private void ShowMain()
    {
        StopListening();
        PersistIfDirty();
        if (standalone)
        {
            // 메인 메뉴에서는 옵션이 유일한 화면이라 닫는 것이 곧 뒤로 가기다.
            IsOpen = false;
            panel.style.display = DisplayStyle.None;
            OpenChanged?.Invoke(false);
            return;
        }
        optionsView.style.display = DisplayStyle.None;
        mainView.style.display = DisplayStyle.Flex;
        ResetMenuConfirm();
        title.text = "일시정지";
    }

    private void ShowOptions()
    {
        RefreshControls();
        mainView.style.display = DisplayStyle.None;
        optionsView.style.display = DisplayStyle.Flex;
        title.text = "옵션";
        SelectTab(FirstTab());
    }

    private Button FirstTab()
    {
        foreach (Button button in tabs.Keys) return button;
        return null;
    }

    private void SelectTab(Button selected)
    {
        StopListening();
        foreach (KeyValuePair<Button, VisualElement> tab in tabs)
        {
            bool active = tab.Key == selected;
            tab.Key.EnableInClassList("options-tab-active", active);
            tab.Value.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    // ---- 키 설정 ----

    // 동작을 묶음(이동, 행동, 화면, 건축 모드)별로 나누어 이름과 현재 키 버튼을 한 줄씩 만든다.
    private void BuildKeyRows()
    {
        keysList.Clear();
        keyButtons.Clear();
        string group = null;
        foreach (KeyBindings.Def def in KeyBindings.All)
        {
            if (def.group != group)
            {
                group = def.group;
                var header = new Label(group);
                header.AddToClassList("keys-group");
                keysList.Add(header);
            }

            var row = new VisualElement();
            row.AddToClassList("key-row");
            var label = new Label(def.label);
            label.AddToClassList("key-row-label");
            var button = new Button();
            button.AddToClassList("key-row-button");
            GameKey key = def.key;
            button.clicked += () => StartListening(key);
            row.Add(label);
            row.Add(button);
            keysList.Add(row);
            keyButtons[def.key] = button;
        }
        RefreshKeyLabels();
        keysHint.text = KeysHintDefault;
    }

    private void RefreshKeyLabels()
    {
        foreach (KeyValuePair<GameKey, Button> pair in keyButtons)
        {
            pair.Value.text = KeyBindings.Label(pair.Key);
            pair.Value.EnableInClassList("key-row-button-listening", listeningKey == pair.Key);
        }
    }

    // 다음에 누르는 키보드 키를 이 동작의 키로 삼는다. Esc는 취소, 마우스/게임패드 입력은 무시하고 계속 기다린다.
    private void StartListening(GameKey key)
    {
        StopListening();
        listeningKey = key;
        keyButtons[key].text = "키를 누르세요...";
        keyButtons[key].AddToClassList("key-row-button-listening");
        listener = InputSystem.onAnyButtonPress.CallOnce(OnAnyButton);
    }

    private void OnAnyButton(InputControl control)
    {
        listener = null;
        if (listeningKey == null) return;

        if (control is UnityEngine.InputSystem.Controls.KeyControl escape && escape.keyCode == Key.Escape)
        {
            escapeConsumedFrame = Time.frameCount;
            StopListening();
            return;
        }

        string path = KeyBindings.PathFromControl(control);
        if (path == null)
        {
            // 키보드 키가 아니면 무시하고 계속 기다린다.
            listener = InputSystem.onAnyButtonPress.CallOnce(OnAnyButton);
            return;
        }

        GameKey key = listeningKey.Value;
        listeningKey = null;
        string swapped = KeyBindings.Rebind(key, path);
        dirty = true;
        RefreshKeyLabels();
        keysHint.text = swapped != null ? $"'{swapped}'와(과) 키가 서로 바뀌었습니다" : KeysHintDefault;
    }

    private void StopListening()
    {
        listener?.Dispose();
        listener = null;
        if (listeningKey == null) return;
        listeningKey = null;
        RefreshKeyLabels();
    }

    // ---- 설정 ----

    // 현재 설정에서 값을 바꿔 즉시 적용한다. 파일 저장은 옵션을 닫을 때 한 번만 한다(슬라이더를 끄는 동안 계속 쓰지 않는다).
    private void Change(Action<GameSettings> edit)
    {
        if (refreshing) return;
        GameSettings next = GameSettingsStore.Current.Clone();
        edit(next);
        GameSettingsStore.Apply(next, false);
        dirty = true;
    }

    private void PersistIfDirty()
    {
        if (!dirty) return;
        GameSettingsStore.Persist();
        dirty = false;
    }

    private void ResetToDefaults()
    {
        GameSettings defaults = GameSettings.CreateDefault();
        // 해상도/창 모드는 기본값이 "지금 값 유지"라서, 값을 비워 두는 대신 지금 값을 그대로 확정해 두고 나머지만 되돌린다.
        defaults.screenWidth = GameSettingsStore.Current.screenWidth;
        defaults.screenHeight = GameSettingsStore.Current.screenHeight;
        defaults.fullScreenMode = GameSettingsStore.Current.fullScreenMode;
        GameSettingsStore.Apply(defaults, false);
        dirty = true;
        RefreshControls();
    }

    // 저장된 설정으로 컨트롤 값을 채운다(값 변경 콜백이 다시 적용하지 않게 막는다).
    private void RefreshControls()
    {
        BuildKeyRows();
        refreshing = true;
        GameSettings s = GameSettingsStore.Current;
        master.SetValueWithoutNotify(s.masterVolume * 100f);
        effects.SetValueWithoutNotify(s.effectsVolume * 100f);
        voices.SetValueWithoutNotify(s.voicesVolume * 100f);
        footsteps.SetValueWithoutNotify(s.footstepsVolume * 100f);
        fov.SetValueWithoutNotify(s.fovScale * 100f);
        uiScale.SetValueWithoutNotify(s.uiScale * 100f);
        sensitivityX.SetValueWithoutNotify(GameSettings.SensitivityToDisplay(s.mouseSensitivityX));
        sensitivityY.SetValueWithoutNotify(GameSettings.SensitivityToDisplay(s.mouseSensitivityY));
        vSync.SetValueWithoutNotify(s.vSync);
        invertY.SetValueWithoutNotify(s.invertY);
        foreach (KeyValuePair<HudElement, Toggle> pair in hudToggles) pair.Value.SetValueWithoutNotify(!s.IsHudHidden(pair.Key));

        FillResolutions(s);
        window.choices = new List<string>(Array.ConvertAll(WindowChoices, c => c.label));
        FullScreenMode currentMode = s.fullScreenMode >= 0 ? (FullScreenMode)s.fullScreenMode : Screen.fullScreenMode;
        int windowIndex = Array.FindIndex(WindowChoices, c => c.mode == currentMode);
        window.SetValueWithoutNotify(windowIndex >= 0 ? WindowChoices[windowIndex].label : WindowChoices[0].label);
        window.index = Mathf.Max(0, windowIndex);

        graphics.choices = new List<string>(GraphicsLabels);
        graphics.index = Mathf.Clamp(s.graphicsPreset, 0, GraphicsLabels.Length - 1);
        refreshing = false;
    }

    private void FillResolutions(GameSettings s)
    {
        resolutionChoices.Clear();
        var labels = new List<string>();
        foreach (Resolution r in Screen.resolutions)
        {
            Vector2Int size = new Vector2Int(r.width, r.height);
            if (resolutionChoices.Contains(size)) continue;
            resolutionChoices.Add(size);
        }
        // 에디터처럼 해상도 목록이 비어 있으면 지금 화면 크기만 보여 준다.
        Vector2Int current = s.screenWidth > 0 ? new Vector2Int(s.screenWidth, s.screenHeight) : new Vector2Int(Screen.width, Screen.height);
        if (!resolutionChoices.Contains(current)) resolutionChoices.Add(current);
        resolutionChoices.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        foreach (Vector2Int size in resolutionChoices) labels.Add(size.x + " x " + size.y);

        resolution.choices = labels;
        resolution.index = Mathf.Max(0, resolutionChoices.IndexOf(current));
    }

    // ---- 게임 ----

    private void Save()
    {
        GameObject player = getPlayer();
        if (player == null) { SetNote("플레이어를 찾을 수 없습니다"); return; }
        if (MultiplayerSession.IsClientOnly) { SetNote("호스트만 저장할 수 있습니다"); return; }
        SaveLoadDebugController.SaveNow(player);
        SetNote($"저장했습니다 (슬롯 {GameSaveSystem.CurrentSlot})");
    }

    private void Load()
    {
        GameObject player = getPlayer();
        if (player == null) { SetNote("플레이어를 찾을 수 없습니다"); return; }
        if (MultiplayerSession.Active) { SetNote("멀티플레이 중에는 불러올 수 없습니다 (슬롯을 바꾸거나 불러오려면 메인 메뉴에서 다시 시작하세요)"); return; }
        if (!GameSaveSystem.HasSave) { SetNote($"슬롯 {GameSaveSystem.CurrentSlot}에 저장 파일이 없습니다"); return; }
        if (SaveLoadDebugController.LoadNow(player)) Close();
        else SetNote("불러오지 못했습니다");
    }

    private static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // 멀티플레이 중이면 접속 정보(호스트 주소, 인원)를 안내 문구로 보여 준다.
    private static string MultiplayerNote()
    {
        if (!MultiplayerSession.Active) return "";
        // 멀티플레이 중 저장 슬롯은 세션 동안 고정이다. 저장(수동, 자동, 참가자 데이터)은 호스트가 정한 슬롯에만 쓰이고 참가자는 슬롯이 없다.
        string role = MultiplayerSession.IsHost
            ? $"호스트 {MultiplayerSession.LocalAddress()}:{MultiplayerSession.DefaultPort} · 저장 슬롯 {GameSaveSystem.CurrentSlot}"
            : "참가 중 · 저장은 호스트가 합니다";
        return $"멀티플레이 · {role} · {MultiplayerSession.PlayerCount}명";
    }

    private void SetNote(string text)
    {
        if (note != null) note.text = text;
    }
}
