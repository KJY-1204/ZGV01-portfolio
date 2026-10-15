// 설치된 건축물의 내구도다. 좀비가 부술 수 있고 체력이 0이 되면 소리와 함께 사라진다(플레이어의 총알/주먹은 피해를 주지 않는다).
using System;
using System.Collections.Generic;
using UnityEngine;

public class BuildableHealth : MonoBehaviour
{
    // 좀비가 막힌 길 앞에서 부술 대상을 빠르게 찾을 수 있게 활성 인스턴스를 모아 둔다.
    private static readonly List<BuildableHealth> active = new List<BuildableHealth>();
    public static IReadOnlyList<BuildableHealth> Active => active;

    private BuildableDefinition definition;
    // 이 구조물을 동시에 공격할 수 있는 좀비 자리다(문/벽 한 조각 앞에는 한꺼번에 몇 마리밖에 서지 못한다).
    private readonly List<UnityEngine.Object> attackers = new List<UnityEngine.Object>();
    private const float SlotWidth = 1.0f;
    private Bounds worldBounds;
    private bool hasBounds;

    public float Max { get; private set; }
    public float Current { get; private set; }
    public bool IsBroken { get; private set; }
    // 좀비가 다가가야 할 위치를 구할 때 쓰는 월드 경계다.
    public Bounds WorldBounds => worldBounds;
    public bool HasBounds => hasBounds;

    // 동시에 공격할 수 있는 수다. 가장 긴 변 1m당 한 자리, 1~4자리다.
    public int MaxAttackers
    {
        get
        {
            if (!hasBounds) return 1;
            float longSide = Mathf.Max(worldBounds.size.x, worldBounds.size.z);
            return Mathf.Clamp(Mathf.FloorToInt(longSide / SlotWidth), 1, 4);
        }
    }

    // 공격 자리를 하나 차지한다. 이미 가졌으면 true, 자리가 없으면 false(기다려야 한다).
    public bool TryClaimAttacker(UnityEngine.Object who)
    {
        attackers.RemoveAll(a => a == null);
        if (attackers.Contains(who)) return true;
        if (attackers.Count >= MaxAttackers) return false;
        attackers.Add(who);
        return true;
    }

    public void ReleaseAttacker(UnityEngine.Object who) => attackers.Remove(who);

    // 어느 건축물이든 피해나 수리로 체력이 바뀌면 알린다. 서버가 접속자에게 체력을 전하는 데 쓴다.
    public static event Action<BuildableHealth> Modified;

    public event Action<float, float> Damaged;
    // 체력이 바뀔 때마다(초기화, 피해, 수리) 알린다. 피해 표시가 구독한다.
    public event Action Changed;
    public event Action Broken;

    private void OnEnable() => active.Add(this);
    private void OnDisable() => active.Remove(this);

    public void Initialize(BuildableDefinition buildable, float current)
    {
        definition = buildable;
        Max = buildable.MaxHealth;
        Current = current < 0f ? Max : Mathf.Clamp(current, 1f, Max);
        hasBounds = BuildableInstance.TryGetColliderBounds(gameObject, out worldBounds);
        Changed?.Invoke();
    }

    // hitPoint는 맞은 자리다(먼지를 일으킨다). 모르면 경계 중심을 쓴다.
    public void ApplyDamage(float amount, Vector3? hitPoint = null)
    {
        if (IsBroken || amount <= 0f) return;

        Current = Mathf.Max(0f, Current - amount);
        Damaged?.Invoke(amount, Current);
        Changed?.Invoke();
        Modified?.Invoke(this);

        if (hasBounds && Current > 0f)
        {
            Vector3 at = hitPoint ?? worldBounds.center;
            BuildableDebris.SpawnDust(new Bounds(at, Vector3.one * 0.3f), 4);
        }

        if (Current > 0f)
        {
            PlaySound(definition != null ? definition.HitSound : null);
            return;
        }

        IsBroken = true;
        Broken?.Invoke();
        // 이 조각이 받치던 건축물(벽에 붙은 가구, 위층 조각)은 받침을 잃으면 함께 무너진다.
        BuildableSupport.Collapse(GetComponent<BuildableInstance>());
        Remove();
    }

    // 서버가 알린 체력을 참가자 쪽 복제본에 반영한다(깎였으면 먼지와 피격 소리도 낸다). 무너짐은 별도로 알려 준다.
    public void ApplyReplicated(float current)
    {
        if (IsBroken) return;

        float next = Mathf.Clamp(current, 0f, Max);
        float lost = Current - next;
        Current = next;
        if (lost > 0f)
        {
            Damaged?.Invoke(lost, Current);
            if (hasBounds) BuildableDebris.SpawnDust(new Bounds(worldBounds.center, Vector3.one * 0.3f), 4);
            PlaySound(definition != null ? definition.HitSound : null);
        }
        Changed?.Invoke();
    }

    // 받침을 잃어 무너질 때 불린다. delay초 뒤 파편을 튀기며 무너진다(연쇄 붕괴가 순서대로 보이도록).
    public void Collapse(float delay)
    {
        if (IsBroken) return;
        IsBroken = true;
        Broken?.Invoke();
        Remove(delay);
    }

    // 무너지는 순간(파편이 튀는 때) 부서지는 소리를 낸다.
    public void PlayBreakSound() => PlaySound(definition != null ? definition.BreakSound : null);

    // 파편을 튀기며 무너뜨린다(delay초 뒤). 한 번만 처리된다.
    private void Remove(float delay = 0f)
    {
        BuildableInstance instance = GetComponent<BuildableInstance>();
        if (instance != null) BuildableDebris.Crumble(instance, delay);
        else
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }

    // 체력을 amount만큼 채운다(최대치까지). 수리 소리를 낸다.
    public void Repair(float amount)
    {
        if (IsBroken || amount <= 0f || Current >= Max) return;

        Current = Mathf.Min(Max, Current + amount);
        Changed?.Invoke();
        Modified?.Invoke(this);

        AudioClip clip = definition != null ? definition.HitSound.PickClip() : null;
        if (clip != null) EffectPool.PlaySoundAt(clip, hasBounds ? worldBounds.center : transform.position, 0.6f, 1.15f);
    }

    private void PlaySound(SoundGroup group)
    {
        if (group == null) return;
        AudioClip clip = group.PickClip();
        if (clip == null) return;
        Vector3 at = hasBounds ? worldBounds.center : transform.position;
        EffectPool.PlaySoundAt(clip, at, group.volume, group.PickPitch());
    }
}
