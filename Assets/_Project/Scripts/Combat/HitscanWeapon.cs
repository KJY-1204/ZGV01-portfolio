// 카메라 조준선 기준으로 히트스캔 사격을 수행하고 대상의 Health에 데미지를 적용하는 로컬 권위 컴포넌트다.
using UnityEngine;
using UnityEngine.InputSystem;

public class HitscanWeapon : MonoBehaviour
{
    [SerializeField] private Camera aimCamera;
    [SerializeField] private LayerMask hitMask = ~0;

    private BuildingSystem buildingSystem;
    private WeaponEquipment equipment;
    private Health ownHealth;
    private SurvivalStats survival;
    private InputAction fireAction;
    private float nextFireTime;

    // 총알이 살아 있는 대상에 맞았을 때 알린다(머리 피격 여부, 처치 여부). HUD 조준점 표시가 구독한다.
    public event System.Action<bool, bool> HitConfirmed;

    private void Awake()
    {
        buildingSystem = GetComponent<BuildingSystem>();
        equipment = GetComponent<WeaponEquipment>();
        ownHealth = GetComponent<Health>();
        survival = GetComponent<SurvivalStats>();

        PlayerInput playerInput = GetComponent<PlayerInput>();
        if (playerInput != null) fireAction = playerInput.actions.FindAction("Fire");
    }

    private void OnFire(InputValue value)
    {
        if (value.isPressed) TryFire();
    }

    // 자동 무기는 버튼을 누르고 있는 동안 발사 간격마다 계속 쏜다. 눌림 해제 이벤트를 놓쳐도
    // (창 포커스 변경 등) 계속 연사되지 않도록 이벤트가 아니라 입력 액션의 실제 눌림 상태를 읽는다.
    private void Update()
    {
        if (fireAction == null || !fireAction.IsPressed()) return;
        WeaponDefinition weapon = equipment != null ? equipment.Current : null;
        if (weapon != null && weapon.Automatic) TryFire();
    }

    private void TryFire()
    {
        // 장착한 무기가 없으면 사격이 아니라 맨손 공격이다.
        WeaponDefinition weapon = equipment != null ? equipment.Current : null;
        if (weapon == null) return;
        if (Time.time < nextFireTime) return;
        if (ownHealth != null && ownHealth.IsDead) return;
        if (buildingSystem != null && buildingSystem.BuildModeActive) return;
        // 인벤토리/제작 패널이 열려 커서가 풀려있을 때는 클릭이 UI 조작이지 사격이 아니다.
        if (HudController.AnyPanelOpen) return;
        // 탄창이 비었거나 재장전 중이면 쏘지 못한다(빈 탄창이면 재장전이 시작된다).
        if (!equipment.TryConsumeRound()) return;

        // 허약 상태에서는 공격 속도 배율만큼 발사 간격이 길어진다.
        nextFireTime = Time.time + weapon.FireInterval / (survival != null ? survival.AttackSpeedMultiplier : 1f);
        Fire(weapon);
        NoiseSystem.Emit(transform.position, weapon.NoiseRadius, this);
        equipment.NotifyFired();
    }

    // 서버가 참가자의 사격을 판정한 결과를 알려 올 때 조준점 표시를 낸다.
    public void NotifyRemoteHit(bool headshot, bool killed) => HitConfirmed?.Invoke(headshot, killed);

    private void Fire(WeaponDefinition weapon)
    {
        Camera cam = aimCamera != null ? aimCamera : Camera.main;
        if (cam == null) return;

        // 카메라가 캐릭터 뒤에 있어 조준선이 자기 몸에 먼저 걸릴 수 있으므로, 자기 자신을 건너뛴 첫 대상을 찾는다.
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        ShotResolver.Result shot = ShotResolver.Trace(ray, weapon.Range, hitMask, ownHealth);

        // 멀티플레이 참가자는 판정 권위가 없다. 요청만 서버에 보내고 자기 화면의 탄착 효과만 미리 보여 준다.
        if (MultiplayerSession.IsClientOnly && NetworkCombat.Local != null)
        {
            NetworkCombat.Local.RequestFire(ray.origin, ray.direction, weapon.WeaponId);
            if (shot.didHit) ImpactEffects.Spawn(shot.hit.collider, shot.hit.point, shot.hit.normal);
            return;
        }

        // 호스트의 사격은 총소리와 탄착을 참가자들에게도 알린다(빗나가도 총소리는 간다).
        NetworkCombat.BroadcastHostShot(weapon.WeaponId, ray.origin, shot.didHit, shot.didHit ? shot.hit.point : ray.origin + ray.direction * weapon.Range,
            shot.didHit ? shot.hit.normal : -ray.direction, shot.didHit && ImpactEffectLibrary.Instance != null ? ImpactEffects.SurfaceOf(shot.hit.collider, ImpactEffectLibrary.Instance) : ImpactSurface.Dirt);
        if (!shot.didHit) return;

        if (shot.health != null)
        {
            float damage = weapon.Damage * shot.multiplier * (survival != null ? survival.DamageMultiplier : 1f);
            shot.health.ApplyDamage(damage, ray.direction, shot.headshot);
            HitConfirmed?.Invoke(shot.headshot, shot.health.IsDead);
        }
        ImpactEffects.Spawn(shot.hit.collider, shot.hit.point, shot.hit.normal);
    }
}
