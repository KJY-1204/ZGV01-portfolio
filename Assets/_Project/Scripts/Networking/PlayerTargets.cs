// 좀비 AI와 스포너가 "플레이어"를 찾을 때 쓰는 정적 도우미다. 싱글플레이와 호스트의 로컬 플레이어에 더해, 호스트 월드에서는 참가자 아바타도 목표 후보로 본다. 피해도 대상이 로컬 플레이어인지 아바타인지에 따라 알맞게 전달한다.
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public static class PlayerTargets
{
    private static readonly List<Transform> buffer = new List<Transform>();

    // 지금 목표가 될 수 있는(살아 있는) 플레이어들이다. 싱글플레이면 로컬 플레이어 하나다.
    public static List<Transform> Alive()
    {
        buffer.Clear();

        GameObject local = GameObject.FindGameObjectWithTag("Player");
        if (local != null)
        {
            Health health = local.GetComponent<Health>();
            if (health == null || !health.IsDead) buffer.Add(local.transform);
        }

        NetworkManager manager = NetworkManager.Singleton;
        if (manager != null && manager.IsServer)
        {
            foreach (KeyValuePair<ulong, NetworkClient> pair in manager.ConnectedClients)
            {
                // 호스트 자신의 아바타는 로컬 플레이어가 이미 있으므로 건너뛴다.
                if (pair.Key == manager.LocalClientId || pair.Value.PlayerObject == null) continue;
                NetworkCombat combat = pair.Value.PlayerObject.GetComponent<NetworkCombat>();
                if (combat != null && combat.IsDead) continue;
                buffer.Add(pair.Value.PlayerObject.transform);
            }
        }
        return buffer;
    }

    // from에서 가장 가까운 살아 있는 플레이어(없으면 null)다.
    public static Transform Nearest(Vector3 from)
    {
        Transform best = null;
        float bestSqr = float.MaxValue;
        foreach (Transform candidate in Alive())
        {
            float sqr = (candidate.position - from).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; best = candidate; }
        }
        return best;
    }

    // 살아 있는 플레이어 중 무작위 하나(없으면 null)다.
    public static Transform Random()
    {
        List<Transform> players = Alive();
        return players.Count == 0 ? null : players[UnityEngine.Random.Range(0, players.Count)];
    }

    // 가장 가까운 플레이어까지 거리의 제곱이다(없으면 무한대).
    public static float NearestSqrDistance(Vector3 from)
    {
        Transform nearest = Nearest(from);
        return nearest != null ? (nearest.position - from).sqrMagnitude : float.MaxValue;
    }

    // 목표에게 피해를 준다. 로컬 플레이어는 Health에 바로, 참가자 아바타는 소유자에게 전달한다.
    public static void Damage(Transform target, float amount, Vector3 hitDirection)
    {
        if (target == null) return;

        NetworkObject avatar = target.GetComponentInParent<NetworkObject>();
        if (avatar != null && avatar.IsSpawned && avatar.IsPlayerObject && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer
            && avatar.OwnerClientId != NetworkManager.Singleton.LocalClientId)
        {
            NetworkCombat.DamagePlayer(avatar.OwnerClientId, amount, hitDirection, false);
            return;
        }

        Health health = target.GetComponentInParent<Health>();
        if (health != null) health.ApplyDamage(amount);
    }
}
