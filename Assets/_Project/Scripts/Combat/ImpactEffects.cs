// 맞은 콜라이더의 표면 종류를 판별해 탄착 이펙트와 맞는 소리를 내는 정적 도우미다. 표현만 담당하고 게임 상태를 바꾸지 않는다.
using UnityEngine;

public static class ImpactEffects
{
    // 체력이 있는 대상은 살점, 표면 표식이 있으면 그 표면, 아니면 라이브러리의 기본 표면이다.
    public static ImpactSurface SurfaceOf(Collider collider, ImpactEffectLibrary library)
    {
        if (collider.GetComponentInParent<Health>() != null) return ImpactSurface.SoftBody;

        ImpactSurfaceMarker marker = collider.GetComponentInParent<ImpactSurfaceMarker>();
        if (marker != null) return marker.Surface;

        return library.DefaultSurface;
    }

    // 이펙트는 표면에서 살짝 띄워(z파이팅 방지) 법선 방향으로 세운다. 파티클과 소리는 풀에서 꺼내 쓴다.
    // playSound가 false면 소리는 내지 않는다(펀치처럼 호출한 쪽에서 전용 소리를 내는 경우).
    public static void Spawn(Collider collider, Vector3 point, Vector3 normal, bool playSound = true)
    {
        ImpactEffectLibrary library = ImpactEffectLibrary.Instance;
        if (library == null || collider == null) return;

        SpawnSurface(SurfaceOf(collider, library), point, normal, playSound);
    }

    // 표면 종류를 이미 아는 경우(멀티플레이로 받은 사격 효과)의 이펙트와 소리다.
    public static void SpawnSurface(ImpactSurface surface, Vector3 point, Vector3 normal, bool playSound = true)
    {
        ImpactEffectLibrary library = ImpactEffectLibrary.Instance;
        if (library == null) return;

        ImpactEffectLibrary.Entry entry = library.GetEntry(surface);
        if (entry == null) return;

        if (entry.effect != null)
            EffectPool.Spawn(entry.effect, point + normal * 0.01f, Quaternion.FromToRotation(Vector3.forward, normal));

        if (playSound && entry.sounds != null && entry.sounds.Length > 0)
        {
            AudioClip clip = entry.sounds[Random.Range(0, entry.sounds.Length)];
            EffectPool.PlaySoundAt(clip, point, entry.soundVolume, entry.soundPitch, entry.soundStart, entry.soundDuration);
        }
    }
}
