// 발소리 한 종류(기본 걸음 소리 + 바닥 재질별 겹침 소리)를 데이터로 관리하는 에셋이다. 플레이어와 좀비가 각자 다른 세트를 쓴다.
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "FootstepSoundSet", menuName = "ZGV01/Audio/Footstep Sound Set")]
public class FootstepSoundSet : ScriptableObject
{
    [Serializable]
    public class SurfaceLayer
    {
        public ImpactSurface surface;
        // 기본 걸음 소리 위에 겹쳐 재질감을 더하는 소리다.
        public SoundGroup layer = new SoundGroup();
        // 이 재질에서 기본 걸음 소리의 음량/피치를 얼마나 바꿀지(부드러운 바닥은 작고 낮게, 단단한 바닥은 크고 높게).
        [Range(0f, 2f)] public float stepVolumeScale = 1f;
        public float stepPitchScale = 1f;
    }

    [SerializeField] private SoundGroup steps = new SoundGroup();
    [SerializeField] private SurfaceLayer[] surfaces = new SurfaceLayer[0];
    // 바닥에 표면 표식이 없을 때 쓰는 재질이다(대부분의 지면이 흙이라 흙을 기본으로 한다).
    [SerializeField] private ImpactSurface defaultSurface = ImpactSurface.Dirt;

    public SoundGroup Steps => steps;
    public ImpactSurface DefaultSurface => defaultSurface;

    public SurfaceLayer GetSurface(ImpactSurface surface)
    {
        foreach (SurfaceLayer entry in surfaces)
            if (entry != null && entry.surface == surface) return entry;
        return null;
    }
}
