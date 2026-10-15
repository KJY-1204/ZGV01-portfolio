// 같은 상황에서 쓰는 효과음 변형 묶음이다(클립 여러 개, 음량, 피치 범위). 소리 데이터를 에셋에 담을 때 공통으로 쓴다.
using System;
using UnityEngine;

[Serializable]
public class SoundGroup
{
    public AudioClip[] clips = new AudioClip[0];
    [Range(0f, 1f)] public float volume = 1f;
    // 재생마다 이 범위에서 피치를 고른다. 낮은 값은 더 굵고 느린 소리가 된다.
    public Vector2 pitchRange = Vector2.one;

    public bool HasClips => clips != null && clips.Length > 0;

    public AudioClip PickClip()
    {
        return HasClips ? clips[UnityEngine.Random.Range(0, clips.Length)] : null;
    }

    public float PickPitch()
    {
        return UnityEngine.Random.Range(pitchRange.x, pitchRange.y);
    }
}
