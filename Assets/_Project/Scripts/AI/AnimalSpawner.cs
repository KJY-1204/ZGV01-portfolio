// 구역 안에 사냥감 동물을 정해 둔 수만큼 유지하며 플레이어에서 떨어진 NavMesh 위에 스폰하는 도메인 컴포넌트다. 죽은 동물은 살아 있는 수에서 빼서 다시 채운다.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AnimalSpawner : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        public GameObject prefab;
        public float weight = 1f;
    }

    [SerializeField] private Entry[] animals = Array.Empty<Entry>();
    // 스폰 구역(이 오브젝트 중심의 원)이다. 구역 밖에는 스폰하지 않는다.
    [SerializeField] private float radius = 45f;
    [SerializeField] private int maxAlive = 6;
    [SerializeField] private int initialCount = 5;
    // 살아 있는 수가 최대보다 적으면 이 간격마다 하나씩 보충한다.
    [SerializeField] private float respawnInterval = 20f;
    // 플레이어에게서 이 거리 안에는 새로 만들지 않는다(눈앞에서 갑자기 나타나지 않게).
    [SerializeField] private float minPlayerDistance = 14f;
    [SerializeField] private int placementAttempts = 20;

    private readonly List<Health> alive = new List<Health>();
    private float nextSpawnTime;

    public int AliveCount => alive.Count;
    public event Action<GameObject> Spawned;

    private void Start()
    {
        // 멀티플레이 참가자의 월드에는 AI를 돌리지 않는다(AI는 서버가 실행한다).
        if (MultiplayerSession.IsClientOnly) return;
        for (int i = 0; i < Mathf.Min(initialCount, maxAlive); i++) TrySpawn();
        nextSpawnTime = Time.time + respawnInterval;
    }

    private void Update()
    {
        if (MultiplayerSession.IsClientOnly) return;
        alive.RemoveAll(h => h == null);
        if (alive.Count >= maxAlive || Time.time < nextSpawnTime) return;

        nextSpawnTime = Time.time + respawnInterval;
        TrySpawn();
    }

    public bool TrySpawn()
    {
        if (!TryPickPrefab(out GameObject prefab)) return false;
        if (!TryFindPosition(out Vector3 position)) return false;

        // 멀티플레이 호스트에서는 네트워크 오브젝트로 소환한다(부모 없이: 네트워크 오브젝트는 일반 오브젝트 아래에 둘 수 없다).
        bool networked = MultiplayerSession.IsHost && prefab.GetComponent<Unity.Netcode.NetworkObject>() != null;
        GameObject animal = Instantiate(prefab, position, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), networked ? null : transform);
        Health health = animal.GetComponent<Health>();
        if (health == null) return false;
        if (networked) animal.GetComponent<Unity.Netcode.NetworkObject>().Spawn(true);

        alive.Add(health);
        health.Died += () => alive.Remove(health);
        Spawned?.Invoke(animal);
        return true;
    }

    // 이 스포너의 목록에 있는 프리팹인지다(저장을 불러올 때 어느 스포너가 맡을지 정한다).
    public bool Owns(GameObject prefab)
    {
        foreach (Entry entry in animals)
            if (entry != null && entry.prefab == prefab) return true;
        return false;
    }

    // 밖에서 만든 동물(저장 불러오기)을 이 스포너의 살아 있는 수에 넣고 아래에 둔다. 그래야 최대 수를 넘겨 더 만들지 않는다.
    public void Adopt(GameObject animal)
    {
        Health health = animal.GetComponent<Health>();
        if (health == null) return;

        // 소환된 네트워크 오브젝트는 일반 오브젝트 아래로 옮기지 않는다.
        Unity.Netcode.NetworkObject networkObject = animal.GetComponent<Unity.Netcode.NetworkObject>();
        if (networkObject == null || !networkObject.IsSpawned) animal.transform.SetParent(transform, true);
        alive.Add(health);
        health.Died += () => alive.Remove(health);
    }

    private bool TryPickPrefab(out GameObject prefab)
    {
        prefab = null;
        float total = 0f;
        foreach (Entry entry in animals)
            if (entry != null && entry.prefab != null) total += Mathf.Max(0f, entry.weight);
        if (total <= 0f) return false;

        float roll = UnityEngine.Random.value * total;
        float cumulative = 0f;
        foreach (Entry entry in animals)
        {
            if (entry == null || entry.prefab == null) continue;
            cumulative += Mathf.Max(0f, entry.weight);
            if (roll <= cumulative)
            {
                prefab = entry.prefab;
                return true;
            }
        }
        return false;
    }

    private bool TryFindPosition(out Vector3 position)
    {
        position = default;
        for (int i = 0; i < placementAttempts; i++)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * radius;
            Vector3 candidate = GroundSnap.OnGround(transform.position + new Vector3(offset.x, 0f, offset.y));
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, NavMesh.AllAreas)) continue;
            // 멀티플레이에서는 어느 플레이어에게도 가까우면 안 된다.
            if (PlayerTargets.NearestSqrDistance(hit.position) < minPlayerDistance * minPlayerDistance) continue;

            position = hit.position;
            return true;
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.9f, 0.4f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
