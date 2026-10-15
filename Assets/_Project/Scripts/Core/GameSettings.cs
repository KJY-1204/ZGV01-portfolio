// 옵션 창이 다루는 사용자 설정(음량, 화면, 시점 조작) 데이터다. 엔진 호출 없이 값의 범위 규칙과 JSON 직렬화만 맡는다.
using System;
using System.Collections.Generic;
using UnityEngine;

// 사용자가 바꾼 키 하나다. id는 키 동작 이름, path는 입력 컨트롤 경로(예: "<Keyboard>/v")다. 기본값은 저장하지 않는다.
[Serializable]
public class KeyBindingEntry
{
    public string id;
    public string path;
}

[Serializable]
public class GameSettings
{
    // 감도 배율이다. 옵션 창에는 100을 곱해 보여 준다(기본 1 = 화면에 100). 실제 시선 입력에는 LookBaseScale이 한 번 더 곱해져
    // 기본값(100)이 원래 입력의 0.2배(체감 20)로 적용된다.
    public const float MinSensitivity = 0.2f;
    public const float MaxSensitivity = 3f;
    public const float SensitivityDisplayUnit = 100f;
    public const float LookBaseScale = 0.2f;
    public const float MinFovScale = 0.8f;
    public const float MaxFovScale = 1.3f;
    public const float MinUiScale = 0.7f;
    public const float MaxUiScale = 1.5f;

    // 그래픽 품질 단계다. 낮음은 셰이더 효과와 잔디를 거의 끄고, 높음이 원래 설정이다.
    public const int GraphicsLow = 0;
    public const int GraphicsMedium = 1;
    public const int GraphicsHigh = 2;

    public int version = 2;

    // 소리: 마스터와 분류별(효과음, 목소리, 발소리) 음량이다. 실제 음량은 마스터 x 분류다.
    public float masterVolume = 1f;
    public float effectsVolume = 1f;
    public float voicesVolume = 1f;
    public float footstepsVolume = 1f;

    // 해상도와 창 모드(UnityEngine.FullScreenMode 값)다. 0 또는 음수는 지금 값을 그대로 둔다는 뜻이다.
    public int screenWidth;
    public int screenHeight;
    public int fullScreenMode = -1;
    public int graphicsPreset = GraphicsHigh;
    public bool vSync = true;
    // 카메라 시야각에 곱하는 배율이다(1이면 원래 값).
    public float fovScale = 1f;
    // UI 크기 배율이다(1이면 기준 크기).
    public float uiScale = 1f;

    // 마우스 좌우/상하 감도다. 상하 반전은 따로 켠다.
    public float mouseSensitivityX = 1f;
    public float mouseSensitivityY = 1f;
    public bool invertY;

    // 숨기기로 한 HUD 요소의 비트 모음이다(0이면 모두 보인다). 요소별 비트는 HudElement 순서다.
    public int hiddenHud;

    public bool IsHudHidden(HudElement element) => (hiddenHud & (1 << (int)element)) != 0;

    public void SetHudHidden(HudElement element, bool hidden)
    {
        int bit = 1 << (int)element;
        hiddenHud = hidden ? hiddenHud | bit : hiddenHud & ~bit;
    }

    // 기본값에서 바꾼 키만 담는다(없으면 기본 키).
    public List<KeyBindingEntry> keyBindings = new List<KeyBindingEntry>();

    public static GameSettings CreateDefault() => new GameSettings();

    // 감도 배율과 옵션 창에 보이는 값을 서로 바꾼다(기본 배율 1 = 화면 값 100).
    public static float SensitivityToDisplay(float sensitivity) => sensitivity * SensitivityDisplayUnit;
    public static float DisplayToSensitivity(float display) => display / SensitivityDisplayUnit;

    public GameSettings Clone()
    {
        GameSettings copy = (GameSettings)MemberwiseClone();
        copy.keyBindings = new List<KeyBindingEntry>();
        if (keyBindings != null)
            foreach (KeyBindingEntry entry in keyBindings) copy.keyBindings.Add(new KeyBindingEntry { id = entry.id, path = entry.path });
        return copy;
    }

    // 바꾼 키의 경로를 돌려준다. 바꾸지 않았으면 null이다.
    public string GetBinding(string id)
    {
        if (keyBindings == null) return null;
        foreach (KeyBindingEntry entry in keyBindings)
            if (entry != null && entry.id == id) return entry.path;
        return null;
    }

    // 키를 바꾼다. path가 비면 기본값으로 되돌린다(항목을 지운다).
    public void SetBinding(string id, string path)
    {
        if (keyBindings == null) keyBindings = new List<KeyBindingEntry>();
        keyBindings.RemoveAll(entry => entry == null || entry.id == id);
        if (!string.IsNullOrEmpty(path)) keyBindings.Add(new KeyBindingEntry { id = id, path = path });
    }

    // 범위를 벗어나거나 깨진 값을 허용 범위로 되돌린다.
    public void Sanitize()
    {
        masterVolume = Clamp01(masterVolume);
        effectsVolume = Clamp01(effectsVolume);
        voicesVolume = Clamp01(voicesVolume);
        footstepsVolume = Clamp01(footstepsVolume);
        fovScale = Clamp(fovScale, MinFovScale, MaxFovScale, 1f);
        uiScale = Clamp(uiScale, MinUiScale, MaxUiScale, 1f);
        mouseSensitivityX = Clamp(mouseSensitivityX, MinSensitivity, MaxSensitivity, 1f);
        mouseSensitivityY = Clamp(mouseSensitivityY, MinSensitivity, MaxSensitivity, 1f);
        if (screenWidth <= 0 || screenHeight <= 0)
        {
            screenWidth = 0;
            screenHeight = 0;
        }
        if (fullScreenMode < -1) fullScreenMode = -1;
        graphicsPreset = Mathf.Clamp(graphicsPreset, GraphicsLow, GraphicsHigh);
        SanitizeBindings();
    }

    // 비었거나 같은 id가 여럿인 항목을 정리한다(같은 id는 마지막 것만 남긴다).
    private void SanitizeBindings()
    {
        if (keyBindings == null) { keyBindings = new List<KeyBindingEntry>(); return; }
        var cleaned = new List<KeyBindingEntry>();
        foreach (KeyBindingEntry entry in keyBindings)
        {
            if (entry == null || string.IsNullOrEmpty(entry.id) || string.IsNullOrEmpty(entry.path)) continue;
            cleaned.RemoveAll(existing => existing.id == entry.id);
            cleaned.Add(entry);
        }
        keyBindings = cleaned;
    }

    public string ToJson() => JsonUtility.ToJson(this, true);

    // 비었거나 깨진 JSON이면 기본값을 돌려준다(옵션 때문에 게임이 안 켜지면 안 된다).
    public static GameSettings FromJson(string json)
    {
        GameSettings settings = null;
        if (!string.IsNullOrWhiteSpace(json))
        {
            try { settings = JsonUtility.FromJson<GameSettings>(json); }
            catch (ArgumentException) { settings = null; }
        }
        if (settings == null) settings = CreateDefault();
        settings.Sanitize();
        return settings;
    }

    // 마우스 시선 입력에 기본 배율과 좌우/상하 감도, 상하 반전을 적용한다.
    public Vector2 ScaleLook(Vector2 look)
    {
        look.x *= mouseSensitivityX * LookBaseScale;
        look.y *= mouseSensitivityY * LookBaseScale;
        if (invertY) look.y = -look.y;
        return look;
    }

    private static float Clamp01(float value) => float.IsNaN(value) ? 1f : Mathf.Clamp01(value);

    private static float Clamp(float value, float min, float max, float fallback) => float.IsNaN(value) ? fallback : Mathf.Clamp(value, min, max);
}
