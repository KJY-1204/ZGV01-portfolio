// 사용자 설정(GameSettings)을 파일에 저장/불러오고 엔진에 적용하는 정적 저장소다. 시작할 때 스스로 불러와 적용한다.
using System;
using System.IO;
using UnityEngine;

public static class GameSettingsStore
{
    private static GameSettings current = GameSettings.CreateDefault();

    // 읽기 전용 복사본이다. 바꾸려면 Apply로 새 값을 넣는다.
    public static GameSettings Current => current;
    public static event Action Changed;

    private static string FilePath => Path.Combine(Application.persistentDataPath, "settings.json");

    // 카메라 시야각에 곱할 배율이다(CombatCameraController가 읽는다).
    public static float FovScale => current.fovScale;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        current = Load();
        ApplyToEngine(current);
    }

    // 마우스 시선 입력에 감도와 상하 반전을 적용한다(StarterAssetsInputs가 부른다).
    public static Vector2 ScaleLook(Vector2 look) => current.ScaleLook(look);

    // 새 설정을 엔진에 적용한다. persist가 참이면 파일에도 저장한다(슬라이더를 끄는 동안처럼 자주 바뀔 때는 거짓으로 하고 나중에 Persist를 부른다).
    public static void Apply(GameSettings settings, bool persist = true)
    {
        GameSettings next = settings.Clone();
        next.Sanitize();
        current = next;
        ApplyToEngine(current);
        if (persist) Save(current);
        Changed?.Invoke();
    }

    // 지금 설정을 파일에 저장한다.
    public static void Persist() => Save(current);

    private static GameSettings Load()
    {
        try
        {
            return File.Exists(FilePath) ? GameSettings.FromJson(File.ReadAllText(FilePath)) : GameSettings.CreateDefault();
        }
        catch (IOException)
        {
            return GameSettings.CreateDefault();
        }
    }

    private static void Save(GameSettings settings)
    {
        try
        {
            File.WriteAllText(FilePath, settings.ToJson());
        }
        catch (IOException exception)
        {
            Debug.LogWarning("설정을 저장하지 못했다: " + exception.Message);
        }
    }

    private static void ApplyToEngine(GameSettings settings)
    {
        AudioListener.volume = settings.masterVolume;
        GraphicsPresets.Apply(settings.graphicsPreset);
        QualitySettings.vSyncCount = settings.vSync ? 1 : 0;

        if (settings.screenWidth > 0 && settings.screenHeight > 0)
        {
            FullScreenMode mode = settings.fullScreenMode >= 0 ? (FullScreenMode)settings.fullScreenMode : Screen.fullScreenMode;
            if (Screen.width != settings.screenWidth || Screen.height != settings.screenHeight || Screen.fullScreenMode != mode)
                Screen.SetResolution(settings.screenWidth, settings.screenHeight, mode);
        }
        else if (settings.fullScreenMode >= 0 && (FullScreenMode)settings.fullScreenMode != Screen.fullScreenMode)
        {
            Screen.fullScreenMode = (FullScreenMode)settings.fullScreenMode;
        }
    }
}
