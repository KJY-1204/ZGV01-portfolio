// 좀비의 원거리 공격을 담당한다. 활 쏘기 모션의 시위를 놓는 순간에 맞춰 화살을 만들어 목표 쪽으로 날린다.
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Health))]
public class ZombieRangedAttack : MonoBehaviour
{
    [SerializeField] private ZombieProjectile projectilePrefab;
    // 화살이 나가는 위치다(활을 든 손 쪽). 비워 두면 좀비 가슴 높이에서 나간다.
    [SerializeField] private Transform muzzle;
    // 공격 모션이 시작되고 시위를 놓기까지의 시간이다.
    [SerializeField] private float releaseDelay = 0.35f;
    [SerializeField] private float projectileSpeed = 28f;
    // 목표 몸통을 향해 조준하는 높이와 흩어지는 각도(도)다.
    [SerializeField] private float aimHeight = 1.2f;
    [SerializeField] private float spreadAngle = 1f;
    [SerializeField] private SoundGroup releaseSounds = new SoundGroup();
    [SerializeField] private float minDistance = 3f;
    [SerializeField] private float maxDistance = 30f;

    // 조준하는 동안 목표가 너무 가까우면 뒤로 물러나고, 아니면 좌우로 이동한다.
    [Header("Positioning")]
    [SerializeField] private float retreatDistance = 6f;
    [SerializeField] private float moveSpeed = 1.3f;
    [SerializeField] private Vector2 strafeSwitchInterval = new Vector2(1.5f, 3f);
    // 발사 직후 이 시간 동안은 제자리에서 쏘고 장전한다.
    [SerializeField] private float holdAfterFire = 1.7f;

    private Health health;
    private ZombieAI ai;
    private NavMeshAgent agent;
    private AudioSource voice;
    private bool dead;
    private float lastFireTime = -100f;
    private float nextStrafeSwitch;
    private float strafeSign = 1f;

    // 서버가 화살을 날릴 때 시작 위치와 속도를 알린다(멀티플레이 참가자 화면에 같은 화살을 보여 주는 데 쓴다).
    public event System.Action<Vector3, Vector3> ArrowFired;

    private void Awake()
    {
        health = GetComponent<Health>();
        ai = GetComponent<ZombieAI>();
        agent = GetComponent<NavMeshAgent>();
        health.Died += OnDied;

        voice = gameObject.AddComponent<AudioSource>();
        voice.playOnAwake = false;
        voice.spatialBlend = 1f;
        voice.rolloffMode = AudioRolloffMode.Linear;
        voice.minDistance = minDistance;
        voice.maxDistance = maxDistance;
    }

    private void OnDestroy()
    {
        if (health != null) health.Died -= OnDied;
    }

    private void OnDied()
    {
        dead = true;
    }

    public void Fire(Transform target, float damage)
    {
        if (dead) return;
        lastFireTime = Time.time;
        StartCoroutine(Release(target, damage));
    }

    // 공격 상태에서 매 프레임 불린다. 목표가 가까우면 뒤로 물러나고, 아니면 좌우로 움직인다(발사 직후에는 멈춘다).
    public void Reposition(Transform target)
    {
        if (dead || !agent.enabled || !agent.isOnNavMesh || Time.time < lastFireTime + holdAfterFire) return;

        Vector3 away = transform.position - target.position;
        away.y = 0f;
        Vector3 step;
        if (away.magnitude < retreatDistance)
        {
            step = away.normalized;
        }
        else
        {
            if (Time.time >= nextStrafeSwitch)
            {
                strafeSign = -strafeSign;
                nextStrafeSwitch = Time.time + Random.Range(strafeSwitchInterval.x, strafeSwitchInterval.y);
            }
            step = transform.right * strafeSign;
        }
        agent.Move(step * (moveSpeed * Time.deltaTime));
    }

    private IEnumerator Release(Transform target, float damage)
    {
        yield return new WaitForSeconds(releaseDelay);
        // 시위를 놓는 순간 목표가 가려졌으면 쏘지 않는다.
        if (dead || target == null || projectilePrefab == null || (ai != null && !ai.CanSee(target, false))) yield break;

        Vector3 origin = muzzle != null ? muzzle.position : transform.position + Vector3.up * aimHeight;
        Vector3 aimPoint = target.position + Vector3.up * aimHeight;
        Vector3 direction = (aimPoint - origin).normalized;
        direction = Quaternion.Euler(Random.Range(-spreadAngle, spreadAngle), Random.Range(-spreadAngle, spreadAngle), 0f) * direction;

        ZombieProjectile arrow = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(direction));
        arrow.Launch(direction * projectileSpeed, damage, transform);
        ArrowFired?.Invoke(origin, direction * projectileSpeed);
        PlayReleaseSound();
    }

    // 멀티플레이 참가자 쪽: 서버가 쏜 화살의 모습과 활 소리만 만든다(꼭두각시라 이 컴포넌트는 꺼져 있어도 호출할 수 있다).
    public void SpawnVisualArrow(Vector3 origin, Vector3 velocity)
    {
        if (projectilePrefab == null || velocity.sqrMagnitude < 0.01f) return;
        ZombieProjectile arrow = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(velocity));
        arrow.LaunchVisual(velocity, transform);
        PlayReleaseSound();
    }

    private void PlayReleaseSound()
    {
        AudioClip clip = releaseSounds.PickClip();
        if (clip == null) return;
        voice.pitch = releaseSounds.PickPitch();
        voice.PlayOneShot(clip, releaseSounds.volume * AudioBus.Volume(AudioCategory.Effects));
    }
}
