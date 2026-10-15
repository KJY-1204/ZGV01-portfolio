// 월드 공유 상태(자원 노드와 지형 나무가 다 채집돼 사라진 것)를 서버가 모든 접속자에게 알리는 컴포넌트다. 서버가 게임 씬을 연 뒤 한 번 소환하고, 늦게 들어온 접속자는 지금까지 사라진 목록을 한 번에 받는다.
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Unity.Netcode;
using UnityEngine;

public class WorldSync : NetworkBehaviour
{
    // 사라진 노드를 위치로 찾을 때 허용하는 거리(m)다. 노드는 씬에 고정돼 있어 두 컴퓨터에서 같은 자리다.
    private const float NodeMatchRadius = 0.6f;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            ResourceNode.AnyDepleted += OnNodeDepleted;
            TerrainTreeGather.TreeDepleted += OnTreeDepleted;
        }
        else
        {
            RequestStateRpc();
        }
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // 테스트용: -mp-world-log 인자가 있으면 참가자가 보는 공유 월드 요약(좀비, 전리품, 사라진 나무/노드)을 2초마다 로그로 남긴다.
    private static readonly bool worldLog = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-mp-world-log") >= 0;
    // 테스트용: -mp-animal-log 인자가 있으면 이 접속자가 보는 동물/맹수 요약(수, 꼭두각시 수, 사망, 상태별 수, 체력 줄어든 수)을 2초마다 로그로 남긴다.
    private static readonly bool animalLog = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-mp-animal-log") >= 0;
    private float nextLog;
    private float nextAnimalLog;

    private void Update()
    {
        if (animalLog && IsSpawned && Time.time >= nextAnimalLog)
        {
            nextAnimalLog = Time.time + 2f;
            int animals = 0, puppets = 0, deadAnimals = 0, hurt = 0;
            var states = new System.Text.StringBuilder();
            foreach (NetworkAnimal animal in FindObjectsByType<NetworkAnimal>(FindObjectsSortMode.None))
            {
                animals++;
                IAnimalNetworkSource source = animal.GetComponent<IAnimalNetworkSource>();
                Health health = animal.GetComponent<Health>();
                if (source != null && source.IsPuppet) puppets++;
                if (health != null && health.IsDead) deadAnimals++;
                if (health != null && health.CurrentHealth < health.MaxHealth) hurt++;
                states.Append(animal.name.Replace("(Clone)", "")).Append(':').Append(source != null ? source.StateValue : -1).Append(health != null && health.IsDead ? "(사망)" : "").Append(' ');
            }
            Debug.Log($"[mp-animal] 동물={animals} 꼭두각시={puppets} 사망={deadAnimals} 피해입음={hurt} 상태={states}");
        }

        if (!worldLog || !IsSpawned || Time.time < nextLog) return;
        nextLog = Time.time + 2f;

        int zombies = 0, dead = 0, hits = 0;
        foreach (NetworkZombie zombie in FindObjectsByType<NetworkZombie>(FindObjectsSortMode.None))
        {
            zombies++;
            Health health = zombie.GetComponent<Health>();
            if (health != null && health.IsDead) dead++;
            if (health != null && health.CurrentHealth < health.MaxHealth) hits++;
        }
        int loot = FindObjectsByType<LootPickup>(FindObjectsSortMode.None).Length;
        int trees = TerrainTreeGather.Instance != null ? TerrainTreeGather.Instance.DepletedTrees.Count : -1;
        int nodes = 0;
        foreach (ResourceNode node in ResourceNode.Active) if (node != null && node.TerrainTreeIndex < 0 && !node.HasYield) nodes++;
        Debug.Log($"[mp-world] 좀비={zombies} 사망={dead} 피해입음={hits} 전리품={loot} 사라진나무={trees} 사라진노드={nodes} 보기전용화살={ZombieProjectile.VisualSpawned}");
    }
#endif

    public override void OnNetworkDespawn()
    {
        ResourceNode.AnyDepleted -= OnNodeDepleted;
        TerrainTreeGather.TreeDepleted -= OnTreeDepleted;
    }

    // ---- 서버 → 참가자: 실시간 변화 ----

    private void OnNodeDepleted(ResourceNode node)
    {
        // 지형 나무 대역은 나무 번호 경로로 알리므로 건너뛴다.
        if (node == null || node.TerrainTreeIndex >= 0) return;
        NodeDepletedRpc(node.transform.position);
    }

    private void OnTreeDepleted(int tree) => TreeDepletedRpc(tree);

    [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
    private void NodeDepletedRpc(Vector3 position) => ApplyNodeDepleted(position);

    [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
    private void TreeDepletedRpc(int tree)
    {
        if (TerrainTreeGather.Instance != null) TerrainTreeGather.Instance.ApplyRemoteTreeDepleted(tree);
    }

    private static void ApplyNodeDepleted(Vector3 position)
    {
        ResourceNode best = null;
        float bestSqr = NodeMatchRadius * NodeMatchRadius;
        foreach (ResourceNode node in ResourceNode.Active)
        {
            if (node == null || node.TerrainTreeIndex >= 0 || !node.HasYield) continue;
            float sqr = (node.transform.position - position).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; best = node; }
        }
        if (best != null) best.ApplyRemoteDepleted();
    }

    // ---- 늦게 들어온 접속자: 지금까지 사라진 목록 ----

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestStateRpc(RpcParams rpcParams = default)
    {
        var nodes = new StringBuilder();
        foreach (ResourceNode node in ResourceNode.Active)
        {
            if (node == null || node.TerrainTreeIndex >= 0 || node.HasYield) continue;
            Vector3 p = node.transform.position;
            if (nodes.Length > 0) nodes.Append(';');
            nodes.Append(p.x.ToString("F2", CultureInfo.InvariantCulture)).Append(',').Append(p.y.ToString("F2", CultureInfo.InvariantCulture)).Append(',').Append(p.z.ToString("F2", CultureInfo.InvariantCulture));
        }

        var trees = new StringBuilder();
        if (TerrainTreeGather.Instance != null)
        {
            foreach (int index in TerrainTreeGather.Instance.DepletedTrees)
            {
                if (trees.Length > 0) trees.Append(',');
                trees.Append(index);
            }
        }

        StateRpc(nodes.ToString(), trees.ToString(), RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void StateRpc(string nodes, string trees, RpcParams rpcParams = default)
    {
        if (!string.IsNullOrEmpty(nodes))
        {
            foreach (string entry in nodes.Split(';'))
            {
                string[] parts = entry.Split(',');
                if (parts.Length != 3) continue;
                if (float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
                    && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y)
                    && float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
                    ApplyNodeDepleted(new Vector3(x, y, z));
            }
        }

        if (!string.IsNullOrEmpty(trees) && TerrainTreeGather.Instance != null)
        {
            foreach (string entry in trees.Split(','))
                if (int.TryParse(entry, out int index)) TerrainTreeGather.Instance.ApplyRemoteTreeDepleted(index);
        }
    }
}
