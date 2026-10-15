// 좀비가 쏜 화살처럼 포물선으로 날아가 처음 맞은 대상에게 피해를 주고 그 자리에 박히는 발사체다.
using UnityEngine;

public class ZombieProjectile : MonoBehaviour
{
    [SerializeField] private float gravity = 6f;
    [SerializeField] private float lifetime = 6f;
    [SerializeField] private float stuckLifetime = 8f;

    private Vector3 velocity;
    private float damage;
    private Transform owner;
    private float age;
    private bool stuck;
    // 멀티플레이 참가자 화면용 화살이다. 피해 없이 날아가 박히기만 한다(판정과 피해는 서버의 화살이 한다).
    private bool visualOnly;

    // 이 컴퓨터에서 만든 보기 전용 화살 수다(멀티플레이 시험 로그용).
    public static int VisualSpawned { get; private set; }

    public void Launch(Vector3 initialVelocity, float hitDamage, Transform shooter)
    {
        velocity = initialVelocity;
        damage = hitDamage;
        owner = shooter;
        transform.rotation = Quaternion.LookRotation(velocity);
    }

    // 참가자 쪽에서 서버가 쏜 화살을 모습만 따라 날린다.
    public void LaunchVisual(Vector3 initialVelocity, Transform shooter)
    {
        visualOnly = true;
        VisualSpawned++;
        Launch(initialVelocity, 0f, shooter);
    }

    private void Update()
    {
        if (stuck) return;

        age += Time.deltaTime;
        if (age >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        velocity += Vector3.down * (gravity * Time.deltaTime);
        Vector3 step = velocity * Time.deltaTime;

        if (TryHit(step, out RaycastHit hit))
        {
            transform.position = hit.point;
            // 맞은 표면(살, 흙, 나무 등)에 맞는 탄착 이펙트와 소리를 낸다.
            Vector3 normal = hit.normal.sqrMagnitude > 0.01f ? hit.normal : -velocity.normalized;
            ImpactEffects.Spawn(hit.collider, hit.point, normal);
            if (!visualOnly) ApplyHitDamage(hit.collider);
            Stick(hit.collider.transform);
            return;
        }

        transform.position += step;
        transform.rotation = Quaternion.LookRotation(velocity);
    }

    // 맞은 대상에게 피해를 준다. 호스트 플레이어와 좀비 등은 로컬 Health에, 멀티플레이 접속자 아바타는 소유자에게 전달한다(이미 죽은 아바타는 피해 없이 박힌다).
    private void ApplyHitDamage(Collider collider)
    {
        NetworkPlayerAvatar avatar = collider.GetComponentInParent<NetworkPlayerAvatar>();
        if (avatar != null)
        {
            NetworkCombat combat = avatar.GetComponent<NetworkCombat>();
            if (combat != null && combat.IsDead) return;
            PlayerTargets.Damage(avatar.transform, damage, velocity.normalized);
            return;
        }

        Health health = collider.GetComponentInParent<Health>();
        if (health != null) health.ApplyDamage(damage);
    }

    // 쏜 좀비와 다른 좀비는 통과하고, 그 밖의 첫 충돌체(접속자 아바타 포함)를 맞은 것으로 본다.
    private bool TryHit(Vector3 step, out RaycastHit result)
    {
        RaycastHit[] hits = Physics.RaycastAll(transform.position, step.normalized, step.magnitude, ~0, QueryTriggerInteraction.Ignore);
        float nearest = float.MaxValue;
        result = default;
        bool found = false;

        foreach (RaycastHit hit in hits)
        {
            if (hit.distance >= nearest) continue;
            if (owner != null && hit.collider.transform.IsChildOf(owner)) continue;
            if (hit.collider.GetComponentInParent<ZombieAI>() != null) continue;
            nearest = hit.distance;
            result = hit;
            found = true;
        }
        return found;
    }

    private void Stick(Transform surface)
    {
        stuck = true;
        // 박힌 화살이 움직이는 대상에 붙어 끌려가며 궤적선을 그리지 않도록 끈다.
        TrailRenderer trail = GetComponent<TrailRenderer>();
        if (trail != null)
        {
            trail.emitting = false;
            trail.Clear();
        }
        transform.SetParent(surface, true);
        Destroy(gameObject, stuckLifetime);
    }
}
