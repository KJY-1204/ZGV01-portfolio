// 표면 종류별 탄착 이펙트 프리팹을 데이터로 관리하는 에셋이다. Resources에서 한 번 불러 쓴다.
using System;
using UnityEngine;

public enum ImpactSurface
{
    Concrete,
    Dirt,
    Metal,
    Sand,
    SoftBody,
    Wood,
}

[CreateAssetMenu(fileName = "ImpactEffectLibrary", menuName = "ZGV01/Combat/Impact Effect Library")]
public class ImpactEffectLibrary : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public ImpactSurface surface;
        public GameObject effect;

        // 맞은 지점에서 나는 소리다. 변형 중 하나를 무작위로 고른다.
        public AudioClip[] sounds = new AudioClip[0];
        [Range(0f, 1f)] public float soundVolume = 0.8f;
        public float soundPitch = 1f;
        // 긴 클립에서 일부만 쓸 때 시작 시각과 길이(초)다. 길이가 0이면 클립 전체를 쓴다.
        public float soundStart;
        public float soundDuration;
    }

    private const string ResourcePath = "ImpactEffectLibrary";

    [SerializeField] private Entry[] entries = Array.Empty<Entry>();
    // 표면 표식이 없는 물체에 쓰는 기본 표면이다.
    [SerializeField] private ImpactSurface defaultSurface = ImpactSurface.Concrete;

    private static ImpactEffectLibrary instance;

    public static ImpactEffectLibrary Instance
    {
        get
        {
            if (instance == null) instance = Resources.Load<ImpactEffectLibrary>(ResourcePath);
            return instance;
        }
    }

    public ImpactSurface DefaultSurface => defaultSurface;

    public Entry GetEntry(ImpactSurface surface)
    {
        foreach (Entry entry in entries)
            if (entry != null && entry.surface == surface) return entry;
        return null;
    }

    public GameObject GetEffect(ImpactSurface surface)
    {
        Entry entry = GetEntry(surface);
        return entry != null ? entry.effect : null;
    }
}
