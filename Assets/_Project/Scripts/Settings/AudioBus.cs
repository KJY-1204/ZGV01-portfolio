// 소리 분류(효과음, 목소리, 발소리)별 음량을 알려 주는 정적 도우미다. 마스터 음량은 AudioListener.volume이 맡고, 각 재생 지점이 자기 분류 음량을 곱한다.
using UnityEngine;

public enum AudioCategory
{
    Effects,
    Voices,
    Footsteps,
}

public static class AudioBus
{
    // 옵션 창에서 정한 분류 음량(0~1)이다.
    public static float Volume(AudioCategory category)
    {
        GameSettings settings = GameSettingsStore.Current;
        switch (category)
        {
            case AudioCategory.Voices: return settings.voicesVolume;
            case AudioCategory.Footsteps: return settings.footstepsVolume;
            default: return settings.effectsVolume;
        }
    }
}
