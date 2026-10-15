// 구역 안에서 시각에 맞는 수의 좀비를 유지하며 플레이어 시야 밖에 스폰하고, 죽은 좀비와 너무 멀거나 남는 좀비를 치우는 도메인 컴포넌트다.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZombieSpawner : MonoBehaviour
{
    [SerializeField] private ZombieSpawnTable table;
    // 스폰 구역(이 오브젝트 중심의 원)이다. 구역 밖에는 스폰하지 않는다.
    [SerializeField] private float radius = 40f;
    // 밀도 1일 때 동시에 살아 있을 수 있는 최대 수와, 시작할 때 한꺼번에 만드는 수다.
    [SerializeField] private int maxAlive = 6;
    [SerializeField] private int initialCount = 4;
    // 밀도 1일 때 살아 있는 수가 최대보다 적으면 이 간격마다 하나씩 보충한다(밀도가 높으면 더 자주).
    [SerializeField] private float spawnInterval = 8f;
    // 시각(0~24시)에 따른 밀도 배율이다. 최대 수에 곱하고 보충 간격을 나눈다. 밤에 많고 낮에 적게 한다.
    [SerializeField] private AnimationCurve densityByHour = new AnimationCurve(
        new Keyframe(0f, 1.25f), new Keyframe(5f, 1f), new Keyframe(7f, 0.45f), new Keyframe(12f, 0.3f),
        new Keyframe(17f, 0.5f), new Keyframe(20f, 1f), new Keyframe(24f, 1.25f));
    // 스폰 지점은 플레이어 둘레의 고리(가까운 거리~먼 거리)에서 고른다. 가까운 거리보다 가까운 곳과 먼 거리보다 먼 곳에는 만들지 않는다.
    [SerializeField] private float minPlayerDistance = 22f;
    [SerializeField] private float maxPlayerDistance = 45f;
    // 플레이어 화면에 보이는 곳에는 스폰하지 않는다. 화면 가장자리에서 이만큼(뷰포트 비율) 벗어나야 안 보이는 곳으로 친다.
    [SerializeField] private float viewportMargin = 0.08f;
    // 플레이어와 이 거리보다 멀어진 좀비는 제거하고 다른 곳에 다시 스폰한다.
    [SerializeField] private float despawnDistance = 90f;
    // 밀도가 낮아져 남는 좀비는 시야 밖이고 이 거리 이상 떨어진 것부터 이 간격마다 하나씩 치운다(아침에 서서히 줄어든다).
    [SerializeField] private float excessDespawnInterval = 4f;
    [SerializeField] private float excessDespawnDistance = 20f;
    // 죽은 좀비를 이 시간 뒤에 치운다.
    [SerializeField] private float corpseLifetime = 30f;
    [SerializeField] private int placementAttempts = 24;
    // 광산처럼 지형 아래에 있는 실내 구역용이다. 켜면 후보를 지형 윗면이 아니라 이 높이의 바닥에서 찾고, 머리 위에 천장이 있는 곳에만 스폰한다.
    [SerializeField] private bool indoorFloor;
    [SerializeField] private float indoorFloorY;
    private const float IndoorFloorTolerance = 1.5f;
    private const float IndoorRoofCheck = 20f;

    private readonly List<Health> alive = new List<Health>();
    private float nextSpawnTime;
    private float nextExcessDespawnTime;
    private Transform player;

    public int AliveCount => alive.Count;
    // 지금 시각의 밀도 배율과 그에 따른 최대 수다(시험과 디버그용).
    public float Density => Mathf.Max(0f, densityByHour.Evaluate(CurrentHour));
    // 낮에는 좀비가 나오지 않는다(남은 좀비는 불타 죽는다). 시계가 없는 씬은 이 규칙을 쓰지 않는다.
    public int EffectiveMaxAlive => IsDaylight ? 0 : Mathf.Max(0, Mathf.RoundToInt(maxAlive * Density));
    public event Action<GameObject> Spawned;

    private static bool IsDaylight => GameClock.Current != null && !GameClock.Current.IsNight;

    // 시계가 없으면 한낮(12시)으로 본다.
    private static float CurrentHour => GameClock.Current != null ? GameClock.Current.Hour : 12f;

    private void Start()
    {
        // 멀티플레이 참가자의 월드에는 AI를 돌리지 않는다(AI는 서버가 실행한다).
        if (MultiplayerSession.IsClientOnly) return;
        int start = Mathf.Min(initialCount, EffectiveMaxAlive);
        for (int i = 0; i < start; i++) TrySpawn();
        nextSpawnTime = Time.time + SpawnInterval;
    }

    private float SpawnInterval => spawnInterval / Mathf.Max(0.2f, Density);

    private void Update()
    {
        if (MultiplayerSession.IsClientOnly) return;
        DespawnFar();
        DespawnExcess();

        if (alive.Count >= EffectiveMaxAlive || Time.time < nextSpawnTime) return;
        nextSpawnTime = Time.time + SpawnInterval;
        TrySpawn();
    }

    private Transform Player
    {
        get
        {
            if (player == null)
            {
                GameObject found = GameObject.FindGameObjectWithTag("Player");
                if (found != null) player = found.transform;
            }
            return player;
        }
    }

    private void DespawnFar()
    {
        if (Player == null) return;

        for (int i = alive.Count - 1; i >= 0; i--)
        {
            Health zombie = alive[i];
            if (zombie == null)
            {
                alive.RemoveAt(i);
                continue;
            }
            // 멀티플레이에서는 어느 플레이어와도 멀어졌을 때만 치운다.
            if (PlayerTargets.NearestSqrDistance(zombie.transform.position) <= despawnDistance * despawnDistance) continue;

            alive.RemoveAt(i);
            Destroy(zombie.gameObject);
        }
    }

    // 밀도가 낮아져(낮) 최대 수를 넘은 좀비를, 눈에 띄지 않게 시야 밖의 먼 것부터 하나씩 치운다.
    private void DespawnExcess()
    {
        // 낮의 남은 좀비는 조용히 치우지 않고 불타 죽게 둔다.
        if (IsDaylight || Player == null || alive.Count <= EffectiveMaxAlive || Time.time < nextExcessDespawnTime) return;
        nextExcessDespawnTime = Time.time + excessDespawnInterval;

        Health farthest = null;
        float best = excessDespawnDistance * excessDespawnDistance;
        foreach (Health zombie in alive)
        {
            if (zombie == null || zombie.IsDead) continue;
            float sqr = PlayerTargets.NearestSqrDistance(zombie.transform.position);
            if (sqr < best || IsVisibleToPlayer(zombie.transform.position)) continue;
            best = sqr;
            farthest = zombie;
        }

        if (farthest == null) return;
        alive.Remove(farthest);
        Destroy(farthest.gameObject);
    }

    private void TrySpawn()
    {
        if (IsDaylight || table == null || !table.TryPick(CurrentHour, out GameObject prefab)) return;
        if (!TryFindPosition(out Vector3 position)) return;

        // 멀티플레이 호스트에서는 네트워크 오브젝트로 소환한다(부모 없이: 네트워크 오브젝트는 일반 오브젝트 아래에 둘 수 없다).
        bool networked = MultiplayerSession.IsHost && prefab.GetComponent<Unity.Netcode.NetworkObject>() != null;
        GameObject zombie = Instantiate(prefab, position, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), networked ? null : transform);
        Health health = zombie.GetComponent<Health>();
        if (health == null) return;
        if (networked) zombie.GetComponent<Unity.Netcode.NetworkObject>().Spawn(true);

        alive.Add(health);
        health.Died += () => OnZombieDied(health);
        Spawned?.Invoke(zombie);
    }

    // 플레이어 둘레의 고리 안에서 구역 안이고 NavMesh 위이며 플레이어 시야 밖인 지점을 찾는다.
    private bool TryFindPosition(out Vector3 position)
    {
        position = default;
        // 스폰 고리의 중심은 살아 있는 플레이어 중 무작위 한 명이다(멀티플레이에서는 참가자 주변에도 스폰한다).
        Transform reference = PlayerTargets.Random();
        if (reference == null) return false;

        for (int i = 0; i < placementAttempts; i++)
        {
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float distance = UnityEngine.Random.Range(minPlayerDistance, maxPlayerDistance);
            Vector3 candidate = reference.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
            if (Vector3.Distance(new Vector3(candidate.x, transform.position.y, candidate.z), transform.position) > radius) continue;
            // 높낮이가 있는 지형에서는 후보 높이를 지면에 맞춰야 NavMesh 표본이 잡힌다.
            candidate = indoorFloor ? new Vector3(candidate.x, indoorFloorY, candidate.z) : GroundSnap.OnGround(candidate);
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, NavMesh.AllAreas)) continue;
            if (indoorFloor && !IsIndoor(hit.position)) continue;
            if (PlayerTargets.NearestSqrDistance(hit.position) < minPlayerDistance * minPlayerDistance) continue;
            if (IsVisibleToPlayer(hit.position)) continue;

            position = hit.position;
            return true;
        }
        return false;
    }

    // 실내 바닥 높이 근처이고 머리 위에 천장이 있으면 실내다(광장이나 지형 위 NavMesh는 걸러낸다).
    private bool IsIndoor(Vector3 point)
    {
        if (Mathf.Abs(point.y - indoorFloorY) > IndoorFloorTolerance) return false;
        return Physics.Raycast(point + Vector3.up * 0.5f, Vector3.up, IndoorRoofCheck, ~0, QueryTriggerInteraction.Ignore);
    }

    // 플레이어 카메라 화면 안에 있고 사이를 가리는 것이 없으면 보이는 곳이다. 발/몸통/머리 높이 중 한 곳이라도 트여 있으면 보이는 것으로 본다.
    private bool IsVisibleToPlayer(Vector3 point)
    {
        Camera cam = Camera.main;
        if (cam == null) return false;

        foreach (float height in SampleHeights)
        {
            Vector3 body = point + Vector3.up * height;
            Vector3 viewport = cam.WorldToViewportPoint(body);
            bool inFrustum = viewport.z > 0f
                && viewport.x > -viewportMargin && viewport.x < 1f + viewportMargin
                && viewport.y > -viewportMargin && viewport.y < 1f + viewportMargin;
            if (inFrustum && HasClearLine(cam.transform.position, body)) return true;
        }
        return false;
    }

    private static readonly float[] SampleHeights = { 0.2f, 1.2f, 1.75f };

    // 두 점 사이에 벽이나 지형이 없는지 본다(플레이어와 좀비 자신은 가리지 않는다).
    private bool HasClearLine(Vector3 from, Vector3 to)
    {
        Vector3 toTarget = to - from;
        RaycastHit[] hits = Physics.RaycastAll(from, toTarget.normalized, toTarget.magnitude, ~0, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            if (Player != null && hit.collider.transform.IsChildOf(Player)) continue;
            if (hit.collider.GetComponentInParent<ZombieAI>() != null) continue;
            return false;
        }
        return true;
    }

    // 죽은 좀비는 살아 있는 수에서 빼서 보충이 되게 하고, 시간이 지나면 시체를 치운다.
    private void OnZombieDied(Health zombie)
    {
        alive.Remove(zombie);
        if (zombie != null) Destroy(zombie.gameObject, corpseLifetime);
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // 성능 측정용: 시야/구역/밀도 제한 없이 center 둘레 고리에 count마리를 만든다. alive 목록에 넣지 않아 보충·정리 규칙이 건드리지 않는다.
    public int DebugSpawnAround(Vector3 center, int count, float minDistance, float maxDistance)
    {
        if (table == null) return 0;
        int made = 0;
        for (int tries = 0; made < count && tries < count * 12; tries++)
        {
            if (!table.TryPick(CurrentHour, out GameObject prefab)) break;
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float distance = UnityEngine.Random.Range(minDistance, maxDistance);
            Vector3 candidate = GroundSnap.OnGround(center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance);
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, NavMesh.AllAreas)) continue;

            bool networked = MultiplayerSession.IsHost && prefab.GetComponent<Unity.Netcode.NetworkObject>() != null;
            GameObject zombie = Instantiate(prefab, hit.position, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), networked ? null : transform);
            if (networked) zombie.GetComponent<Unity.Netcode.NetworkObject>().Spawn(true);
            made++;
        }
        return made;
    }
#endif

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
