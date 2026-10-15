// 플레이어가 말을 타고 내리는 상태를 처리하는 컴포넌트다. 이동·카메라·충돌은 기존 StarterAssets 컨트롤러가 그대로 맡고, 탑승 중에는 이동 속도와 점프 높이만 말의 값으로 바꾸며 말이 플레이어를 따라온다.
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class HorseRider : MonoBehaviour
{
    // 내릴 때 말의 오른쪽으로 이만큼 떨어진 곳에 선다.
    [SerializeField] private float dismountDistance = 1.7f;

    private CharacterController controller;
    private StarterAssets.ThirdPersonController movement;
    private Animator animator;
    private Health health;
    private LadderClimber climber;
    private PlayerFootsteps footsteps;

    // 안장 높이만큼 올릴 자식(카메라 루트, 캐릭터 모델, 뼈대)이다. 충돌 캡슐은 지면에 그대로 둔다.
    private Transform[] raised = System.Array.Empty<Transform>();
    private Mountable mount;
    private float savedMoveSpeed, savedSprintSpeed, savedJumpHeight;
    private float airSeconds;
    // 탄 직후 몸이 서 있는 자세로 가라앉을 때까지 기다린 뒤 애니메이션을 멈춘다.
    private float settleSeconds;

    public bool IsMounted => mount != null;
    public Mountable Mount => mount;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        movement = GetComponent<StarterAssets.ThirdPersonController>();
        animator = GetComponent<Animator>();
        health = GetComponent<Health>();
        climber = GetComponent<LadderClimber>();
        footsteps = GetComponent<PlayerFootsteps>();

        var list = new System.Collections.Generic.List<Transform>();
        if (movement != null && movement.CinemachineCameraTarget != null) list.Add(movement.CinemachineCameraTarget.transform);
        foreach (string name in new[] { "Geometry", "Skeleton" })
        {
            Transform child = transform.Find(name);
            if (child != null) list.Add(child);
        }
        raised = list.ToArray();
    }

    // 닿을 수 있는 거리이고 탈 수 있는 상태인지다. distance는 말 콜라이더까지의 거리다.
    public bool CanMount(Mountable candidate, out float distance)
    {
        distance = float.MaxValue;
        if (IsMounted || candidate == null || !candidate.CanBeMounted) return false;
        if (health != null && health.IsDead) return false;
        if (climber != null && climber.IsClimbing) return false;

        Collider collider = candidate.GetComponent<Collider>();
        Vector3 point = collider != null ? collider.ClosestPoint(transform.position) : candidate.transform.position;
        distance = Vector3.Distance(point, transform.position);
        return distance <= candidate.MountRange;
    }

    public bool TryMount(Mountable candidate)
    {
        if (!CanMount(candidate, out _)) return false;

        mount = candidate;
        mount.BeginRide();

        // 말 자리(지면)로 들어가 말이 보던 방향을 본다. 캡슐은 잠시 꺼서 위치를 옮긴다.
        controller.enabled = false;
        transform.SetPositionAndRotation(mount.transform.position + Vector3.up * 0.05f, Quaternion.Euler(0f, mount.transform.eulerAngles.y, 0f));
        controller.enabled = true;

        foreach (Transform t in raised) t.localPosition += Vector3.up * mount.SaddleHeight;

        savedMoveSpeed = movement.MoveSpeed;
        savedSprintSpeed = movement.SprintSpeed;
        savedJumpHeight = movement.JumpHeight;
        movement.MoveSpeed = mount.WalkSpeed;
        movement.SprintSpeed = mount.GallopSpeed;
        movement.JumpHeight = mount.JumpHeight;

        if (animator != null) animator.SetFloat("Speed", 0f);
        settleSeconds = 0.35f;
        if (footsteps != null) footsteps.enabled = false;
        airSeconds = 0f;

        health.Died += OnRiderDied;
        mount.GetComponent<Health>().Died += OnMountDied;
        return true;
    }

    private void LateUpdate()
    {
        if (!IsMounted) return;

        // 탄 채로 죽거나 말이 사라졌을 때를 놓치지 않도록 매 프레임 확인한다.
        if (mount == null || !mount.IsAlive || (health != null && health.IsDead))
        {
            Dismount();
            return;
        }

        airSeconds = movement.Grounded ? 0f : airSeconds + Time.deltaTime;
        Vector3 velocity = controller.velocity;
        velocity.y = 0f;
        HorseGait gait = HorseGaitRule.Choose(velocity.magnitude, airSeconds, mount.WalkSpeed, mount.GallopSpeed);
        mount.Follow(transform.position, transform.eulerAngles.y, gait);

        // 모델이 달리는 자세로 말 위에 있지 않도록 서 있는 자세로 멈춘다.
        if (settleSeconds > 0f)
        {
            settleSeconds -= Time.deltaTime;
            if (settleSeconds <= 0f && animator != null) animator.speed = 0f;
        }
    }

    public void Dismount()
    {
        if (!IsMounted) return;

        Mountable horse = mount;
        mount = null;
        health.Died -= OnRiderDied;
        if (horse != null) horse.GetComponent<Health>().Died -= OnMountDied;

        Vector3 landing = transform.position;
        if (horse != null)
        {
            landing = horse.transform.position + horse.transform.right * dismountDistance;
            if (Physics.Raycast(landing + Vector3.up * 3f, Vector3.down, out RaycastHit hit, 8f, ~0, QueryTriggerInteraction.Ignore)) landing = hit.point;
            else landing = GroundSnap.OnGround(landing);
        }

        foreach (Transform t in raised) t.localPosition -= Vector3.up * (horse != null ? horse.SaddleHeight : 0f);
        controller.enabled = false;
        transform.position = landing + Vector3.up * 0.05f;
        controller.enabled = true;

        movement.MoveSpeed = savedMoveSpeed;
        movement.SprintSpeed = savedSprintSpeed;
        movement.JumpHeight = savedJumpHeight;
        if (animator != null) animator.speed = 1f;
        if (footsteps != null) footsteps.enabled = true;
        if (horse != null) horse.EndRide();
    }

    private void OnRiderDied() => Dismount();
    private void OnMountDied() => Dismount();
}
