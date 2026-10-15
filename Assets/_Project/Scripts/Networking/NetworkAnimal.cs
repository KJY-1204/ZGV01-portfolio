// 동물과 맹수의 네트워크 동기화 컴포넌트다. 서버(호스트)에서는 평소처럼 AI로 움직이고, 참가자 쪽 복제본은 AI를 끈 꼭두각시로 바꿔 서버가 알려 주는 상태, 애니메이션, 피해/사망, 경계/공격 사건으로 모습과 소리를 낸다.
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class NetworkAnimal : NetworkBehaviour
{
    // 서버가 쓰는 현재 상태와 마지막 애니메이션이다(나중에 들어온 접속자도 지금 모습을 알 수 있게 변수로 둔다).
    private readonly NetworkVariable<int> state = new NetworkVariable<int>(0);
    private readonly NetworkVariable<FixedString64Bytes> animation = new NetworkVariable<FixedString64Bytes>();
    private readonly NetworkVariable<bool> dead = new NetworkVariable<bool>(false);

    private Health health;
    private IAnimalNetworkSource source;

    public override void OnNetworkSpawn()
    {
        health = GetComponent<Health>();
        source = GetComponent<IAnimalNetworkSource>();
        if (source == null) return;

        if (IsServer)
        {
            health.Damaged += OnDamaged;
            source.Alarmed += OnAlarmed;
            source.Attacked += OnAttacked;
            source.AnimationPlayed += OnAnimationPlayed;
            return;
        }

        source.BecomePuppet();
        state.OnValueChanged += (_, now) => source.RemoteState(now);
        source.RemoteState(state.Value);
        // 늦게 본 접속자는 서버가 마지막으로 재생한 모습에서 시작한다.
        if (animation.Value.Length > 0) source.RemoteAnimation(animation.Value.ToString(), false);
        // 이미 죽어 있는 동물을 늦게 본 접속자는 바로 쓰러진 모습으로 둔다.
        if (dead.Value) health.ApplyReplicatedDamage(health.MaxHealth, 0f, Vector3.zero, false);
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer || source == null) return;
        health.Damaged -= OnDamaged;
        source.Alarmed -= OnAlarmed;
        source.Attacked -= OnAttacked;
        source.AnimationPlayed -= OnAnimationPlayed;
    }

    private void Update()
    {
        if (!IsSpawned || !IsServer || source == null) return;
        if (state.Value != source.StateValue) state.Value = source.StateValue;
    }

    // ---- 서버 → 참가자 ----

    private void OnDamaged(float amount, float current)
    {
        if (current <= 0f) dead.Value = true;
        DamagedRpc(amount, current);
    }

    [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
    private void DamagedRpc(float amount, float current)
    {
        health.ApplyReplicatedDamage(amount, current, Vector3.zero, false);
    }

    private void OnAlarmed() => AlarmedRpc();

    [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
    private void AlarmedRpc()
    {
        if (source != null) source.RemoteAlarmed();
    }

    private void OnAttacked() => AttackedRpc();

    [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
    private void AttackedRpc()
    {
        if (source != null) source.RemoteAttacked();
    }

    private void OnAnimationPlayed(string stateName, bool force)
    {
        animation.Value = stateName;
        AnimationRpc(stateName, force);
    }

    [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
    private void AnimationRpc(FixedString64Bytes stateName, bool force)
    {
        if (source != null) source.RemoteAnimation(stateName.ToString(), force);
    }
}
