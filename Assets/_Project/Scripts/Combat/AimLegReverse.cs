// 조준하며 뒤로 물러날 때 걷기 애니메이션을 거꾸로 재생해 발이 미끄러지지 않게 한다. 컨트롤러가 매 프레임 쓰는 MotionSpeed를 그 뒤에 음수로 덮는다.
using UnityEngine;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(WeaponEquipment))]
[RequireComponent(typeof(Animator))]
public class AimLegReverse : MonoBehaviour
{
    private static readonly int MotionSpeedHash = Animator.StringToHash("MotionSpeed");

    private WeaponEquipment equipment;
    private Animator animator;

    private void Awake()
    {
        equipment = GetComponent<WeaponEquipment>();
        animator = GetComponent<Animator>();
    }

    // Animator는 Update가 끝난 뒤 평가되므로 컨트롤러 Update 뒤(실행 순서 100)에서 값을 바꾸면 이번 프레임 재생에 반영된다.
    private void Update()
    {
        if (!equipment.ReverseLegs) return;
        animator.SetFloat(MotionSpeedHash, -Mathf.Abs(animator.GetFloat(MotionSpeedHash)));
    }
}
