// 사다리 오르기 상태를 처리하는 플레이어 컴포넌트다. Interact로 사다리를 잡고, 이동 입력 앞/뒤로 오르내리며, 점프로 놓는다.
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class LadderClimber : MonoBehaviour
{
    [SerializeField] private float grabRange = 1.3f;
    [SerializeField] private float climbSpeed = 1.6f;

    private CharacterController controller;
    private StarterAssets.ThirdPersonController movement;
    private StarterAssets.StarterAssetsInputs inputs;
    private Animator animator;
    private Health health;
    private BuildableLadder ladder;
    private float climbHeight;

    public bool IsClimbing => ladder != null;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        movement = GetComponent<StarterAssets.ThirdPersonController>();
        inputs = GetComponent<StarterAssets.StarterAssetsInputs>();
        animator = GetComponent<Animator>();
        health = GetComponent<Health>();
    }

    // 오르는 중에 E를 누르면 사다리를 놓는다. 잡는 것은 상호작용 안내(InteractionFocus)가 대상을 골라 TryGrab을 부른다.
    private void OnInteract(InputValue value)
    {
        if (value.isPressed && IsClimbing) Release(transform.position);
    }

    // 오르는 중이면 사다리를 놓고 지금 자리에 선다(Esc 해제용).
    public bool ReleaseHere()
    {
        if (!IsClimbing) return false;
        Release(transform.position);
        return true;
    }

    // 사다리 앞쪽(벽 반대편) 가까이에 있으면 잡을 수 있다. distance는 사다리 앞면까지의 거리다.
    public bool CanGrab(BuildableLadder candidate, out float distance)
    {
        distance = float.MaxValue;
        if (IsClimbing || (health != null && health.IsDead)) return false;

        Vector3 local = candidate.transform.InverseTransformPoint(transform.position);
        if (Mathf.Abs(local.x) > 0.6f || local.y < -0.3f || local.y > candidate.Height) return false;
        // 로컬 -Z가 오르는 사람이 서는 쪽이다.
        if (local.z > 0.2f || local.z < -grabRange) return false;

        distance = Mathf.Abs(local.z);
        return true;
    }

    public bool TryGrab(BuildableLadder target)
    {
        if (target == null || !CanGrab(target, out _)) return false;

        ladder = target;
        // 지면 높이(0)에서 잡으면 첫 프레임에 "맨 아래까지 내려옴"으로 판정돼 바로 놓아지므로 살짝 위에서 시작한다.
        climbHeight = Mathf.Clamp(ladder.transform.InverseTransformPoint(transform.position).y, 0.1f, ladder.ExitHeight);
        movement.enabled = false;
        controller.enabled = false;
        inputs.JumpInput(false);
        FaceLadder();
        transform.position = ladder.PointAt(climbHeight);
        if (animator != null) animator.SetFloat("Speed", 0f);
        return true;
    }

    private void Update()
    {
        if (!IsClimbing) return;

        if (health != null && health.IsDead)
        {
            Release(transform.position);
            return;
        }

        // 점프를 누르면 사다리에서 뒤로 떨어진다.
        if (inputs.jump)
        {
            inputs.JumpInput(false);
            Release(ladder.PointAt(climbHeight) - ladder.transform.forward * 0.35f);
            return;
        }

        climbHeight += inputs.move.y * climbSpeed * Time.deltaTime;

        if (climbHeight <= 0f)
        {
            Release(ladder.BottomExitPosition());
            return;
        }
        if (climbHeight >= ladder.ExitHeight)
        {
            Release(ladder.TopExitPosition());
            return;
        }

        transform.position = ladder.PointAt(climbHeight);
        FaceLadder();
        if (animator != null) animator.SetFloat("Speed", 0f);
    }

    private void FaceLadder()
    {
        Vector3 forward = ladder.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(forward);
    }

    private void Release(Vector3 position)
    {
        ladder = null;
        transform.position = position;
        controller.enabled = true;
        movement.enabled = true;
    }
}
