// 히트스캔 사격 한 발이 무엇에 맞는지(첫 대상, 머리 피격 배율)만 계산하는 정적 도우미다. 데미지는 적용하지 않는다. 싱글/호스트의 로컬 사격과 서버가 클라이언트 사격 요청을 처리할 때 같은 판정을 쓴다.
using UnityEngine;

public static class ShotResolver
{
    public struct Result
    {
        public bool didHit;
        public RaycastHit hit;
        // 맞은 대상의 Health(없으면 null, 벽 등)다.
        public Health health;
        public float multiplier;
        public bool headshot;
    }

    // ignore: 자기 자신의 Health(조준선이 자기 몸에 먼저 걸리는 것을 건너뛴다). skipPlayers: 플레이어(Player 태그)는 맞지 않게 한다(서버가 다른 사람의 사격을 처리할 때 호스트 플레이어를 맞추지 않도록).
    public static Result Trace(Ray ray, float range, LayerMask mask, Health ignore, bool skipPlayers = false)
    {
        // 접속자 아바타 몸은 총알을 막지 않는다(플레이어끼리는 맞지 않는 협동 규칙).
        RaycastHit[] hits = Physics.RaycastAll(ray, range, mask & AvatarBody.ExcludeMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            Health health = hit.collider.GetComponentInParent<Health>();
            if (health != null && health == ignore) continue;
            if (skipPlayers && health != null && health.CompareTag("Player")) continue;

            Result result = new Result { didHit = true, hit = hit, health = health, multiplier = 1f };
            if (health == null) return result;

            // 같은 대상의 머리 부위를 선이 지났으면 머리 피격이다(머리 콜라이더는 몸통 콜라이더 안에 있어 먼저 맞지 않는다).
            foreach (RaycastHit other in hits)
            {
                HitZone zone = other.collider.GetComponent<HitZone>();
                if (zone == null || other.collider.GetComponentInParent<Health>() != health) continue;
                if (zone.Multiplier > result.multiplier) result.multiplier = zone.Multiplier;
                result.headshot |= zone.IsHead;
            }
            return result;
        }
        return default;
    }
}
