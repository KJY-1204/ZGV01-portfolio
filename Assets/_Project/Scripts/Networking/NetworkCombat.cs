// 접속자 아바타의 전투 동기화 컴포넌트다. 참가자의 사격 요청을 서버(호스트)가 받아 호스트 월드에서 판정하고 데미지를 적용하며, 사격 효과와 플레이어의 체력/사망 표시를 다른 접속자에게 알린다. 서버가 플레이어에게 주는 피해도 여기서 소유자에게 전달한다.
using Unity.Netcode;
using UnityEngine;

public class NetworkCombat : NetworkBehaviour
{
    // 카메라는 캐릭터 뒤에 있으므로 요청한 총구 위치가 아바타에서 이만큼까지 떨어져 있는 것은 허용한다.
    private const float MaxOriginOffset = 6f;
    // 발사 간격 검사는 네트워크 지터를 감안해 이 비율만큼 느슨하게 한다.
    private const float FireIntervalSlack = 0.8f;
    // 맨손 공격 요청: 아바타에서 이 거리(m) 안의 위치여야 하고, 이 간격(초)보다 잦은 요청은 버린다(정상 펀치는 0.5초 이상 걸린다).
    private const float MaxPunchOriginOffset = 3f;
    private const float PunchMinInterval = 0.3f;

    // 이 컴퓨터가 소유한 아바타(소유자 쪽에서만 있다)다.
    public static NetworkCombat Local { get; private set; }

    // 소유자가 쓰는 체력/사망 표시 값이다(다른 접속자는 읽기만 한다).
    private readonly NetworkVariable<float> healthRatio = new NetworkVariable<float>(1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<bool> dead = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public float HealthRatio => healthRatio.Value;
    public bool IsDead => dead.Value;

    private Health localHealth;
    private float nextShotAllowed;
    private float nextPunchAllowed;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // 테스트용: -mp-autofire=무기id 인자가 있으면 소유자가 앞쪽으로 0.5초마다 사격을 요청한다.
    private static readonly string autoFireWeapon = FindArg("-mp-autofire=");
    private float nextAutoFire;

    private static string FindArg(string prefix)
    {
        foreach (string arg in System.Environment.GetCommandLineArgs())
            if (arg.StartsWith(prefix)) return arg.Substring(prefix.Length);
        return "";
    }
#endif

    public override void OnNetworkSpawn()
    {
        if (IsOwner) Local = this;
    }

    public override void OnNetworkDespawn()
    {
        if (Local == this) Local = null;
    }

    private void Update()
    {
        if (!IsSpawned || !IsOwner) return;

        if (localHealth == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) localHealth = player.GetComponent<Health>();
        }
        if (localHealth == null) return;

        float ratio = localHealth.MaxHealth > 0f ? localHealth.CurrentHealth / localHealth.MaxHealth : 0f;
        if (!Mathf.Approximately(healthRatio.Value, ratio)) healthRatio.Value = ratio;
        if (dead.Value != localHealth.IsDead) dead.Value = localHealth.IsDead;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!string.IsNullOrEmpty(autoFireWeapon) && Time.time >= nextAutoFire && !IsServer)
        {
            nextAutoFire = Time.time + 0.5f;
            Transform player = localHealth.transform;
            RequestFire(player.position + Vector3.up * 1.5f, player.forward, autoFireWeapon);
        }
#endif
    }

    // ---- 참가자 → 서버: 사격 요청 ----

    public void RequestFire(Vector3 origin, Vector3 direction, string weaponId) => FireRpc(origin, direction, weaponId);

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void FireRpc(Vector3 origin, Vector3 direction, string weaponId)
    {
        ResolveShot(origin, direction, weaponId);
    }

    // 서버가 요청을 검증하고 호스트 월드에서 판정한다. 잘못된 값이나 너무 잦은 요청은 버린다.
    private void ResolveShot(Vector3 origin, Vector3 direction, string weaponId)
    {
        if (!IsServer || dead.Value) return;
        if (!IsFinite(origin) || !IsFinite(direction) || direction.sqrMagnitude < 0.25f) return;
        if ((origin - transform.position).sqrMagnitude > MaxOriginOffset * MaxOriginOffset) return;

        WeaponEquipment catalogSource = FindFirstObjectByType<WeaponEquipment>();
        WeaponDefinition weapon = catalogSource != null ? catalogSource.FindById(weaponId) : null;
        if (weapon == null || Time.time < nextShotAllowed) return;
        nextShotAllowed = Time.time + weapon.FireInterval * FireIntervalSlack;

        direction.Normalize();
        // 플레이어끼리는 맞지 않게 한다(협동). 호스트 플레이어는 호스트 월드에서 건너뛴다.
        ShotResolver.Result shot = ShotResolver.Trace(new Ray(origin, direction), weapon.Range, ~0, null, true);

        if (shot.didHit && shot.health != null)
        {
            shot.health.ApplyDamage(weapon.Damage * shot.multiplier, direction, shot.headshot);
            HitConfirmedRpc(shot.headshot, shot.health.IsDead);
        }
        NoiseSystem.Emit(transform.position, weapon.NoiseRadius, this);

        ImpactSurface surface = ImpactSurface.Dirt;
        if (shot.didHit && ImpactEffectLibrary.Instance != null) surface = ImpactEffects.SurfaceOf(shot.hit.collider, ImpactEffectLibrary.Instance);
        ShotFxRpc(weapon.WeaponId, origin, shot.didHit, shot.didHit ? shot.hit.point : origin + direction * weapon.Range, shot.didHit ? shot.hit.normal : -direction, (byte)surface);
    }

    // ---- 참가자 → 서버: 맨손 공격 요청 ----

    // 참가자가 펀치의 타격 순간에 서버에 판정을 요청한다. 위치는 발 위치, 방향은 몸 앞쪽, 배율은 허약 같은 상태가 줄인 피해 비율이다.
    public void RequestPunch(Vector3 position, Vector3 forward, float damageScale) => PunchRpc(position, forward, damageScale);

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void PunchRpc(Vector3 position, Vector3 forward, float damageScale)
    {
        if (!IsServer || dead.Value) return;
        if (!IsFinite(position) || !IsFinite(forward) || !float.IsFinite(damageScale)) return;
        if ((position - transform.position).sqrMagnitude > MaxPunchOriginOffset * MaxPunchOriginOffset) return;
        if (Time.time < nextPunchAllowed) return;

        // 총을 든 채로는 맨손 공격을 하지 않는다(클라이언트도 막지만 서버가 다시 확인한다).
        AvatarWeaponSync weaponSync = GetComponent<AvatarWeaponSync>();
        UnarmedPunch punch = GetComponent<UnarmedPunch>();
        if (punch == null || (weaponSync != null && weaponSync.HoldsWeapon)) return;

        forward.y = 0f;
        if (forward.sqrMagnitude < 0.25f) return;
        forward.Normalize();
        nextPunchAllowed = Time.time + PunchMinInterval;

        NoiseSystem.Emit(transform.position, UnarmedPunch.NoiseRadius, this);
        int hits = punch.ResolveForServer(position, forward, damageScale, out Vector3 point, out Vector3 normal, out Collider collider);
        if (hits == 0) return;

        ImpactSurface surface = ImpactSurface.Dirt;
        if (ImpactEffectLibrary.Instance != null) surface = ImpactEffects.SurfaceOf(collider, ImpactEffectLibrary.Instance);
        PunchFxRpc(point, normal, (byte)surface, false);
    }

    // 호스트가 자기 펀치가 맞은 것을 참가자들에게 알린다(UnarmedPunch가 부른다). 호스트 월드의 판정과 효과는 이미 끝난 뒤다.
    public static void BroadcastHostPunch(Vector3 point, Vector3 normal, ImpactSurface surface)
    {
        if (Local == null || !MultiplayerSession.IsHost) return;
        Local.PunchFxRpc(point, normal, (byte)surface, true);
    }

    // 맞은 지점의 타격 효과와 펀치 맞는 소리다. 쏜 쪽이 이미 로컬로 효과를 냈으면(호스트) 그 사람은 건너뛴다.
    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
    private void PunchFxRpc(Vector3 point, Vector3 normal, byte surface, bool ownerAlreadyShown)
    {
        if (ownerAlreadyShown && IsOwner) return;

        ImpactEffects.SpawnSurface((ImpactSurface)surface, point, normal);
        CombatAudioLibrary library = CombatAudioLibrary.Instance;
        if (library != null && library.PunchHits != null && library.PunchHits.Length > 0)
            EffectPool.PlaySoundAt(library.PunchHits[Random.Range(0, library.PunchHits.Length)], point);
    }

    // ---- 서버 → 소유자: 맞았다는 알림 ----

    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
    private void HitConfirmedRpc(bool headshot, bool killed)
    {
        HitscanWeapon weapon = FindFirstObjectByType<HitscanWeapon>();
        if (weapon != null) weapon.NotifyRemoteHit(headshot, killed);
    }

    // ---- 서버 → 모두: 사격 효과(총소리, 탄착) ----

    // 호스트가 자기 무기로 쏜 것을 참가자들에게 알린다(HitscanWeapon이 부른다). 서버 월드의 판정은 이미 끝난 뒤다.
    public static void BroadcastHostShot(string weaponId, Vector3 origin, bool didHit, Vector3 point, Vector3 normal, ImpactSurface surface)
    {
        if (Local == null || !MultiplayerSession.IsHost) return;
        Local.ShotFxRpc(weaponId, origin, didHit, point, normal, (byte)surface);
    }

    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
    private void ShotFxRpc(string weaponId, Vector3 origin, bool didHit, Vector3 point, Vector3 normal, byte surface)
    {
        // 쏜 사람은 이미 자기 화면에서 효과를 봤다.
        if (IsOwner) return;

        WeaponEquipment catalogSource = FindFirstObjectByType<WeaponEquipment>();
        WeaponDefinition weapon = catalogSource != null ? catalogSource.FindById(weaponId) : null;
        if (weapon != null && weapon.FireSounds != null && weapon.FireSounds.Length > 0)
            EffectPool.PlaySoundAt(weapon.FireSounds[Random.Range(0, weapon.FireSounds.Length)], origin, weapon.FireVolume);
        if (didHit) ImpactEffects.SpawnSurface((ImpactSurface)surface, point, normal);
    }

    // ---- 서버 → 소유자: 플레이어가 받는 피해 ----

    // 서버(예: 좀비 AI)가 접속자에게 피해를 준다. 피해는 그 사람의 로컬 Health(방어구 감소율 포함)에 적용된다.
    public static bool DamagePlayer(ulong clientId, float amount, Vector3 hitDirection, bool headshot)
    {
        NetworkManager manager = NetworkManager.Singleton;
        if (manager == null || !manager.IsServer || !manager.ConnectedClients.TryGetValue(clientId, out NetworkClient client) || client.PlayerObject == null) return false;

        NetworkCombat target = client.PlayerObject.GetComponent<NetworkCombat>();
        if (target == null) return false;
        target.ApplyDamageRpc(amount, hitDirection, headshot);
        return true;
    }

    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
    private void ApplyDamageRpc(float amount, Vector3 hitDirection, bool headshot)
    {
        if (localHealth == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) localHealth = player.GetComponent<Health>();
        }
        if (localHealth != null) localHealth.ApplyDamage(amount, hitDirection, headshot);
    }

    private static bool IsFinite(Vector3 v) => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
}
