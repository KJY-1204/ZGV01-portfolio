// 게임 키 설정의 중심 클래스다. 입력 액션 에셋(이동, 점프 등)에 연결된 키와 코드가 직접 읽는 키(인벤토리, 제작 등)를 하나의 목록으로 묶어 이름, 기본값, 현재 키를 알려 주고 바꾼다.
// 바꾼 키는 GameSettings.keyBindings에 저장된다. 액션 에셋의 오버라이드는 끝날 때 지워 에셋이 바뀐 채 남지 않게 한다.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;

public enum GameKey
{
    MoveForward, MoveBack, MoveLeft, MoveRight, Jump, Sprint, Interact, Reload, Holster, BuildMode,
    Inventory, Craft, Equipment, Lantern, Repair,
    Demolish, RotateLeft, RotateRight, LevelUp, LevelDown,
}

public static class KeyBindings
{
    public sealed class Def
    {
        public GameKey key;
        public string label;
        public string group;
        // 기본 키 경로다. 액션 에셋에 연결된 키는 에셋의 값을 읽어 채운다.
        public string defaultPath;
        // 같은 범위 안에서만 키가 겹치는지 본다(건축 모드 전용 키는 평소 키와 겹쳐도 된다).
        public int scope;
        // 액션 에셋에 연결된 키면 액션 이름과 복합 바인딩 조각 이름(없으면 null)이다. 코드가 읽는 키면 actionName이 null이다.
        public string actionName;
        public string partName;
    }

    private static readonly Def[] Defs =
    {
        Asset(GameKey.MoveForward, "앞으로", "이동", "Move", "up"),
        Asset(GameKey.MoveBack, "뒤로", "이동", "Move", "down"),
        Asset(GameKey.MoveLeft, "왼쪽", "이동", "Move", "left"),
        Asset(GameKey.MoveRight, "오른쪽", "이동", "Move", "right"),
        Asset(GameKey.Jump, "점프", "이동", "Jump", null),
        Asset(GameKey.Sprint, "달리기", "이동", "Sprint", null),
        Asset(GameKey.Interact, "상호작용(길게)", "행동", "Interact", null),
        Asset(GameKey.Reload, "장전", "행동", "Reload", null),
        Asset(GameKey.Holster, "무기 집어넣기", "행동", "Holster", null),
        Asset(GameKey.BuildMode, "건축 모드", "행동", "BuildMode", null),
        Code(GameKey.Lantern, "랜턴", "행동", "<Keyboard>/l", 0),
        Code(GameKey.Repair, "수리(길게)", "행동", "<Keyboard>/z", 0),
        Code(GameKey.Inventory, "인벤토리", "화면", "<Keyboard>/tab", 0),
        Code(GameKey.Craft, "제작", "화면", "<Keyboard>/c", 0),
        Code(GameKey.Equipment, "장비", "화면", "<Keyboard>/v", 0),
        Code(GameKey.Demolish, "철거", "건축 모드", "<Keyboard>/x", 1),
        Code(GameKey.RotateLeft, "왼쪽 회전", "건축 모드", "<Keyboard>/q", 1),
        Code(GameKey.RotateRight, "오른쪽 회전", "건축 모드", "<Keyboard>/e", 1),
        Code(GameKey.LevelUp, "층 올리기", "건축 모드", "<Keyboard>/r", 1),
        Code(GameKey.LevelDown, "층 내리기", "건축 모드", "<Keyboard>/f", 1),
    };

    private static Def Asset(GameKey key, string label, string group, string action, string part) =>
        new Def { key = key, label = label, group = group, scope = 0, actionName = action, partName = part };

    private static Def Code(GameKey key, string label, string group, string path, int scope) =>
        new Def { key = key, label = label, group = group, defaultPath = path, scope = scope };

    public static IReadOnlyList<Def> All => Defs;

    // 액션 에셋에 연결된 키의 바인딩 위치(액션, 인덱스)다.
    private struct AssetSlot
    {
        public InputAction action;
        public int index;
    }

    private static readonly Dictionary<GameKey, AssetSlot> assetSlots = new Dictionary<GameKey, AssetSlot>();
    private static readonly Dictionary<string, ButtonControl> controlCache = new Dictionary<string, ButtonControl>();
    private static InputActionAsset attachedActions;
    private static bool quitHooked;
    // 이번 씬에서 이미 액션 에셋 연결을 시도했는지다(플레이어 입력이 없는 씬에서 매번 찾지 않게 한다).
    private static bool attachAttempted;

    private static void EnsureAttached()
    {
        if (attachedActions == null && !attachAttempted) Attach();
    }

    // 씬이 열릴 때마다 그 씬의 플레이어 입력에 다시 연결한다(메인 메뉴에서 게임 씬으로 넘어가는 경우를 위해).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Hook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Attach();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        attachedActions = null;
        Attach();
    }

    private static void Attach()
    {
        attachAttempted = true;
        GameSettingsStore.Changed -= ApplyToActions;
        GameSettingsStore.Changed += ApplyToActions;

        PlayerInput playerInput = UnityEngine.Object.FindFirstObjectByType<PlayerInput>();
        if (playerInput == null || playerInput.actions == null) return;

        attachedActions = playerInput.actions;
        CaptureAssetSlots();
        if (!quitHooked)
        {
            quitHooked = true;
            Application.quitting += Restore;
        }
        ApplyToActions();
    }

    // 액션 에셋에서 각 키가 쓰는 바인딩(키보드 그룹의 첫 번째)과 기본 경로를 찾는다.
    private static void CaptureAssetSlots()
    {
        assetSlots.Clear();
        foreach (Def def in Defs)
        {
            if (def.actionName == null) continue;
            InputAction action = attachedActions.FindAction(def.actionName, false);
            if (action == null) continue;

            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding binding = action.bindings[i];
                bool matches = def.partName != null ? binding.isPartOfComposite && binding.name == def.partName : !binding.isComposite && !binding.isPartOfComposite;
                if (!matches || !binding.groups.Contains("Keyboard&Mouse") || !binding.path.StartsWith("<Keyboard>")) continue;

                assetSlots[def.key] = new AssetSlot { action = action, index = i };
                // 오버라이드가 남아 있어도 원래 경로를 기본값으로 쓴다.
                def.defaultPath = binding.path;
                break;
            }
        }
    }

    // 설정에 저장된 키를 액션 에셋에 반영한다(바꾸지 않은 키는 오버라이드를 지워 기본으로 둔다).
    private static void ApplyToActions()
    {
        controlCache.Clear();
        if (attachedActions == null) return;

        foreach (KeyValuePair<GameKey, AssetSlot> pair in assetSlots)
        {
            string saved = GameSettingsStore.Current.GetBinding(pair.Key.ToString());
            AssetSlot slot = pair.Value;
            if (string.IsNullOrEmpty(saved)) slot.action.RemoveBindingOverride(slot.index);
            else slot.action.ApplyBindingOverride(slot.index, saved);
        }
    }

    // 끝날 때 오버라이드를 모두 지워 입력 에셋이 바뀐 채 남지 않게 한다.
    private static void Restore()
    {
        Application.quitting -= Restore;
        quitHooked = false;
        if (attachedActions != null) attachedActions.RemoveAllBindingOverrides();
    }

    // ---- 조회 ----

    public static Def Find(GameKey key)
    {
        foreach (Def def in Defs) if (def.key == key) return def;
        return null;
    }

    // 지금 쓰는 키 경로다(바꿨으면 바꾼 키, 아니면 기본 키).
    public static string PathOf(GameKey key)
    {
        EnsureAttached();
        string saved = GameSettingsStore.Current.GetBinding(key.ToString());
        if (!string.IsNullOrEmpty(saved)) return saved;
        return Find(key)?.defaultPath;
    }

    // 화면에 보일 키 이름이다(예: "Tab", "Left Shift").
    public static string Label(GameKey key) => LabelOfPath(PathOf(key));

    public static string LabelOfPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return "-";
        return InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice);
    }

    // ---- 코드가 읽는 키 ----

    private static ButtonControl Control(GameKey key)
    {
        string path = PathOf(key);
        if (string.IsNullOrEmpty(path) || Keyboard.current == null) return null;
        if (controlCache.TryGetValue(path, out ButtonControl cached) && cached != null && cached.device.added) return cached;

        ButtonControl control = InputSystem.FindControl(path) as ButtonControl;
        controlCache[path] = control;
        return control;
    }

    public static bool Pressed(GameKey key)
    {
        ButtonControl control = Control(key);
        return control != null && control.wasPressedThisFrame;
    }

    public static bool Held(GameKey key)
    {
        ButtonControl control = Control(key);
        return control != null && control.isPressed;
    }

    // ---- 바꾸기 ----

    // 키를 바꾼다. 같은 범위에서 이미 그 키를 쓰는 동작이 있으면 서로 바꾼다. 바뀐 동작의 이름을 돌려준다(없으면 null).
    public static string Rebind(GameKey key, string newPath)
    {
        Def target = Find(key);
        if (target == null || string.IsNullOrEmpty(newPath)) return null;

        string oldPath = PathOf(key);
        string swappedLabel = null;
        GameSettings next = GameSettingsStore.Current.Clone();

        foreach (Def other in Defs)
        {
            if (other.key == key || other.scope != target.scope || PathOf(other.key) != newPath) continue;
            SetOrClear(next, other, oldPath);
            swappedLabel = other.label;
            break;
        }
        SetOrClear(next, target, newPath);
        GameSettingsStore.Apply(next, false);
        return swappedLabel;
    }

    // 기본 키와 같으면 저장 항목을 지우고, 다르면 저장한다.
    private static void SetOrClear(GameSettings settings, Def def, string path)
    {
        settings.SetBinding(def.key.ToString(), path == def.defaultPath ? null : path);
    }

    // 눌린 컨트롤이 설정할 수 있는 키보드 키면 경로를 돌려준다(아니면 null). 마우스/게임패드 입력은 무시한다.
    public static string PathFromControl(InputControl control)
    {
        if (control == null || !(control.device is Keyboard) || !(control is KeyControl key)) return null;
        // 다시 읽을 수 있도록 장치 앞 경로 "<Keyboard>/이름" 형태로 만든다.
        return "<Keyboard>/" + key.name;
    }
}
