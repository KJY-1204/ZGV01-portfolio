// 플레이어 쪽 전투/조작 효과음을 재생하는 컴포넌트다. 연사 소리가 서로 끊기지 않도록 여러 AudioSource를 돌려 쓴다.
using UnityEngine;

public class CombatAudio : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float masterVolume = 0.8f;
    // 연사(초당 약 10발)에 발사음 꼬리(약 1초)가 겹쳐도 앞 소리를 끊지 않을 만큼 둔다.
    [SerializeField] private int sourceCount = 12;

    private AudioSource[] sources;
    private int next;

    private void Awake()
    {
        sources = new AudioSource[sourceCount];
        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            sources[i] = source;
        }
    }

    public void Play(AudioClip clip, float volume = 1f, float pitchVariation = 0.04f)
    {
        if (clip == null || sources == null) return;

        AudioSource source = sources[next];
        next = (next + 1) % sources.Length;
        source.clip = clip;
        source.volume = Mathf.Clamp01(volume * masterVolume * AudioBus.Volume(AudioCategory.Effects));
        source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        source.Play();
    }

    // 변형이 여러 개면 그중 하나를 무작위로 고른다.
    public void PlayRandom(AudioClip[] clips, float volume = 1f, float pitchVariation = 0.04f)
    {
        if (clips == null || clips.Length == 0) return;
        Play(clips[Random.Range(0, clips.Length)], volume, pitchVariation);
    }
}
