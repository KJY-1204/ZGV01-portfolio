// 접속자 아바타에 손에 든 무기의 모습을 동기화하는 표현 전용 컴포넌트다. 소유자 쪽에서는 로컬 플레이어의 무기/조준/재장전/사격 상태를 네트워크 변수로 올리고, 다른 접속자 쪽에서는 그 값으로 아바타의 무기 자세(WeaponEquipment 보기 전용 모드)를 만든다. 게임 규칙은 갖지 않는다.
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(WeaponEquipment))]
public class AvatarWeaponSync : NetworkBehaviour
{
    // 조준 방향이 이 각도(도) 이상 바뀔 때만 보내 네트워크 사용량을 줄인다.
    private const float AimSendThreshold = 0.5f;
    // 받은 조준 방향을 부드럽게 따라가는 속도다(네트워크 틱 사이의 끊김을 가린다).
    private const float AimSmoothing = 18f;

    private readonly NetworkVariable<FixedString32Bytes> weaponId = new NetworkVariable<FixedString32Bytes>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<bool> aiming = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<bool> reloading = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<float> aimYaw = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<float> aimPitch = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    // 발사할 때마다 하나씩 올라간다. 값이 바뀌면 다른 접속자 쪽에서 반동과 총구 화염을 낸다.
    private readonly NetworkVariable<int> fireCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private WeaponEquipment view;
    private NetworkCombat combat;
    // 보는 쪽: 소유자가 죽어 있는 동안은 손에 든 무기를 숨긴다.
    private bool hiddenForDeath;
    // 소유자 쪽: 로컬 플레이어의 무기 컴포넌트
    private WeaponEquipment source;
    private string sentWeaponId = "";
    // 보는 쪽: 부드럽게 따라가는 조준 방향
    private float shownYaw;
    private float shownPitch;

    // 서버가 이 접속자가 지금 총을 들고 있는지 알 때 쓴다(총을 든 채 맨손 공격 요청은 받지 않는다).
    public bool HoldsWeapon => weaponId.Value.Length > 0;

    public override void OnNetworkSpawn()
    {
        view = GetComponent<WeaponEquipment>();
        combat = GetComponent<NetworkCombat>();
        if (IsOwner) return;

        // 다른 접속자의 아바타는 입력 없이 받은 값으로만 자세를 만든다.
        view.EnterRemoteView();
        shownYaw = aimYaw.Value;
        shownPitch = aimPitch.Value;
        weaponId.OnValueChanged += OnWeaponChanged;
        fireCount.OnValueChanged += OnFired;
        ShowWeapon(weaponId.Value.ToString());
    }

    public override void OnNetworkDespawn()
    {
        weaponId.OnValueChanged -= OnWeaponChanged;
        fireCount.OnValueChanged -= OnFired;
        if (source != null) source.Fired -= OnLocalFired;
        source = null;
    }

    private void Update()
    {
        if (!IsSpawned) return;
        if (IsOwner) UpdateOwner();
        else UpdateRemote();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        RunDebug();
#endif
    }

    // ---- 소유자: 로컬 플레이어 상태를 올린다 ----

    private void UpdateOwner()
    {
        if (source == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            WeaponEquipment found = player != null ? player.GetComponent<WeaponEquipment>() : null;
            if (found == null) return;
            source = found;
            source.Fired += OnLocalFired;
        }

        string id = source.Current != null ? source.Current.WeaponId : "";
        if (id != sentWeaponId)
        {
            sentWeaponId = id;
            weaponId.Value = id;
        }
        if (id.Length == 0)
        {
            if (aiming.Value) aiming.Value = false;
            if (reloading.Value) reloading.Value = false;
            return;
        }

        bool isAiming = source.IsAiming;
        if (aiming.Value != isAiming) aiming.Value = isAiming;
        if (reloading.Value != source.IsReloading) reloading.Value = source.IsReloading;

        Camera cam = Camera.main;
        if (cam == null) return;
        float yaw = cam.transform.eulerAngles.y;
        float pitch = Mathf.DeltaAngle(0f, cam.transform.eulerAngles.x);
        if (Mathf.Abs(Mathf.DeltaAngle(aimYaw.Value, yaw)) >= AimSendThreshold) aimYaw.Value = yaw;
        if (Mathf.Abs(pitch - aimPitch.Value) >= AimSendThreshold) aimPitch.Value = pitch;
    }

    private void OnLocalFired()
    {
        if (IsSpawned && IsOwner) fireCount.Value++;
    }

    // ---- 다른 접속자: 받은 값으로 자세를 만든다 ----

    private void UpdateRemote()
    {
        if (view == null) return;

        bool dead = combat != null && combat.IsDead;
        if (dead != hiddenForDeath)
        {
            hiddenForDeath = dead;
            ShowWeapon(weaponId.Value.ToString());
        }

        float blend = 1f - Mathf.Exp(-AimSmoothing * Time.deltaTime);
        shownYaw = Mathf.LerpAngle(shownYaw, aimYaw.Value, blend);
        shownPitch = Mathf.Lerp(shownPitch, aimPitch.Value, blend);
        view.SetRemoteAim(shownYaw, shownPitch, aiming.Value, reloading.Value);
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // 테스트용: -mp-weapon-log면 다른 접속자의 아바타가 보여 주는 무기 상태를 2초마다 남기고, -mp-weapon-test면 소유자가 3초마다 한 번씩 발사 모습(조준 자세, 반동, 총구 화염)을 낸다.
    private static readonly bool weaponLog = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-mp-weapon-log") >= 0;
    private static readonly bool weaponTest = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-mp-weapon-test") >= 0;
    private float nextWeaponLog;
    private float nextWeaponTest;

    private void RunDebug()
    {
        if (IsOwner)
        {
            if (weaponTest && source != null && source.Current != null && Time.time >= nextWeaponTest)
            {
                nextWeaponTest = Time.time + 3f;
                source.NotifyFired();
            }
            return;
        }

        if (!weaponLog || Time.time < nextWeaponLog) return;
        nextWeaponLog = Time.time + 2f;
        Debug.Log($"[mp-weapon] {name} 무기={(view != null && view.Current != null ? view.Current.WeaponId : "")} 모델={(transform.Find("WeaponPivot") != null)} 조준={aiming.Value} 재장전={reloading.Value} 발사수={fireCount.Value} yaw={aimYaw.Value:F0} pitch={aimPitch.Value:F0}");
    }
#endif

    private void OnWeaponChanged(FixedString32Bytes previous, FixedString32Bytes now) => ShowWeapon(now.ToString());

    private void ShowWeapon(string id)
    {
        if (view != null) view.ShowRemote(hiddenForDeath ? null : view.FindById(id));
    }

    private void OnFired(int previous, int now)
    {
        if (view != null) view.NotifyFired();
    }
}
