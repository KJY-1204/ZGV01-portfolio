// 장착한 무기를 관리하고, 총을 내려 든 자세/조준 자세/반동을 휴머노이드 본 IK로 만들어 주는 컴포넌트다.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[RequireComponent(typeof(Animator))]
public class WeaponEquipment : MonoBehaviour
{
    // 게임에 존재하는 무기 정의 목록이다. 인벤토리의 무기 아이템에서 무기 정의를 찾을 때 쓴다.
    [FormerlySerializedAs("loadout")]
    [SerializeField] private WeaponDefinition[] catalog = Array.Empty<WeaponDefinition>();

    // 아래 오프셋은 캐릭터 기준(오른쪽/위/앞)이며 양 어깨 중점에서 잰다.
    [Header("Pose")]
    [SerializeField] private Vector3 readyOffset = new Vector3(0.12f, -0.3f, 0.28f);
    [SerializeField] private Vector3 aimOffset = new Vector3(0.15f, -0.06f, 0.4f);
    [SerializeField] private float readyPitch = 35f;
    [SerializeField] private float aimHoldSeconds = 1.5f;
    [SerializeField] private float blendSpeed = 10f;
    [SerializeField] private float turnSpeed = 14f;
    [SerializeField, Range(0f, 1f)] private float chestPitchRatio = 0.4f;
    // 조준할 때 상체를 오른쪽으로 트는 각도다. 총 쥔 오른쪽 어깨가 뒤로 빠지고 왼쪽 어깨가 앞으로 나와 오른팔이 덜 접히고 왼손이 덮개에 닿기 쉬워진다.
    [SerializeField] private float chestAimYaw = 30f;
    // 조준하며 옆/뒤로 움직일 때 다리가 이동 방향을 향하도록 돌리는 최대 각도다. 스트레이프 클립이 없어서 다리를 이동 방향에 맞추고 상체를 반대로 비튼다.
    [SerializeField] private float legTurnLimit = 70f;
    [SerializeField] private float aimDistance = 30f;

    // 손가락이 향할 방향(총 좌표)과 손목 회전 보정이다. 본의 로컬 축을 몰라도 시각으로 맞출 수 있게 노출한다.
    [Header("Hands")]
    [SerializeField] private Vector3 rightFingerDirection = new Vector3(-0.6f, -0.2f, 0.8f);
    [SerializeField] private Vector3 leftFingerDirection = new Vector3(1f, 0.2f, 0.1f);
    [SerializeField] private float rightHandRoll;
    [SerializeField] private float leftHandRoll = 90f;
    // IK 목표는 손목 위치라서 손바닥 중심은 그보다 손가락 쪽으로 이만큼 앞서 있다. 그만큼 목표를 당겨 잡는다.
    [SerializeField] private float palmOffset = 0.06f;

    // 손가락 쥐는 정도(0 펴짐 ~ 1 완전히 접힘). 오른손은 손잡이, 왼손은 덮개를 쥔다.
    // 검지는 평소에는 방아쇠울 밖으로 펴 두고(rightIndexReady), 조준/사격 중에만 방아쇠에 건다(rightIndexAimed).
    [System.Serializable]
    private struct HandGrip
    {
        public float thumb, index, middle, ring, little;
    }

    [Header("Fingers")]
    [SerializeField] private HandGrip rightGrip = new HandGrip { thumb = 0.5f, index = 0.6f, middle = 0.8f, ring = 0.85f, little = 0.85f };
    [SerializeField] private HandGrip leftGrip = new HandGrip { thumb = 0.45f, index = 0.65f, middle = 0.7f, ring = 0.7f, little = 0.7f };
    [SerializeField] private float rightIndexReady = 0.1f;
    [SerializeField] private float rightIndexAimed = 0.6f;
    [SerializeField] private float gripBlendSpeed = 8f;

    private Animator animator;
    private Health ownHealth;
    private BuildingSystem buildingSystem;
    private Inventory inventory;
    private CombatAudio combatAudio;

    // 재장전음은 손이 탄창을 갈아 끼우는 중간쯤(진행도 45%)에 한 번 난다. 빈 탄창 클릭은 연타해도 시끄럽지 않게 간격을 둔다.
    private const float ReloadSoundProgress = 0.45f;
    private const float DryFireInterval = 0.3f;
    private bool reloadSoundPending;
    private float nextDryFireTime;

    // 휴머노이드 근육 값으로 손가락만 접는다. 근육 인덱스는 이름(Right Index 1 Stretched 등)으로 찾는다.
    private HumanPoseHandler poseHandler;
    private HumanPose humanPose;
    private int[][] rightFingerMuscles;
    private int[][] leftFingerMuscles;
    private float gripWeight;

    // 탄창에 든 탄은 런타임 상태라 정의 에셋이 아니라 여기서 무기별로 들고 있는다. 처음 장착하면 가득 찬 탄창이다.
    private readonly Dictionary<WeaponDefinition, int> magazines = new Dictionary<WeaponDefinition, int>();
    private bool reloading;
    private float reloadStartTime;

    private Transform chest;
    private Transform spine;
    private Transform head;
    private Transform rightUpper, rightLower, rightHand, rightFinger;
    private Transform leftUpper, leftLower, leftHand, leftFinger;

    private WeaponDefinition current;
    // 마지막으로 장착했던 무기. 집어넣은 뒤 같은 키로 다시 꺼낼 때 쓴다.
    private WeaponDefinition lastWeapon;
    private Transform pivot;
    // 총구 화염은 총에 붙여 둔 WarFX 이펙트 하나를 매 발사마다 다시 재생한다(발사마다 새로 만들지 않는다).
    private ParticleSystem[] flashSystems = Array.Empty<ParticleSystem>();
    private Light[] flashLights = Array.Empty<Light>();
    private bool aimHeld;
    private InputAction aimAction;
    private StarterAssets.StarterAssetsInputs moveInput;
    private StarterAssets.ThirdPersonController locomotion;
    private bool backpedal;
    private bool wasAiming;
    private float rootYaw;
    private float lastFireTime = -100f;
    private float aimBlend;
    private float recoil;
    private float flashTimer;

    // 멀티플레이에서 다른 접속자의 아바타에 붙은 보기 전용 모드다. 입력, 카메라, 탄약, 재장전 진행 없이 소유자가 보낸 무기/조준 값으로 자세만 만든다.
    private bool remoteView;
    private float remoteYaw;
    private float remotePitch;
    private bool remoteAiming;

    // 장착한 무기. 시작은 맨손(null)이고 장비창에서만 바꾼다.
    public WeaponDefinition Current => current;
    public WeaponDefinition LastWeapon => lastWeapon;
    public IReadOnlyList<WeaponDefinition> Catalog => catalog;
    // 저장용: 무기별 탄창 속 탄수.
    public IEnumerable<KeyValuePair<WeaponDefinition, int>> Magazines => magazines;
    // 조준 키를 누르고 있거나 막 사격했으면 true. 카메라와 자세가 이 값을 따른다.
    public bool IsAiming => Current != null && (remoteView ? remoteAiming : aimHeld || Time.time - lastFireTime < aimHoldSeconds);
    // 무기 목록에서의 번호(1부터). 맨손이면 0이다.
    public int SlotNumber => current == null ? 0 : Array.IndexOf(catalog, current) + 1;
    public bool IsReloading => reloading;
    // 조준하며 뒤로 물러나는 중이면 true. AimLegReverse가 걷기 애니메이션을 거꾸로 재생하는 데 쓴다.
    public bool ReverseLegs { get; private set; }
    public int MagazineRounds => Current != null ? GetMagazine(Current) : 0;
    // 예비 탄약은 인벤토리의 탄약 아이템 수량이다.
    public int ReserveRounds => Current != null && Current.AmmoItem != null && inventory != null ? inventory.GetQuantity(Current.AmmoItem) : 0;
    public event Action<WeaponDefinition> Changed;
    // 장착/탄창/재장전 상태가 바뀔 때마다 불린다. HUD가 이 이벤트로 갱신한다.
    public event Action StatusChanged;
    // 실제로 발사했을 때(NotifyFired) 알린다. 멀티플레이 아바타가 다른 접속자에게 사격 모습을 보내는 데 쓴다.
    public event Action Fired;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        ownHealth = GetComponent<Health>();
        buildingSystem = GetComponent<BuildingSystem>();
        inventory = GetComponent<Inventory>();
        combatAudio = GetComponent<CombatAudio>();
        PlayerInput playerInput = GetComponent<PlayerInput>();
        if (playerInput != null) aimAction = playerInput.actions["Aim"];
        moveInput = GetComponent<StarterAssets.StarterAssetsInputs>();
        locomotion = GetComponent<StarterAssets.ThirdPersonController>();
    }


    private void Start()
    {
        // 휴머노이드 아바타라 본 이름이 아니라 HumanBodyBones로 찾는다.
        chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        if (chest == null) chest = animator.GetBoneTransform(HumanBodyBones.Spine);
        spine = animator.GetBoneTransform(HumanBodyBones.Spine);
        head = animator.GetBoneTransform(HumanBodyBones.Head);
        rightUpper = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        rightLower = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        rightFinger = animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
        leftUpper = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        leftLower = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        leftFinger = animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal);

        // 탄약을 주우면 예비 탄약 표시가 바뀌어야 한다.
        if (inventory != null) inventory.ItemAdded += (_, __) => StatusChanged?.Invoke();

        SetupFingers();
    }

    private const float FlashLifetime = 0.08f;
    private const float FlashLightForwardOffset = 0.3f;
    private const float FlashLightIntensity = 4f;
    private const float FlashLightRange = 1.2f;

    private static readonly string[] FingerNames = { "Thumb", "Index", "Middle", "Ring", "Little" };
    // 1/2/3번 관절을 접는 비율. 끝마디는 조금 덜 접힌다.
    private static readonly float[] JointFactors = { 0.8f, 1f, 0.9f };

    private void SetupFingers()
    {
        if (animator.avatar == null || !animator.avatar.isHuman) return;
        Transform avatarRoot = FindAvatarRoot();
        if (avatarRoot == null) return;

        poseHandler = new HumanPoseHandler(animator.avatar, avatarRoot);
        rightFingerMuscles = BuildFingerMuscles("Right");
        leftFingerMuscles = BuildFingerMuscles("Left");
    }

    // HumanPoseHandler는 Animator가 붙은 오브젝트가 아니라 아바타 스켈레톤의 루트(FBX 루트)를 기준으로 동작한다.
    // 엉덩이 본에서 위로 올라가며 스킨 메시를 가진 가장 낮은 조상을 그 루트로 본다.
    private Transform FindAvatarRoot()
    {
        Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        if (hips == null) return null;
        for (Transform t = hips.parent; t != null && t != transform.parent; t = t.parent)
            if (t.GetComponentInChildren<SkinnedMeshRenderer>() != null) return t;
        return null;
    }

    private static int[][] BuildFingerMuscles(string side)
    {
        string[] muscleNames = HumanTrait.MuscleName;
        int[][] result = new int[FingerNames.Length][];
        for (int f = 0; f < FingerNames.Length; f++)
        {
            result[f] = new int[3];
            for (int j = 0; j < 3; j++)
                result[f][j] = Array.IndexOf(muscleNames, $"{side} {FingerNames[f]} {j + 1} Stretched");
        }
        return result;
    }

    // Animator가 쓴 이번 프레임 포즈에서 손가락 근육만 쥔 모양으로 바꿔 다시 쓴다.
    // 이후의 팔 IK는 손목 위치만 다루므로 손가락 모양과 충돌하지 않는다.
    private void ApplyFingers(float deltaTime)
    {
        if (poseHandler == null) return;

        gripWeight = Mathf.MoveTowards(gripWeight, 1f, gripBlendSpeed * deltaTime);
        poseHandler.GetHumanPose(ref humanPose);

        float rightIndex = Mathf.Lerp(rightIndexReady, rightIndexAimed, aimBlend);
        SetFinger(rightFingerMuscles, 0, rightGrip.thumb);
        SetFinger(rightFingerMuscles, 1, rightIndex);
        SetFinger(rightFingerMuscles, 2, rightGrip.middle);
        SetFinger(rightFingerMuscles, 3, rightGrip.ring);
        SetFinger(rightFingerMuscles, 4, rightGrip.little);

        SetFinger(leftFingerMuscles, 0, leftGrip.thumb);
        SetFinger(leftFingerMuscles, 1, leftGrip.index);
        SetFinger(leftFingerMuscles, 2, leftGrip.middle);
        SetFinger(leftFingerMuscles, 3, leftGrip.ring);
        SetFinger(leftFingerMuscles, 4, leftGrip.little);

        poseHandler.SetHumanPose(ref humanPose);
    }

    private void SetFinger(int[][] muscles, int finger, float curl)
    {
        float open = finger == 0 ? 0.3f : 0.7f;
        float closed = finger == 0 ? -0.7f : -0.95f;
        for (int j = 0; j < 3; j++)
        {
            int index = muscles[finger][j];
            if (index < 0) continue;
            float target = Mathf.Lerp(open, closed, curl * JointFactors[j]);
            humanPose.muscles[index] = Mathf.Lerp(humanPose.muscles[index], target, gripWeight);
        }
    }

    private void Update()
    {
        // 보기 전용 아바타는 재장전을 끝내거나 탄약을 건드리지 않는다.
        if (remoteView) return;
        PollAim();
        if (!reloading) return;

        WeaponDefinition weapon = Current;
        if (weapon == null || (ownHealth != null && ownHealth.IsDead))
        {
            reloading = false;
            StatusChanged?.Invoke();
            return;
        }

        if (reloadSoundPending && Time.time - reloadStartTime >= weapon.ReloadTime * ReloadSoundProgress)
        {
            reloadSoundPending = false;
            if (combatAudio != null) combatAudio.Play(weapon.ReloadSound);
        }

        if (Time.time - reloadStartTime >= weapon.ReloadTime) FinishReload(weapon);
    }

    private void OnReload(InputValue value)
    {
        if (!value.isPressed) return;
        if (HudController.AnyPanelOpen) return;
        if (buildingSystem != null && buildingSystem.BuildModeActive) return;
        if (ownHealth != null && ownHealth.IsDead) return;
        StartReload();
    }

    private int GetMagazine(WeaponDefinition weapon)
    {
        if (!magazines.TryGetValue(weapon, out int rounds))
        {
            rounds = weapon.MagazineSize;
            magazines[weapon] = rounds;
        }
        return rounds;
    }

    // 사격 한 발을 위해 탄창에서 한 발을 뺀다. 탄창이 비었거나 재장전 중이면 false이고, 빈 탄창이면 재장전을 시작한다.
    public bool TryConsumeRound()
    {
        WeaponDefinition weapon = Current;
        if (weapon == null || reloading) return false;

        int rounds = GetMagazine(weapon);
        if (rounds <= 0)
        {
            StartReload();
            if (!reloading) PlayDryFire();
            return false;
        }

        magazines[weapon] = rounds - 1;
        StatusChanged?.Invoke();
        return true;
    }

    // 탄창이 가득 찼거나 예비 탄약이 없으면 시작하지 않는다.
    public void StartReload()
    {
        WeaponDefinition weapon = Current;
        if (weapon == null || reloading) return;
        if (GetMagazine(weapon) >= weapon.MagazineSize) return;
        if (ReserveRounds <= 0) return;

        reloading = true;
        reloadSoundPending = true;
        reloadStartTime = Time.time;
        StatusChanged?.Invoke();
    }

    // 탄창이 비었는데 예비 탄약도 없을 때 방아쇠를 당기면 나는 찰칵 소리.
    private void PlayDryFire()
    {
        if (Time.time < nextDryFireTime) return;
        nextDryFireTime = Time.time + DryFireInterval;

        CombatAudioLibrary library = CombatAudioLibrary.Instance;
        if (combatAudio != null && library != null) combatAudio.Play(library.DryFire);
    }

    // 서버가 확정한 재장전 탄약을 탄창에 넣는다(멀티플레이 참가자).
    public void GrantReload(WeaponDefinition weapon, int rounds)
    {
        if (weapon == null || rounds <= 0) return;
        magazines[weapon] = Mathf.Min(weapon.MagazineSize, GetMagazine(weapon) + rounds);
        StatusChanged?.Invoke();
    }

    private void FinishReload(WeaponDefinition weapon)
    {
        reloading = false;
        int needed = weapon.MagazineSize - GetMagazine(weapon);
        // 멀티플레이 참가자의 예비 탄약은 서버가 가지고 있다. 필요한 만큼 요청하고 탄창은 서버 응답(GrantReload)이 채운다.
        if (NetworkInventory.ReplicaActive)
        {
            if (needed > 0) NetworkInventory.Local.RequestReload(weapon.WeaponId, needed);
            StatusChanged?.Invoke();
            return;
        }
        int take = Mathf.Min(needed, ReserveRounds);
        if (take > 0 && inventory != null && inventory.Remove(weapon.AmmoItem, take))
            magazines[weapon] = GetMagazine(weapon) + take;
        StatusChanged?.Invoke();
    }

    // SendMessages 모드는 버튼 액션의 뗌(canceled)을 보내지 않아 OnAim으로는 해제를 못 받는다. 액션 상태를 직접 읽는다.
    private void PollAim()
    {
        aimHeld = aimAction != null && aimAction.IsPressed();
    }

    // H키: 장착한 총을 집어넣고(맨손), 맨손이면 마지막으로 든 총을 다시 꺼낸다.
    private void OnHolster(InputValue value)
    {
        if (!value.isPressed) return;
        if (HudController.AnyPanelOpen) return;
        if (buildingSystem != null && buildingSystem.BuildModeActive) return;
        if (ownHealth != null && ownHealth.IsDead) return;

        if (current != null) Unequip();
        else if (lastWeapon != null) TryEquip(lastWeapon);
    }

    // 저장된 무기 id를 정의로 되돌린다. 비어 있거나 모르는 id면 null.
    public WeaponDefinition FindById(string weaponId)
    {
        if (string.IsNullOrEmpty(weaponId)) return null;
        foreach (WeaponDefinition weapon in catalog)
            if (weapon != null && weapon.WeaponId == weaponId) return weapon;
        return null;
    }

    // 불러오기용: 탄창 속 탄과 장착 상태를 한 번에 되돌린다. 인벤토리를 먼저 복원한 뒤에 불러야 한다.
    public void RestoreState(WeaponDefinition equipped, WeaponDefinition last, IEnumerable<KeyValuePair<WeaponDefinition, int>> savedMagazines)
    {
        magazines.Clear();
        foreach (KeyValuePair<WeaponDefinition, int> pair in savedMagazines)
            if (pair.Key != null) magazines[pair.Key] = Mathf.Clamp(pair.Value, 0, pair.Key.MagazineSize);

        reloading = false;
        SetCurrent(Owns(equipped) ? equipped : null, false);
        if (current == null) lastWeapon = Owns(last) ? last : null;
        StatusChanged?.Invoke();
    }

    // ---- 멀티플레이 보기 전용 모드(다른 접속자의 아바타) ----

    public void EnterRemoteView() => remoteView = true;

    // 소유 여부와 상관없이 소유자가 든 무기를 보여 준다(null이면 집어넣는다). 소리는 내지 않는다.
    public void ShowRemote(WeaponDefinition weapon) => SetCurrent(weapon, false);

    // 소유자가 보낸 조준 방향(카메라 방향 yaw/pitch), 조준 여부, 재장전 여부를 자세에 반영한다.
    public void SetRemoteAim(float yaw, float pitch, bool aiming, bool isReloading)
    {
        remoteYaw = yaw;
        remotePitch = pitch;
        remoteAiming = aiming;
        if (isReloading && !reloading) reloadStartTime = Time.time;
        reloading = isReloading;
    }

    // 인벤토리에 이 무기 아이템이 있으면 true.
    public bool Owns(WeaponDefinition weapon)
    {
        return weapon != null && weapon.Item != null && inventory != null && inventory.GetQuantity(weapon.Item) > 0;
    }

    // 인벤토리의 무기 아이템에 해당하는 무기 정의를 찾는다. 무기가 아닌 아이템이면 null.
    public WeaponDefinition FindWeapon(ItemDefinition item)
    {
        if (item == null) return null;
        foreach (WeaponDefinition weapon in catalog)
            if (weapon != null && weapon.Item == item) return weapon;
        return null;
    }

    // 장비창에서 부른다. 갖고 있지 않은 무기이거나 건축 모드/사망 중이면 장착하지 않는다.
    public bool TryEquip(WeaponDefinition weapon)
    {
        if (weapon == null || !Owns(weapon)) return false;
        if (buildingSystem != null && buildingSystem.BuildModeActive) return false;
        if (ownHealth != null && ownHealth.IsDead) return false;

        SetCurrent(weapon);
        return true;
    }

    public void Unequip()
    {
        SetCurrent(null);
    }

    private void SetCurrent(WeaponDefinition weapon, bool playSound = true)
    {
        if (current == weapon) return;
        current = weapon;
        if (weapon != null) lastWeapon = weapon;

        // 꺼낼 때와 집어넣을 때 같은 소리를 쓴다. 저장 불러오기로 복원할 때는 내지 않는다.
        CombatAudioLibrary library = CombatAudioLibrary.Instance;
        if (playSound && combatAudio != null && library != null) combatAudio.Play(library.Equip);

        if (pivot != null) Destroy(pivot.gameObject);
        pivot = null;
        flashSystems = Array.Empty<ParticleSystem>();
        flashLights = Array.Empty<Light>();

        if (weapon != null && weapon.ModelPrefab != null) BuildWeaponObjects(weapon);

        recoil = 0f;
        reloading = false;
        gripWeight = 0f;
        Changed?.Invoke(weapon);
        StatusChanged?.Invoke();
    }

    private void BuildWeaponObjects(WeaponDefinition weapon)
    {
        pivot = new GameObject("WeaponPivot").transform;
        pivot.SetParent(transform, false);

        GameObject model = Instantiate(weapon.ModelPrefab, pivot);
        model.transform.localRotation = Quaternion.Euler(weapon.ModelEuler);
        model.transform.localPosition = weapon.ModelOffset;
        // 총 모델의 콜라이더가 플레이어/히트스캔과 겹치지 않게 제거한다.
        foreach (Collider col in model.GetComponentsInChildren<Collider>()) Destroy(col);

        if (weapon.ModelMaterial != null)
        {
            foreach (Renderer modelRenderer in model.GetComponentsInChildren<Renderer>())
            {
                Material[] materials = new Material[modelRenderer.sharedMaterials.Length];
                Array.Fill(materials, weapon.ModelMaterial);
                modelRenderer.sharedMaterials = materials;
            }
        }

        if (weapon.MuzzleFlashPrefab != null) BuildMuzzleFlash(weapon);
    }

    // 총구에 WarFX 화염 이펙트를 붙여 두고 멈춘 상태로 대기시킨다. 프리팹의 점광원은 사격 직후에만 켠다.
    private void BuildMuzzleFlash(WeaponDefinition weapon)
    {
        GameObject flash = Instantiate(weapon.MuzzleFlashPrefab, pivot);
        flash.name = "MuzzleFlash";
        // 회전은 프리팹이 가진 값을 그대로 쓴다. WarFX 화염은 루트에 Y 270도가 들어 있어 그 방향이 총구 정면(+Z)이다.
        flash.transform.localPosition = weapon.Muzzle;
        flash.transform.localScale = Vector3.one * weapon.MuzzleFlashScale;

        // 벤더 점광원 스크립트는 Start부터 빛을 계속 깜빡이므로 끄고, 사격 직후에만 우리가 켠다.
        foreach (WFX_LightFlicker flicker in flash.GetComponentsInChildren<WFX_LightFlicker>(true)) flicker.enabled = false;

        flashSystems = flash.GetComponentsInChildren<ParticleSystem>();
        flashLights = flash.GetComponentsInChildren<Light>();
        foreach (ParticleSystem system in flashSystems)
        {
            // 자동 방출(초당 10개)은 끄고 발사 때 직접 내보낸다.
            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = false;
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        foreach (Light flashLight in flashLights)
        {
            // 프리팹 값(강도 12.4, 범위 2m)은 몸에서 0.7m인 총구에서 몸통과 손을 통째로 밝혀 몸에서 터지는 것처럼 보인다.
            // 총구 앞으로 내밀고 약하고 좁게 줄여 총구 주변만 밝힌다.
            flashLight.transform.position = flash.transform.position + pivot.forward * FlashLightForwardOffset;
            flashLight.intensity = FlashLightIntensity;
            flashLight.range = FlashLightRange;
            flashLight.enabled = false;
        }
    }

    // HitscanWeapon이 실제로 발사했을 때 부른다. 반동과 총구 화염, 조준 자세 유지 시간을 갱신한다.
    public void NotifyFired()
    {
        recoil = 1f;
        lastFireTime = Time.time;
        Fired?.Invoke();

        if (combatAudio != null && current != null) combatAudio.PlayRandom(current.FireSounds, current.FireVolume);

        // 연사 중에는 이전 화염이 남아 있어도 처음부터 다시 터지게 한다.
        // 프리팹은 초당 10개를 0.05초씩 내는 반복 이펙트라 한 발에는 입자가 거의 안 나온다. 발사마다 입자 하나를 직접 내보낸다.
        ParticleSystem.EmitParams flashParticle = new ParticleSystem.EmitParams { startLifetime = FlashLifetime };
        foreach (ParticleSystem system in flashSystems)
        {
            system.Clear(false);
            system.Play(false);
            system.Emit(flashParticle, 1);
        }
        flashTimer = 0.06f;
        foreach (Light flashLight in flashLights) flashLight.enabled = true;
    }

    private void LateUpdate()
    {
        WeaponDefinition weapon = Current;
        if (weapon == null || pivot == null || rightUpper == null || leftUpper == null || chest == null || (ownHealth != null && ownHealth.IsDead))
        {
            ReverseLegs = false;
            return;
        }

        float dt = Time.deltaTime;
        Camera cam = remoteView ? null : Camera.main;
        if (!remoteView && cam == null) return;

        // 재장전 중에는 총을 내려서 왼손이 탄창을 갈아 끼우는 모습이 보이게 한다.
        bool aiming = IsAiming && !reloading;
        aimBlend = Mathf.MoveTowards(aimBlend, aiming ? 1f : 0f, blendSpeed * dt);
        recoil *= Mathf.Exp(-14f * dt);

        ApplyFingers(dt);

        if (flashTimer > 0f)
        {
            flashTimer -= dt;
            if (flashTimer <= 0f)
            {
                // 화염 파티클이 루프라서 멈추지 않으면 사격이 끝나도 계속 나온다.
                foreach (ParticleSystem system in flashSystems) system.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                foreach (Light flashLight in flashLights) flashLight.enabled = false;
            }
        }

        // 조준 중에는 상체가 카메라 방향을 보고, 다리는 이동 방향을 향한다(스트레이프 클립이 없어서 발이 미끄러지지 않게).
        // 이동 방향으로 도는 기본 컨트롤러 뒤에 덮어쓴다.
        float camYaw = remoteView ? remoteYaw : cam.transform.eulerAngles.y;
        bool moving = moveInput != null && moveInput.move.sqrMagnitude > 0.01f;
        bool reverseLegs = false;
        // 보기 전용 아바타의 몸 방향은 소유자가 네트워크로 보낸 값이라 여기서 돌리지 않는다.
        if (aiming && !remoteView)
        {
            float targetOffset = 0f;
            if (moving)
            {
                // 입력 방향이 카메라 앞에서 몇 도 틀어졌는지(-180~180). 뒤로 갈 때는 다리를 정면으로 두고 걷기를 거꾸로 재생한다.
                float diff = Mathf.Atan2(moveInput.move.x, moveInput.move.y) * Mathf.Rad2Deg;
                if (backpedal) { if (Mathf.Abs(diff) < 70f) backpedal = false; }
                else if (Mathf.Abs(diff) > 110f) backpedal = true;
                float legDirection = backpedal ? Mathf.DeltaAngle(0f, diff - 180f) : diff;
                targetOffset = Mathf.Clamp(legDirection, -legTurnLimit, legTurnLimit);
                reverseLegs = backpedal && (locomotion == null || locomotion.Grounded);
            }
            else
            {
                backpedal = false;
            }

            // 기본 컨트롤러도 매 프레임 이동 방향으로 루트를 돌리므로 둘이 줄다리기를 하지 않게, 조준 중에는 루트 방향을 직접 정한다.
            if (!wasAiming) rootYaw = transform.eulerAngles.y;
            rootYaw = Mathf.LerpAngle(rootYaw, camYaw + targetOffset, 1f - Mathf.Exp(-turnSpeed * dt));
            transform.rotation = Quaternion.Euler(0f, rootYaw, 0f);
        }
        else
        {
            backpedal = false;
        }
        wasAiming = aiming;
        ReverseLegs = reverseLegs;

        // 다리가 조준 방향에서 벗어난 만큼 척추와 가슴을 반대로 비틀어 상체를 계속 조준 방향에 둔다.
        float twist = Mathf.DeltaAngle(transform.eulerAngles.y, camYaw) * aimBlend;
        if (spine != null && spine != chest)
        {
            spine.rotation = Quaternion.AngleAxis(twist * 0.4f, Vector3.up) * spine.rotation;
            chest.rotation = Quaternion.AngleAxis(twist * 0.6f, Vector3.up) * chest.rotation;
        }
        else
        {
            chest.rotation = Quaternion.AngleAxis(twist, Vector3.up) * chest.rotation;
        }

        // 조준 중 총과 손은 몸이 아니라 조준 방향(카메라 yaw)을 기준으로 잡는다.
        Quaternion yaw = Quaternion.Euler(0f, Mathf.LerpAngle(transform.eulerAngles.y, camYaw, aimBlend), 0f);

        // 카메라 피치를 상체에 일부 나눠 줘서 위아래를 볼 때 몸도 같이 기울게 한다.
        float camPitch = remoteView ? remotePitch : Mathf.DeltaAngle(0f, cam.transform.eulerAngles.x);
        chest.rotation = Quaternion.AngleAxis(camPitch * chestPitchRatio * aimBlend, yaw * Vector3.right) * chest.rotation;
        chest.rotation = Quaternion.AngleAxis(chestAimYaw * aimBlend, Vector3.up) * chest.rotation;
        // 상체를 튼 만큼 머리는 반대로 돌려 계속 목표를 보게 한다.
        if (head != null) head.rotation = Quaternion.AngleAxis(-chestAimYaw * aimBlend, Vector3.up) * head.rotation;

        Vector3 shoulderCenter = (rightUpper.position + leftUpper.position) * 0.5f;
        Vector3 aimPoint = remoteView
            ? shoulderCenter + Quaternion.Euler(remotePitch, remoteYaw, 0f) * Vector3.forward * aimDistance
            : cam.transform.position + cam.transform.forward * aimDistance;

        Vector3 aimPivotPos = shoulderCenter + yaw * aimOffset;
        Quaternion aimRotation = Quaternion.LookRotation((aimPoint - aimPivotPos).normalized, Vector3.up);
        Quaternion readyRotation = yaw * Quaternion.Euler(readyPitch, 0f, 0f);

        Quaternion rotation = Quaternion.Slerp(readyRotation, aimRotation, aimBlend);
        Vector3 position = shoulderCenter + yaw * Vector3.Lerp(readyOffset, aimOffset, aimBlend);

        position += rotation * Vector3.forward * weapon.HoldForward;

        // 반동은 총을 뒤로 밀고 총구를 위로 든다.
        position -= rotation * Vector3.forward * (recoil * weapon.RecoilBack);
        rotation *= Quaternion.Euler(-recoil * weapon.RecoilPitch, 0f, 0f);

        // 재장전 손 동작: 덮개에서 탄창 위치로 내려갔다가(탄창 교체) 다시 덮개로 돌아온다. 총도 살짝 기울인다.
        float reloadHand = reloading ? ReloadHandCurve(Mathf.Clamp01((Time.time - reloadStartTime) / weapon.ReloadTime)) : 0f;
        rotation *= Quaternion.Euler(0f, 0f, -18f * reloadHand);
        pivot.SetPositionAndRotation(position, rotation);

        Vector3 leftTarget = Vector3.Lerp(weapon.LeftGrip, weapon.MagazinePoint, reloadHand);
        Vector3 rightWrist = pivot.TransformPoint(weapon.RightGrip) - rotation * rightFingerDirection.normalized * palmOffset;
        Vector3 leftWrist = pivot.TransformPoint(leftTarget) - rotation * leftFingerDirection.normalized * palmOffset;
        SolveArm(rightUpper, rightLower, rightHand, rightWrist, yaw * new Vector3(0.6f, -1f, -0.2f));
        SolveArm(leftUpper, leftLower, leftHand, leftWrist, yaw * new Vector3(-0.6f, -1f, -0.2f));
        AlignHand(rightHand, rightFinger, rotation, rightFingerDirection, rightHandRoll);
        AlignHand(leftHand, leftFinger, rotation, leftFingerDirection, leftHandRoll);
    }

    // 재장전 진행도(0~1)에 대한 왼손 위치 가중치다. 빠르게 내려가 머물다가 올라온다.
    private static float ReloadHandCurve(float t)
    {
        if (t < 0.2f) return Mathf.SmoothStep(0f, 1f, t / 0.2f);
        if (t < 0.75f) return 1f;
        return Mathf.SmoothStep(1f, 0f, (t - 0.75f) / 0.25f);
    }

    // 어깨-팔꿈치-손목 2본 IK. 손목이 target에 닿도록 팔꿈치를 pole 방향으로 굽혀서 두 본을 돌린다.
    private static void SolveArm(Transform upper, Transform lower, Transform hand, Vector3 target, Vector3 poleDirection)
    {
        float upperLength = Vector3.Distance(upper.position, lower.position);
        float lowerLength = Vector3.Distance(lower.position, hand.position);

        Vector3 toTarget = target - upper.position;
        float distance = Mathf.Clamp(toTarget.magnitude, Mathf.Abs(upperLength - lowerLength) + 0.001f, upperLength + lowerLength - 0.001f);
        Vector3 direction = toTarget.normalized;

        float along = (upperLength * upperLength - lowerLength * lowerLength + distance * distance) / (2f * distance);
        float height = Mathf.Sqrt(Mathf.Max(0f, upperLength * upperLength - along * along));
        Vector3 pole = Vector3.ProjectOnPlane(poleDirection, direction).normalized;
        Vector3 elbow = upper.position + direction * along + pole * height;

        upper.rotation = Quaternion.FromToRotation(lower.position - upper.position, elbow - upper.position) * upper.rotation;
        Vector3 handTarget = upper.position + direction * distance;
        lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, handTarget - lower.position) * lower.rotation;
    }

    // 손가락이 가리키는 방향을 총 기준 방향에 맞추고, 손목 롤을 보정한다.
    private static void AlignHand(Transform hand, Transform finger, Quaternion gunRotation, Vector3 fingerDirection, float roll)
    {
        if (hand == null || finger == null) return;
        Vector3 desired = gunRotation * fingerDirection.normalized;
        Vector3 current = (finger.position - hand.position).normalized;
        hand.rotation = Quaternion.AngleAxis(roll, desired) * Quaternion.FromToRotation(current, desired) * hand.rotation;
    }
}
