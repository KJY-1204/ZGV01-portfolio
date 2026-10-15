// 좀비의 네트워크 동기화 컴포넌트다. 서버(호스트)에서는 좀비가 평소처럼 AI로 움직이고, 참가자 쪽 복제본은 AI를 끈 꼭두각시로 바꿔 서버가 알려 주는 위치, 피해/사망, 경계/공격/추적 사건으로 모습과 소리를 낸다.
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Health))]
public class NetworkZombie : NetworkBehaviour
{
    // 서버가 쓰는 현재 추적 중 여부와 사망 여부다(나중에 들어온 접속자도 상태를 알 수 있게 변수로 둔다).
    private readonly NetworkVariable<bool> chasing = new NetworkVariable<bool>(false);
    private readonly NetworkVariable<bool> dead = new NetworkVariable<bool>(false);

    private Health health;
    private ZombieAI ai;
    private ZombieRangedAttack ranged;
    private Vector3 lastHitDirection;
    private bool lastHitHeadshot;

    public override void OnNetworkSpawn()
    {
        health = GetComponent<Health>();
        ai = GetComponent<ZombieAI>();
        ranged = GetComponent<ZombieRangedAttack>();

        if (IsServer)
        {
            health.HitReceived += OnHitReceived;
            health.Damaged += OnDamaged;
            if (ranged != null) ranged.ArrowFired += OnArrowFired;
            if (ai != null)
            {
                ai.Alerted += OnAlerted;
                ai.Attacked += OnAttacked;
            }
            return;
        }

        BecomePuppet();
        chasing.OnValueChanged += (_, now) => { if (ai != null) ai.RemoteChasing(now); };
        if (ai != null) ai.RemoteChasing(chasing.Value);
        // 이미 죽어 있는 좀비를 늦게 본 접속자는 바로 쓰러진 모습으로 둔다.
        if (dead.Value) health.ApplyReplicatedDamage(health.MaxHealth, 0f, Vector3.zero, false);
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer || health == null) return;
        health.HitReceived -= OnHitReceived;
        health.Damaged -= OnDamaged;
        if (ranged != null) ranged.ArrowFired -= OnArrowFired;
        if (ai != null)
        {
            ai.Alerted -= OnAlerted;
            ai.Attacked -= OnAttacked;
        }
    }

    // 참가자 쪽: 길찾기와 AI, 서버만 하는 일을 끄고 위치는 네트워크 값을 따른다.
    private void BecomePuppet()
    {
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = false;
        if (ai != null) ai.BecomePuppet();

        ZombieSunBurn sunBurn = GetComponent<ZombieSunBurn>();
        if (sunBurn != null) sunBurn.enabled = false;
        ZombieRangedAttack ranged = GetComponent<ZombieRangedAttack>();
        if (ranged != null) ranged.enabled = false;

        // 시체는 사격을 막지 않도록 콜라이더를 끈다(서버에서는 AI가 한다).
        health.Died += () =>
        {
            Collider collider = GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
        };
    }

    private void Update()
    {
        if (!IsSpawned || !IsServer || ai == null) return;
        if (chasing.Value != ai.IsChasing) chasing.Value = ai.IsChasing;
    }

    // ---- 서버 → 참가자 ----

    private void OnHitReceived(float amount, Vector3 direction, bool headshot)
    {
        lastHitDirection = direction;
        lastHitHeadshot = headshot;
    }

    private void OnDamaged(float amount, float current)
    {
        Vector3 direction = lastHitDirection;
        bool headshot = lastHitHeadshot;
        lastHitDirection = Vector3.zero;
        lastHitHeadshot = false;
        if (current <= 0f) dead.Value = true;
        DamagedRpc(amount, current, direction, headshot);
    }

    [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
    private void DamagedRpc(float amount, float current, Vector3 direction, bool headshot)
    {
        health.ApplyReplicatedDamage(amount, current, direction, headshot);
    }

    private void OnAlerted() => AlertedRpc();

    [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
    private void AlertedRpc()
    {
        if (ai != null) ai.RemoteAlerted();
    }

    // 궁수의 화살: 서버가 날린 화살의 시작 위치와 속도를 알려 참가자 화면에도 같은 화살이 날아가게 한다(피해는 서버의 화살만 준다).
    private void OnArrowFired(Vector3 origin, Vector3 velocity) => ArrowRpc(origin, velocity);

    [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
    private void ArrowRpc(Vector3 origin, Vector3 velocity)
    {
        if (ranged != null) ranged.SpawnVisualArrow(origin, velocity);
    }

    private void OnAttacked() => AttackedRpc(ai.LastAttackVariant);

    [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
    private void AttackedRpc(int variant)
    {
        if (ai != null) ai.RemoteAttacked(variant);
    }
}
