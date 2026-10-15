// 가만히 서 있는(대기) 좀비의 가슴과 어깨를 천천히 오르내려 숨쉬는 모습만 보여 주는 컴포넌트다. 대기 상태가 아니면 부드럽게 꺼진다. 표현만 담당한다.
using UnityEngine;

[RequireComponent(typeof(ZombieAI))]
[RequireComponent(typeof(Health))]
public class ZombieBreathing : MonoBehaviour
{
    [SerializeField] private Animator animator;
    // 숨 한 번(들이쉬고 내쉬는 한 주기)에 걸리는 시간(초)이다.
    [SerializeField] private float period = 3.6f;
    // 들이쉴 때 가슴을 뒤로 젖히는 최대 각도와 어깨가 오르내리는 최대 각도(도)다.
    [SerializeField] private float chestAngle = 2.2f;
    [SerializeField] private float shoulderAngle = 2.5f;
    // 호흡을 입힐 본 이름이다(제네릭 리그라 이름으로 찾는다).
    [SerializeField] private string chestBone = "Chest";
    [SerializeField] private string leftShoulderBone = "Shoulder.L";
    [SerializeField] private string rightShoulderBone = "Shoulder.R";
    // 켜지고 꺼질 때의 부드러운 전환 속도다(초당 가중치 변화량).
    [SerializeField] private float blendSpeed = 2.5f;

    private static readonly int IdleHash = Animator.StringToHash("Idle");

    private Health health;
    private Transform chest;
    private Transform leftShoulder;
    private Transform rightShoulder;
    private float weight;
    private float phase;

    private void Awake()
    {
        health = GetComponent<Health>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        // 여러 마리가 같은 박자로 숨쉬지 않게 시작 위상을 무작위로 둔다.
        phase = Random.value;
    }

    private void Start()
    {
        chest = FindBone(chestBone);
        leftShoulder = FindBone(leftShoulderBone);
        rightShoulder = FindBone(rightShoulderBone);
    }

    private Transform FindBone(string boneName)
    {
        foreach (Transform t in animator.GetComponentsInChildren<Transform>())
            if (t.name == boneName) return t;
        return null;
    }

    // 정지해 있는 대기 상태일 때만 숨쉰다(이동/공격/포효/사망 중에는 끈다).
    private bool IsStandingIdle()
    {
        if (health.IsDead || animator == null) return false;
        if (animator.IsInTransition(0)) return false;
        return animator.GetCurrentAnimatorStateInfo(0).shortNameHash == IdleHash;
    }

    // Animator가 이번 프레임 포즈를 쓴 뒤에 덧씌워야 하므로 LateUpdate에서 적용한다.
    private void LateUpdate()
    {
        weight = Mathf.MoveTowards(weight, IsStandingIdle() ? 1f : 0f, blendSpeed * Time.deltaTime);
        if (weight <= 0f) return;

        float breath = Mathf.Sin((Time.time / period + phase) * Mathf.PI * 2f) * weight;
        Vector3 right = transform.right;
        Vector3 forward = transform.forward;

        // 월드 회전을 몸 기준 축으로 곱해서 본의 로컬 축 방향과 무관하게 동작시킨다(가슴을 먼저 돌려야 어깨가 따라온다).
        if (chest != null) chest.rotation = Quaternion.AngleAxis(-breath * chestAngle, right) * chest.rotation;
        if (leftShoulder != null) leftShoulder.rotation = Quaternion.AngleAxis(breath * shoulderAngle, forward) * leftShoulder.rotation;
        if (rightShoulder != null) rightShoulder.rotation = Quaternion.AngleAxis(-breath * shoulderAngle, forward) * rightShoulder.rotation;
    }
}
