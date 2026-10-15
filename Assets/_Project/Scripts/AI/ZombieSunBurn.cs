// 낮이 되면 좀비에 불을 붙여 일정 시간 뒤 죽게 하는 컴포넌트다. 그늘이나 실내는 따지지 않고, 불로 죽을 때는 전리품을 떨어뜨리지 않는다.
using UnityEngine;

[RequireComponent(typeof(Health))]
public class ZombieSunBurn : MonoBehaviour
{
    [SerializeField] private GameObject burnEffect;
    // 낮이 된 뒤 불이 붙기까지의 무작위 지연(초)이다. 새벽에 좀비가 한꺼번에 죽지 않게 흩뜨린다.
    [SerializeField] private float minIgniteDelay = 1f;
    [SerializeField] private float maxIgniteDelay = 30f;
    // 불이 붙은 뒤 죽기까지 걸리는 시간(초)과 피해를 주는 간격(초)이다.
    [SerializeField] private float burnSeconds = 6f;
    [SerializeField] private float tickSeconds = 0.5f;

    private Health health;
    private ZombieLootDropper lootDropper;
    private float igniteAt = -1f;
    private float burnEnd;
    private float nextTick;
    private GameObject effect;

    public bool IsBurning { get; private set; }

    private void Awake()
    {
        health = GetComponent<Health>();
        lootDropper = GetComponent<ZombieLootDropper>();
        health.Died += OnDied;
    }

    private void OnDestroy()
    {
        if (health != null) health.Died -= OnDied;
    }

    private void Update()
    {
        if (health.IsDead) return;

        if (!IsBurning)
        {
            // 시계가 없는 씬은 낮 규칙을 쓰지 않는다. 지연 중에 밤이 오면 취소한다.
            bool daytime = GameClock.Current != null && !GameClock.Current.IsNight;
            if (!daytime)
            {
                igniteAt = -1f;
                return;
            }
            if (igniteAt < 0f) igniteAt = Time.time + Random.Range(minIgniteDelay, Mathf.Max(minIgniteDelay, maxIgniteDelay));
            if (Time.time >= igniteAt) Ignite();
            return;
        }

        if (Time.time < nextTick) return;
        nextTick += tickSeconds;
        Tick();
    }

    private void Ignite()
    {
        IsBurning = true;
        burnEnd = Time.time + burnSeconds;
        nextTick = Time.time + tickSeconds;

        if (burnEffect == null) return;
        Bounds bounds = BodyBounds();
        effect = Instantiate(burnEffect, bounds.center, Quaternion.identity, transform);
        // 이펙트는 키 1.8m 기준으로 만들어 두었으므로 체격에 맞춰 키운다.
        effect.transform.localScale *= Mathf.Max(0.5f, bounds.size.y / 1.8f) / Mathf.Max(0.0001f, transform.lossyScale.y);
    }

    private void Tick()
    {
        float damage = Time.time >= burnEnd
            ? health.CurrentHealth
            : ZombieSunBurnRule.Damage(health.MaxHealth, burnSeconds, tickSeconds);
        // 마지막 일격이 불이면 죽으면서 떨어뜨릴 전리품을 미리 막는다(죽음 이벤트가 피해 안에서 바로 불린다).
        if (ZombieSunBurnRule.IsLethal(health.CurrentHealth, damage) && lootDropper != null) lootDropper.SuppressLoot();
        health.ApplyTrueDamage(damage);
    }

    private void OnDied()
    {
        if (effect == null) return;
        foreach (ParticleSystem system in effect.GetComponentsInChildren<ParticleSystem>())
            system.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        Destroy(effect, 2f);
    }

    private Bounds BodyBounds()
    {
        Bounds bounds = new Bounds(transform.position + Vector3.up, Vector3.one);
        bool first = true;
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            if (renderer is ParticleSystemRenderer) continue;
            if (first) { bounds = renderer.bounds; first = false; }
            else bounds.Encapsulate(renderer.bounds);
        }
        return bounds;
    }
}
