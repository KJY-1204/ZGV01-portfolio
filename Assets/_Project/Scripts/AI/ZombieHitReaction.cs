// 좀비가 맞았을 때 모델이 맞은 방향으로 젖혀졌다가 돌아오는 표현 컴포넌트다. AI나 피해에는 관여하지 않는다.
using UnityEngine;

[RequireComponent(typeof(Health))]
public class ZombieHitReaction : MonoBehaviour
{
    [SerializeField] private Animator animator;
    // 최대 체력 대비 비율에 곱해 젖히는 각도(도)를 정한다. 머리에 맞으면 더 젖힌다.
    [SerializeField] private float leanDegrees = 22f;
    [SerializeField] private float maxLean = 20f;
    [SerializeField] private float headshotBoost = 1.5f;
    [SerializeField] private float recoverySeconds = 0.4f;

    private Health health;
    private Transform model;
    private Quaternion baseLocalRotation;
    private Vector3 leanAxis;
    private float peakAngle;
    private float elapsed = -1f;

    private void Awake()
    {
        health = GetComponent<Health>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            model = animator.transform;
            baseLocalRotation = model.localRotation;
        }

        health.HitReceived += OnHit;
        health.Died += OnDied;
    }

    private void OnDestroy()
    {
        if (health == null) return;
        health.HitReceived -= OnHit;
        health.Died -= OnDied;
    }

    private void OnHit(float amount, Vector3 direction, bool headshot)
    {
        if (model == null) return;

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) direction = -transform.forward;

        float fraction = amount / Mathf.Max(1f, health.MaxHealth);
        peakAngle = Mathf.Min(maxLean, leanDegrees * Mathf.Clamp01(fraction * 4f)) * (headshot ? headshotBoost : 1f);
        // 위(+Y)가 맞은 방향으로 기울어지는 회전축이다.
        leanAxis = Vector3.Cross(Vector3.up, direction.normalized);
        elapsed = 0f;
    }

    // 사망하면 쓰러지는 연출(ZombieAnimator)이 모델을 직접 돌리므로 겹치지 않게 멈춘다.
    private void OnDied()
    {
        elapsed = -1f;
        enabled = false;
    }

    // Animator 포즈가 정해진 뒤에 모델 전체의 회전을 덧씌운다. 기준 회전에서 매번 새로 계산해서 누적되지 않는다.
    private void LateUpdate()
    {
        if (model == null || elapsed < 0f) return;

        elapsed += Time.deltaTime;
        float t = elapsed / recoverySeconds;
        Quaternion baseWorld = model.parent != null ? model.parent.rotation * baseLocalRotation : baseLocalRotation;
        if (t >= 1f)
        {
            model.rotation = baseWorld;
            elapsed = -1f;
            return;
        }

        // 빠르게 젖혔다가(앞 20%) 천천히 돌아온다.
        float curve = t < 0.2f ? t / 0.2f : 1f - Mathf.SmoothStep(0f, 1f, (t - 0.2f) / 0.8f);
        model.rotation = Quaternion.AngleAxis(peakAngle * curve, leanAxis) * baseWorld;
    }
}
