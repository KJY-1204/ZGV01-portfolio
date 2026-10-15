// 플레이어가 탈 수 있는 동물(말)에 붙는 컴포넌트다. 안장 높이와 속도, 탑승 중의 걸음걸이 애니메이션과 발소리 동작, 탑승 중 콜라이더 끄기를 맡는다. 이동은 플레이어가 하고 말은 따라온다.
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AnimalAI))]
[RequireComponent(typeof(Health))]
public class Mountable : MonoBehaviour
{
    // 안장 높이(m)다. 탄 플레이어의 모델과 카메라를 이만큼 올린다. 서 있는 자세의 다리가 말 몸통에 가려져 앉은 것처럼 보이는 높이로 잡았다.
    [SerializeField] private float saddleHeight = 0.95f;
    [SerializeField] private float walkSpeed = 4.5f;
    [SerializeField] private float gallopSpeed = 11f;
    [SerializeField] private float jumpHeight = 1.4f;
    // 이 거리 안에서 탈 수 있다.
    [SerializeField] private float mountRange = 2.8f;
    // 애니메이터의 점프 상태 이름이다(걷기/달리기/대기는 AnimalDefinition의 이름을 쓴다).
    [SerializeField] private string jumpState = "jump";
    [SerializeField] private float animationBlendSeconds = 0.2f;

    private static readonly List<Mountable> active = new List<Mountable>();
    public static IReadOnlyList<Mountable> Active => active;

    private AnimalAI animal;
    private Health health;
    private Animator animator;
    private Collider[] colliders;
    private string currentAnimation;

    public float SaddleHeight => saddleHeight;
    public float WalkSpeed => walkSpeed;
    public float GallopSpeed => gallopSpeed;
    public float JumpHeight => jumpHeight;
    public float MountRange => mountRange;
    public bool IsRidden => animal != null && animal.IsRidden;
    public bool IsAlive => health != null && !health.IsDead;
    public string DisplayName => animal != null && animal.Definition != null ? animal.Definition.DisplayName : "말";

    private void Awake()
    {
        animal = GetComponent<AnimalAI>();
        health = GetComponent<Health>();
        animator = GetComponentInChildren<Animator>();
        colliders = GetComponents<Collider>();
    }

    private void OnEnable() => active.Add(this);
    private void OnDisable() => active.Remove(this);

    // 탈 수 있는 상태인지다(살아 있고 아무도 타고 있지 않다). 멀티플레이 참가자 쪽 말(꼭두각시)은 아직 탈 수 없다.
    public bool CanBeMounted => IsAlive && !IsRidden && !animal.IsPuppet;

    // 탑승이 시작되면 AI를 멈추고 플레이어와 겹쳐 밀어내지 않도록 콜라이더를 끈다.
    public void BeginRide()
    {
        animal.SetRidden(true);
        foreach (Collider collider in colliders) collider.enabled = false;
        currentAnimation = null;
        PlayGait(HorseGait.Idle);
    }

    // 탑승이 끝나면 콜라이더를 켜고 AI에게 돌려준다.
    public void EndRide()
    {
        // 말이 죽어서 내리는 경우에는 시체의 콜라이더를 다시 켜지 않는다.
        if (IsAlive) foreach (Collider collider in colliders) collider.enabled = true;
        animal.SetRidden(false);
    }

    // 플레이어 위치(지면)와 방향을 따라가고 걸음걸이에 맞는 애니메이션과 발소리 동작을 낸다.
    public void Follow(Vector3 position, float yawDegrees, HorseGait gait)
    {
        transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yawDegrees, 0f));
        PlayGait(gait);
        animal.SetRiddenMotion(gait == HorseGait.Gallop ? AnimalMotion.Run : gait == HorseGait.Walk ? AnimalMotion.Walk : AnimalMotion.None);
    }

    private void PlayGait(HorseGait gait)
    {
        if (animator == null || animal.Definition == null) return;

        string state;
        switch (gait)
        {
            case HorseGait.Walk: state = animal.Definition.WalkState; break;
            case HorseGait.Gallop: state = animal.Definition.RunState; break;
            case HorseGait.Jump: state = jumpState; break;
            default: state = animal.Definition.IdleState; break;
        }
        if (string.IsNullOrEmpty(state) || state == currentAnimation) return;

        currentAnimation = state;
        animator.CrossFadeInFixedTime(state, animationBlendSeconds);
    }
}
