// 파티클 이펙트와 일회성 효과음을 풀링해 재사용하는 정적 도우미다. 연사로 이펙트가 쌓여도 생성/파괴 비용과 오브젝트 수가 일정하게 유지된다.
using System.Collections.Generic;
using UnityEngine;

public static class EffectPool
{
    // 프리팹 하나당 동시에 살아 있을 수 있는 이펙트 수. 넘으면 가장 오래된 것을 즉시 재활용한다(총알 구멍이 무한히 쌓이지 않는다).
    private const int MaxActivePerPrefab = 32;
    private const int SoundVoiceCount = 16;
    private const float MinLifetime = 0.5f;
    private const float MaxLifetime = 15f;

    private sealed class ActiveEffect
    {
        public GameObject Instance;
        public float ReturnAt;
    }

    private sealed class PoolEntry
    {
        public readonly Queue<GameObject> Idle = new Queue<GameObject>();
        public readonly List<ActiveEffect> Active = new List<ActiveEffect>();
        public float Lifetime;
    }

    // 풀 오브젝트의 부모이자 반환 시간을 확인하는 보이지 않는 실행기다.
    private sealed class Runner : MonoBehaviour
    {
        private void Update()
        {
            EffectPool.ReturnExpired();
        }
    }

    private static readonly Dictionary<GameObject, PoolEntry> entries = new Dictionary<GameObject, PoolEntry>();
    private static Runner runner;
    private static AudioSource[] voices;
    private static int nextVoice;

    // 관찰용: 지금까지 새로 만든 인스턴스 수, 쓰는 중인 수, 쉬는 중인 수.
    public static int CreatedCount { get; private set; }

    public static int ActiveCount
    {
        get
        {
            int count = 0;
            foreach (PoolEntry entry in entries.Values) count += entry.Active.Count;
            return count;
        }
    }

    public static int IdleCount
    {
        get
        {
            int count = 0;
            foreach (PoolEntry entry in entries.Values) count += entry.Idle.Count;
            return count;
        }
    }

    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;
        EnsureRunner();

        PoolEntry entry = GetEntry(prefab);
        GameObject instance = null;

        // 쉬는 인스턴스를 쓰고, 씬 정리 등으로 파괴된 것은 건너뛴다.
        while (entry.Idle.Count > 0 && instance == null) instance = entry.Idle.Dequeue();

        if (instance == null)
        {
            if (entry.Active.Count >= MaxActivePerPrefab)
            {
                // 상한이면 가장 오래된 활성 이펙트를 끊고 그 인스턴스를 다시 쓴다.
                instance = entry.Active[0].Instance;
                entry.Active.RemoveAt(0);
            }
            if (instance == null) instance = CreateInstance(prefab);
        }

        instance.transform.SetPositionAndRotation(position, rotation);
        instance.SetActive(true);
        RestartParticles(instance);
        entry.Active.Add(new ActiveEffect { Instance = instance, ReturnAt = Time.time + entry.Lifetime });
        return instance;
    }

    // 맞은 지점에서 한 번 나는 3D 효과음. AudioSource를 미리 만들어 돌려 쓴다.
    // start/duration(초)을 주면 긴 클립의 일부만 재생한다(duration 0이면 끝까지).
    public static void PlaySoundAt(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f, float start = 0f, float duration = 0f)
    {
        if (clip == null) return;
        EnsureRunner();

        AudioSource voice = voices[nextVoice];
        nextVoice = (nextVoice + 1) % voices.Length;
        voice.transform.position = position;
        voice.clip = clip;
        voice.volume = Mathf.Clamp01(volume * AudioBus.Volume(AudioCategory.Effects));
        voice.pitch = pitch * (1f + Random.Range(-0.04f, 0.04f));
        voice.time = Mathf.Clamp(start, 0f, Mathf.Max(0f, clip.length - 0.01f));
        voice.Play();

        // 피치가 바뀌면 같은 구간을 재생하는 실제 시간도 달라진다.
        if (duration > 0f) voice.SetScheduledEndTime(AudioSettings.dspTime + duration / Mathf.Max(0.1f, voice.pitch));
    }

    private static void EnsureRunner()
    {
        if (runner != null) return;

        GameObject root = new GameObject("EffectPool");
        Object.DontDestroyOnLoad(root);
        runner = root.AddComponent<Runner>();

        // 도메인 리로드를 끄고 플레이했을 때 이전 판의 풀이 남아 있지 않게 비운다.
        entries.Clear();
        CreatedCount = 0;

        voices = new AudioSource[SoundVoiceCount];
        for (int i = 0; i < voices.Length; i++)
        {
            GameObject voiceObject = new GameObject("Voice " + i);
            voiceObject.transform.SetParent(root.transform, false);
            AudioSource source = voiceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            voices[i] = source;
        }
    }

    private static PoolEntry GetEntry(GameObject prefab)
    {
        if (!entries.TryGetValue(prefab, out PoolEntry entry))
        {
            entry = new PoolEntry { Lifetime = MeasureLifetime(prefab) };
            entries[prefab] = entry;
        }
        return entry;
    }

    // 프리팹의 파티클이 모두 끝나는 시간(지연 + 길이 + 입자 수명)의 최댓값이다.
    private static float MeasureLifetime(GameObject prefab)
    {
        float lifetime = MinLifetime;
        foreach (ParticleSystem system in prefab.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = system.main;
            float length = main.startDelay.constantMax + main.duration + main.startLifetime.constantMax;
            if (length > lifetime) lifetime = length;
        }
        return Mathf.Min(lifetime, MaxLifetime);
    }

    // 만들자마자 꺼 둔다. 이펙트 프리팹에 붙은 스크립트(AutoDestruct 등)는 스스로 파괴하므로 풀링하려면 꺼야 한다.
    // 이펙트 프리팹은 파티클 전용이라 모든 MonoBehaviour를 꺼도 동작이 달라지지 않는다.
    private static GameObject CreateInstance(GameObject prefab)
    {
        GameObject instance = Object.Instantiate(prefab, runner.transform);
        instance.name = prefab.name + " (pooled)";
        foreach (MonoBehaviour behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
        instance.SetActive(false);
        CreatedCount++;
        return instance;
    }

    private static void RestartParticles(GameObject instance)
    {
        foreach (ParticleSystem system in instance.GetComponentsInChildren<ParticleSystem>(true))
        {
            system.Clear(false);
            system.Play(false);
        }
    }

    private static void ReturnExpired()
    {
        float now = Time.time;
        foreach (PoolEntry entry in entries.Values)
        {
            // 한 프리팹의 반환 시간은 생성 순서와 같아서 앞에서부터 확인하면 된다.
            while (entry.Active.Count > 0 && entry.Active[0].ReturnAt <= now)
            {
                GameObject instance = entry.Active[0].Instance;
                entry.Active.RemoveAt(0);
                if (instance == null) continue;

                instance.SetActive(false);
                entry.Idle.Enqueue(instance);
            }
        }
    }
}
