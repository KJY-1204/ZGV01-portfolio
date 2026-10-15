// 발밑 바닥 재질을 판별하고 발소리(기본 걸음 + 재질 겹침)를 재생하는 정적 도우미다. 플레이어와 좀비가 공통으로 쓴다.
using System;
using UnityEngine;

public static class FootstepSounds
{
    private const float RayStartHeight = 0.5f;
    private const float RayLength = 1.5f;

    // 발밑을 아래로 쏴서 처음 닿는 바닥의 재질을 구한다. 자기 자신의 콜라이더는 건너뛴다.
    // 표면 표식이 없는 바닥(지형 등)은 세트의 기본 재질이다.
    public static ImpactSurface SurfaceUnder(Vector3 position, Transform ignoreRoot, FootstepSoundSet set)
    {
        Ray ray = new Ray(position + Vector3.up * RayStartHeight, Vector3.down);
        RaycastHit[] hits = Physics.RaycastAll(ray, RayLength, ~0, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (ignoreRoot != null && hit.collider.transform.IsChildOf(ignoreRoot)) continue;

            ImpactSurfaceMarker marker = hit.collider.GetComponentInParent<ImpactSurfaceMarker>();
            return marker != null ? marker.Surface : set.DefaultSurface;
        }
        return set.DefaultSurface;
    }

    // 기본 걸음 소리는 재질에 맞춰 음량/피치를 바꿔서, 겹침 소리는 별도 소스로 같이 낸다.
    public static void Play(AudioSource stepSource, AudioSource layerSource, FootstepSoundSet set, ImpactSurface surface, float volume)
    {
        FootstepSoundSet.SurfaceLayer entry = set.GetSurface(surface);
        float volumeScale = entry != null ? entry.stepVolumeScale : 1f;
        float pitchScale = entry != null ? entry.stepPitchScale : 1f;

        SoundGroup steps = set.Steps;
        AudioClip stepClip = steps.PickClip();
        if (stepClip != null)
        {
            stepSource.pitch = steps.PickPitch() * pitchScale;
            stepSource.PlayOneShot(stepClip, volume * steps.volume * volumeScale * AudioBus.Volume(AudioCategory.Footsteps));
        }

        PlayLayer(layerSource, set, surface, volume);
    }

    // 바닥 재질 겹침 소리만 낸다(착지처럼 기본 걸음 소리가 필요 없는 때에도 쓴다).
    public static void PlayLayer(AudioSource layerSource, FootstepSoundSet set, ImpactSurface surface, float volume)
    {
        FootstepSoundSet.SurfaceLayer entry = set.GetSurface(surface);
        if (entry == null || !entry.layer.HasClips) return;

        AudioClip layerClip = entry.layer.PickClip();
        layerSource.pitch = entry.layer.PickPitch();
        layerSource.PlayOneShot(layerClip, volume * entry.layer.volume * AudioBus.Volume(AudioCategory.Footsteps));
    }
}
