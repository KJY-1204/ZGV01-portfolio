// 좀비의 이동 속도와 상태 이벤트(발견, 공격, 사망)를 Animator 파라미터로 옮기는 컴포넌트다. AI 상태를 바꾸지 않고 표현만 담당한다.
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(ZombieAI))]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))]
public class ZombieAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float speedDamp = 0.1f;
    // 사망 애니메이션이 없는 모델은 모델 루트를 뒤로 눕혀서 쓰러뜨린다.
    [SerializeField] private bool tipOverOnDeath;
    [SerializeField] private float tipOverSeconds = 0.7f;
    // 쓰러질 때 맞은 방향으로 미끄러지는 거리(m)다.
    [SerializeField] private float deathSlide = 0.5f;
    // 시체가 이 시간 뒤부터 바닥으로 가라앉아 사라진다(스포너가 치우기 전에 눈에 띄지 않게).
    [SerializeField] private float sinkDelay = 12f;
    [SerializeField] private float sinkSeconds = 3f;
    [SerializeField] private float sinkDepth = 1.4f;

    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveZHash = Animator.StringToHash("MoveZ");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AlertHash = Animator.StringToHash("Alert");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int AttackTypeHash = Animator.StringToHash("AttackType");
    private static readonly int DeadHash = Animator.StringToHash("Dead");

    private ZombieAI ai;
    private NavMeshAgent agent;
    private Health health;
    private Vector3 lastPosition;
    private float tipOverTimer = -1f;
    private Quaternion tipOverStart;
    private Quaternion modelBaseLocalRotation;
    private Vector3 deathDirection;
    private Vector3 deathPosition;
    private float deathTime = -1f;

    private void Awake()
    {
        ai = GetComponent<ZombieAI>();
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null) modelBaseLocalRotation = animator.transform.localRotation;

        ai.Alerted += OnAlerted;
        ai.Attacked += OnAttacked;
        health.Died += OnDied;
    }

    private void OnDestroy()
    {
        if (ai != null)
        {
            ai.Alerted -= OnAlerted;
            ai.Attacked -= OnAttacked;
        }
        if (health != null) health.Died -= OnDied;
    }

    private void Start()
    {
        lastPosition = transform.position;
    }

    private void Update()
    {
        if (animator == null) return;

        if (deathTime >= 0f)
        {
            UpdateCorpse();
            return;
        }

        // 에이전트 속도는 agent.Move(좌우/후퇴 이동)를 반영하지 않으므로 실제 이동량으로 속도를 구한다.
        Vector3 velocity = Time.deltaTime > 0f ? (transform.position - lastPosition) / Time.deltaTime : Vector3.zero;
        lastPosition = transform.position;
        velocity.y = 0f;
        Vector3 local = transform.InverseTransformDirection(velocity);
        animator.SetFloat(SpeedHash, velocity.magnitude, speedDamp, Time.deltaTime);
        animator.SetFloat(MoveXHash, local.x, speedDamp, Time.deltaTime);
        animator.SetFloat(MoveZHash, local.z, speedDamp, Time.deltaTime);
    }

    private void OnAlerted()
    {
        if (animator != null) animator.SetTrigger(AlertHash);
    }

    private void OnAttacked()
    {
        if (animator == null) return;
        animator.SetInteger(AttackTypeHash, ai.LastAttackVariant);
        animator.SetTrigger(AttackHash);
    }

    private void OnDied()
    {
        if (animator == null) return;

        // 맞은 방향(없으면 뒤쪽)으로 쓰러진다.
        deathDirection = health.LastHitDirection;
        deathDirection.y = 0f;
        deathDirection = deathDirection.sqrMagnitude > 0.001f ? deathDirection.normalized : -transform.forward;
        deathPosition = transform.position;
        deathTime = Time.time;

        SpawnDeathBlood();

        if (tipOverOnDeath)
        {
            animator.speed = 0f;
            // 피격 반응으로 기울어 있던 자세가 아니라 원래 자세를 기준으로 쓰러뜨린다.
            Transform parent = animator.transform.parent;
            tipOverStart = (parent != null ? parent.rotation : Quaternion.identity) * modelBaseLocalRotation;
            tipOverTimer = 0f;
        }
        else
        {
            animator.SetTrigger(DeadHash);
        }
    }

    // 시체 연출: 쓰러뜨리기(애니메이션이 없는 모델), 맞은 방향으로 미끄러지기, 오래 지나면 가라앉기.
    private void UpdateCorpse()
    {
        float age = Time.time - deathTime;

        if (tipOverTimer >= 0f)
        {
            tipOverTimer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, tipOverTimer / tipOverSeconds);
            // 위(+Y)가 맞은 방향으로 기울도록 Cross(up, 방향)을 축으로 90도 돌린다.
            Vector3 axis = Vector3.Cross(Vector3.up, deathDirection);
            animator.transform.rotation = Quaternion.AngleAxis(90f * t, axis) * tipOverStart;
            transform.position = deathPosition + deathDirection * (deathSlide * t) + SinkOffset(age);
            return;
        }

        transform.position = deathPosition + deathDirection * (deathSlide * Mathf.Clamp01(age / 0.5f) * 0.4f) + SinkOffset(age);
    }

    private bool? hasShaderFx;
    private bool HasShaderFx => hasShaderFx ?? (hasShaderFx = GetComponent<ZombieShaderFX>() != null).Value;

    private Vector3 SinkOffset(float age)
    {
        // 사라지는 셰이더 연출(ZombieShaderFX)이 있으면 땅속으로 가라앉지 않고 그 자리에서 재처럼 사라진다.
        if (HasShaderFx) return Vector3.zero;

        float t = Mathf.Clamp01((age - sinkDelay) / Mathf.Max(0.1f, sinkSeconds));
        return Vector3.down * (sinkDepth * Mathf.SmoothStep(0f, 1f, t));
    }

    // 죽은 자리에서 피가 튀는 효과를 낸다(머리 피격이면 머리 높이, 아니면 가슴 높이).
    private void SpawnDeathBlood()
    {
        ImpactEffectLibrary library = ImpactEffectLibrary.Instance;
        GameObject effect = library != null ? library.GetEffect(ImpactSurface.SoftBody) : null;
        if (effect == null) return;

        float height = health.LastHitWasHeadshot ? 1.6f : 1.1f;
        Vector3 point = transform.position + Vector3.up * height;
        // 맞은 쪽(공격하는 쪽)을 향해 튀게 한다.
        EffectPool.Spawn(effect, point, Quaternion.LookRotation(-deathDirection, Vector3.up));
    }
}
