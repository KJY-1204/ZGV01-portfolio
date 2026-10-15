// 공격 클립이 없는 휴머노이드 좀비를 위해 공격 순간에 양팔을 앞으로 휘두르는 동작을 뼈에 직접 덧씌우는 컴포넌트다. 표현만 담당하고 피해/AI는 건드리지 않는다.
using UnityEngine;

[RequireComponent(typeof(ZombieAI))]
[RequireComponent(typeof(Health))]
public class ZombieStrikeAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float duration = 0.6f;
    // 어깨를 앞으로 들어 올리는 각도(도)의 시간 곡선(0~1)이다. 머리 위로 크게 들었다가(0.3) 앞으로 내려치며 팔이 수평이 되는 순간(0.55)이 타격 순간이다.
    // 0.55 * duration(0.6) = 0.33초가 ZombieAI의 attackHitDelays(0.33)와 맞는다. 둘 중 하나를 바꾸면 같이 맞춘다.
    [SerializeField] private AnimationCurve shoulderRaise = new AnimationCurve(
        new Keyframe(0f, 0f), new Keyframe(0.3f, 120f), new Keyframe(0.55f, 88f), new Keyframe(0.8f, 50f), new Keyframe(1f, 0f));
    // 팔꿈치를 굽히는 각도(도)의 곡선이다. 들어 올릴 때 굽혔다가 타격 순간에 쭉 편다(손이 가장 멀리 나간다).
    [SerializeField] private AnimationCurve elbowBend = new AnimationCurve(
        new Keyframe(0f, 0f), new Keyframe(0.3f, 35f), new Keyframe(0.55f, 0f), new Keyframe(0.8f, 25f), new Keyframe(1f, 0f));
    // 상체를 앞으로 숙이는 각도(도)의 곡선이다. 타격 순간에 가장 숙인다.
    [SerializeField] private AnimationCurve torsoLean = new AnimationCurve(
        new Keyframe(0f, 0f), new Keyframe(0.3f, 6f), new Keyframe(0.55f, 22f), new Keyframe(1f, 0f));

    private ZombieAI ai;
    private Health health;
    private Transform chest;
    private Transform leftUpper;
    private Transform leftLower;
    private Transform rightUpper;
    private Transform rightLower;
    private float elapsed = -1f;

    private void Awake()
    {
        ai = GetComponent<ZombieAI>();
        health = GetComponent<Health>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        ai.Attacked += OnAttacked;
        health.Died += OnDied;
    }

    private void OnDestroy()
    {
        if (ai != null) ai.Attacked -= OnAttacked;
        if (health != null) health.Died -= OnDied;
    }

    private void Start()
    {
        chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        if (chest == null) chest = animator.GetBoneTransform(HumanBodyBones.Spine);
        leftUpper = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        leftLower = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        rightUpper = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        rightLower = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
    }

    private void OnAttacked()
    {
        elapsed = 0f;
    }

    // 사망하면 쓰러지는 연출과 겹치지 않게 멈춘다.
    private void OnDied()
    {
        elapsed = -1f;
        enabled = false;
    }

    // Animator가 이번 프레임 포즈를 쓴 뒤에 덧씌워야 하므로 LateUpdate에서 적용한다.
    private void LateUpdate()
    {
        if (elapsed < 0f) return;

        elapsed += Time.deltaTime;
        float t = elapsed / duration;
        if (t >= 1f)
        {
            elapsed = -1f;
            return;
        }

        Vector3 right = transform.right;
        if (chest != null) chest.rotation = Quaternion.AngleAxis(torsoLean.Evaluate(t), right) * chest.rotation;

        float raise = shoulderRaise.Evaluate(t);
        float bend = elbowBend.Evaluate(t);
        Raise(leftUpper, leftLower, raise, bend, right);
        Raise(rightUpper, rightLower, raise, bend, right);
    }

    // 월드 회전을 몸 기준 축으로 곱해서 본의 로컬 축 방향과 무관하게 동작시킨다(부모 어깨를 먼저 돌려야 팔꿈치가 따라온다).
    private static void Raise(Transform upper, Transform lower, float raise, float bend, Vector3 right)
    {
        if (upper != null) upper.rotation = Quaternion.AngleAxis(-raise, right) * upper.rotation;
        if (lower != null) lower.rotation = Quaternion.AngleAxis(-bend, right) * lower.rotation;
    }
}
