// 맨손 공격 입력을 받아 오른팔과 상체를 곡선으로 움직이고, 타격 순간 앞쪽 대상의 Health에 데미지를 주는 로컬 권위 컴포넌트다.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
public class UnarmedPunch : MonoBehaviour
{
    [Header("Hit")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float reach = 1.4f;
    [SerializeField] private float hitRadius = 0.6f;
    [SerializeField] private float hitHeight = 1.2f;
    [SerializeField, Range(0f, 1f)] private float hitTime = 0.4f;

    [Header("Timing")]
    [SerializeField] private float duration = 0.45f;
    [SerializeField] private float cooldown = 0.15f;

    // 아래 곡선은 가로축이 정규화 시간(0~1), 세로축이 각도(도)다. 처음과 끝이 0이라 기존 애니메이션과 자연스럽게 섞인다.
    // 어깨는 팔을 앞으로 드는 각도, 팔꿈치는 접히는 각도, 상체는 위쪽 축 기준 비틀림(양수가 왼쪽으로 돈다)이다.
    // 기본 포즈에 이미 팔꿈치가 조금 굽어 있어서 타격 순간에는 음수로 더 펴야 팔이 곧게 뻗는다.
    [Header("Pose Curves")]
    [SerializeField] private AnimationCurve shoulderForward = new AnimationCurve(
        new Keyframe(0f, 0f), new Keyframe(0.25f, 45f), new Keyframe(0.4f, 85f), new Keyframe(0.55f, 85f), new Keyframe(1f, 0f));
    [SerializeField] private AnimationCurve elbowBend = new AnimationCurve(
        new Keyframe(0f, 0f), new Keyframe(0.25f, 100f), new Keyframe(0.4f, -35f), new Keyframe(0.55f, -35f), new Keyframe(1f, 0f));
    [SerializeField] private AnimationCurve torsoTwist = new AnimationCurve(
        new Keyframe(0f, 0f), new Keyframe(0.25f, -12f), new Keyframe(0.4f, 18f), new Keyframe(0.55f, 18f), new Keyframe(1f, 0f));

    private Animator animator;
    private Health ownHealth;
    private SurvivalStats survival;
    private BuildingSystem buildingSystem;
    private WeaponEquipment equipment;
    private CombatAudio combatAudio;
    private Transform chest;
    private Transform upperArm;
    private Transform lowerArm;

    private bool punching;
    private bool hitDone;
    private float elapsed;
    private float nextAllowedTime;
    // 멀티플레이에서 다른 접속자의 아바타에 붙은 보기 전용 모드다. 입력, 피해, 소음, 소리 없이 팔과 상체 곡선만 재생한다.
    private bool remoteView;

    // 펀치 소음이 들리는 거리(m)다. 서버가 참가자의 펀치를 처리할 때도 같은 값을 쓴다.
    public const float NoiseRadius = 7f;

    public bool IsPunching => punching;
    // 펀치를 시작할 때 알린다(멀티플레이 아바타가 다른 접속자에게 펀치 모습을 보내는 데 쓴다).
    public event System.Action Punched;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        ownHealth = GetComponent<Health>();
        survival = GetComponent<SurvivalStats>();
        buildingSystem = GetComponent<BuildingSystem>();
        equipment = GetComponent<WeaponEquipment>();
        combatAudio = GetComponent<CombatAudio>();
    }

    private void Start()
    {
        // 휴머노이드 아바타라 본 이름이 아니라 HumanBodyBones로 찾는다.
        chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        if (chest == null) chest = animator.GetBoneTransform(HumanBodyBones.Spine);
        upperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        lowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
    }

    private void OnPunch(InputValue value)
    {
        if (!value.isPressed) return;
        TryStart();
    }

    // 조건이 맞으면 펀치를 시작한다(입력과 시험 코드가 같이 쓴다).
    public bool TryStart()
    {
        if (remoteView || punching || Time.time < nextAllowedTime) return false;
        if (ownHealth != null && ownHealth.IsDead) return false;
        if (buildingSystem != null && buildingSystem.BuildModeActive) return false;
        // 무기를 들고 있으면 사격이 좌클릭을 쓰므로 맨손 공격은 하지 않는다.
        if (equipment != null && equipment.Current != null) return false;
        // 인벤토리/제작 패널이 열려 커서가 풀려있을 때는 게임플레이 입력을 받지 않는다.
        if (HudController.AnyPanelOpen) return false;
        if (upperArm == null || lowerArm == null) return false;

        // 타격이 카메라가 보는 쪽으로 나가도록 시작할 때 캐릭터를 카메라 방향으로 돌린다.
        Camera cam = Camera.main;
        if (cam != null) transform.rotation = Quaternion.Euler(0f, cam.transform.eulerAngles.y, 0f);

        CombatAudioLibrary library = CombatAudioLibrary.Instance;
        if (combatAudio != null && library != null) combatAudio.Play(library.PunchSwing);
        NoiseSystem.Emit(transform.position, NoiseRadius, this);

        punching = true;
        hitDone = false;
        elapsed = 0f;
        Punched?.Invoke();
        return true;
    }

    // ---- 멀티플레이 보기 전용 모드(다른 접속자의 아바타) ----

    public void EnterRemoteView() => remoteView = true;

    // 소유자가 펀치를 시작했다는 알림을 받았을 때 팔과 상체 곡선을 처음부터 재생한다(피해는 소유자 쪽에서 이미 처리했다).
    public void PlayRemote()
    {
        if (!remoteView || upperArm == null || lowerArm == null) return;
        punching = true;
        hitDone = true;
        elapsed = 0f;
    }

    private void Update()
    {
        if (!punching) return;

        if (ownHealth != null && ownHealth.IsDead)
        {
            punching = false;
            return;
        }

        // 허약 상태에서는 공격 속도 배율만큼 펀치 동작이 느려진다.
        elapsed += Time.deltaTime * AttackSpeedMultiplier;
        float t = Mathf.Clamp01(elapsed / duration);

        if (!hitDone && t >= hitTime)
        {
            hitDone = true;
            DealDamage();
        }

        if (elapsed >= duration)
        {
            punching = false;
            nextAllowedTime = Time.time + cooldown / AttackSpeedMultiplier;
        }
    }

    // Animator가 이번 프레임 포즈를 쓴 뒤에 덧씌워야 하므로 LateUpdate에서 적용한다.
    private void LateUpdate()
    {
        if (!punching) return;

        float t = Mathf.Clamp01(elapsed / duration);

        // 월드 회전을 플레이어 기준 축으로 곱해서 본의 로컬 축 방향과 무관하게 동작시킨다.
        // 부모(상체)를 먼저 돌려야 자식인 팔이 그 회전을 이어받는다.
        if (chest != null)
            chest.rotation = Quaternion.AngleAxis(torsoTwist.Evaluate(t), transform.up) * chest.rotation;
        upperArm.rotation = Quaternion.AngleAxis(-shoulderForward.Evaluate(t), transform.right) * upperArm.rotation;
        lowerArm.rotation = Quaternion.AngleAxis(-elbowBend.Evaluate(t), transform.right) * lowerArm.rotation;
    }

    private float AttackSpeedMultiplier => survival != null ? survival.AttackSpeedMultiplier : 1f;

    private void DealDamage()
    {
        float multiplier = survival != null ? survival.DamageMultiplier : 1f;

        // 멀티플레이 참가자는 판정을 서버에 맡긴다. 서버가 호스트 월드에서 판정해 피해를 주고 효과를 모두에게 알린다.
        if (MultiplayerSession.IsClientOnly && NetworkCombat.Local != null)
        {
            NetworkCombat.Local.RequestPunch(transform.position, transform.forward, multiplier);
            return;
        }

        // 싱글플레이와 호스트는 로컬 월드가 권위다. 맞은 효과와 소리를 바로 내고 호스트는 참가자에게도 알린다.
        int hits = ApplyHits(transform.position, transform.forward, damage * multiplier, ownHealth, false, true, out Collider firstCollider, out Vector3 firstPoint, out Vector3 firstNormal);
        if (hits == 0) return;

        CombatAudioLibrary library = CombatAudioLibrary.Instance;
        if (combatAudio != null && library != null) combatAudio.PlayRandom(library.PunchHits);
        if (MultiplayerSession.IsHost && ImpactEffectLibrary.Instance != null)
            NetworkCombat.BroadcastHostPunch(firstPoint, firstNormal, ImpactEffects.SurfaceOf(firstCollider, ImpactEffectLibrary.Instance));
    }

    // 서버가 참가자의 펀치를 처리한다. 이 컴포넌트(보기 전용 아바타의 복사본)가 가진 거리, 반경, 높이, 피해 설정으로 호스트 월드에서 판정하고 플레이어는 맞히지 않는다(협동).
    // 피해 배율은 0~1로 제한한다(허약 상태가 줄이는 값이라 클라이언트가 틀려도 더 약해질 뿐이다). 맞은 대상 수와 첫 맞은 지점을 돌려준다.
    public int ResolveForServer(Vector3 position, Vector3 forward, float damageScale, out Vector3 point, out Vector3 normal, out Collider collider)
    {
        return ApplyHits(position, forward, damage * Mathf.Clamp01(damageScale), null, true, false, out collider, out point, out normal);
    }

    // position 앞쪽 구 범위의 대상마다 한 번씩 피해를 준다. localEffects가 참이면 맞은 지점에 살점 이펙트를 바로 낸다. 맞은 수와 첫 맞은 지점을 돌려준다.
    private int ApplyHits(Vector3 position, Vector3 forward, float amount, Health self, bool skipPlayers, bool localEffects, out Collider firstCollider, out Vector3 firstPoint, out Vector3 firstNormal)
    {
        firstCollider = null;
        firstPoint = default;
        firstNormal = default;

        Vector3 center = position + Vector3.up * hitHeight + forward * (reach * 0.5f);
        Collider[] colliders = Physics.OverlapSphere(center, hitRadius, ~0, QueryTriggerInteraction.Ignore);
        var hitTargets = new HashSet<Health>();
        int count = 0;

        foreach (Collider col in colliders)
        {
            Health target = col.GetComponentInParent<Health>();
            if (target == null || target == self || target.IsDead) continue;
            if (skipPlayers && target.CompareTag("Player")) continue;
            if (!hitTargets.Add(target)) continue;
            target.ApplyDamage(amount);

            // 맞은 지점에서 플레이어 쪽으로 향하는 방향을 법선으로 삼는다.
            Vector3 point = col.ClosestPoint(center);
            Vector3 normal = center - point;
            normal = normal.sqrMagnitude > 0.0001f ? normal.normalized : -forward;
            // 맞는 소리는 펀치 전용 소리를 내므로 탄착 이펙트의 피격음은 끈다.
            if (localEffects) ImpactEffects.Spawn(col, point, normal, false);

            if (count == 0)
            {
                firstCollider = col;
                firstPoint = point;
                firstNormal = normal;
            }
            count++;
        }
        return count;
    }
}
