// 좀비의 상태 변화(발견, 공격, 피격, 사망)와 주기적인 신음에 맞춰 3D 소리를 내는 컴포넌트다. 소리만 담당하고 AI 상태를 바꾸지 않는다.
using UnityEngine;

[RequireComponent(typeof(ZombieAI))]
[RequireComponent(typeof(Health))]
public class ZombieAudio : MonoBehaviour
{
    [SerializeField] private ZombieAudioSet audioSet;
    [SerializeField] private float minDistance = 3f;
    [SerializeField] private float maxDistance = 30f;

    private AudioSource voice;
    // 몸집에 따라 목소리 음정을 바꾸는 배율이다(ZombieAppearance가 정한다).
    public float PitchScale { get; set; } = 1f;
    private ZombieAI ai;
    private Health health;
    private float nextGroanTime;
    private float nextHurtTime;
    private bool dead;

    private void Awake()
    {
        ai = GetComponent<ZombieAI>();
        health = GetComponent<Health>();

        // 좀비마다 위치에서 들리도록 3D 소리로 두고 거리에 따라 선형으로 작아지게 한다.
        voice = gameObject.AddComponent<AudioSource>();
        voice.playOnAwake = false;
        voice.spatialBlend = 1f;
        voice.rolloffMode = AudioRolloffMode.Linear;
        voice.minDistance = minDistance;
        voice.maxDistance = maxDistance;

        ai.Alerted += OnAlerted;
        ai.Attacked += OnAttacked;
        health.Damaged += OnDamaged;
        health.Died += OnDied;
    }

    private void OnDestroy()
    {
        if (ai != null)
        {
            ai.Alerted -= OnAlerted;
            ai.Attacked -= OnAttacked;
        }
        if (health != null)
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }
    }

    private void Start()
    {
        ScheduleGroan();
    }

    private void Update()
    {
        if (dead || audioSet == null || Time.time < nextGroanTime) return;

        // 이미 다른 소리(포효, 비명)를 내는 중이면 신음을 건너뛰고 다음 기회를 기다린다.
        if (!voice.isPlaying) Play(audioSet.Idle);
        ScheduleGroan();
    }

    private void ScheduleGroan()
    {
        if (audioSet == null) return;
        Vector2 interval = ai.IsChasing ? audioSet.ChaseGroanInterval : audioSet.IdleGroanInterval;
        nextGroanTime = Time.time + Random.Range(interval.x, interval.y);
    }

    private void OnAlerted()
    {
        if (dead || audioSet == null) return;
        Play(audioSet.Aggro);
        ScheduleGroan();
    }

    private void OnAttacked()
    {
        if (dead || audioSet == null) return;
        Play(audioSet.Attack);
    }

    private void OnDamaged(float amount, float remaining)
    {
        if (dead || audioSet == null || remaining <= 0f || Time.time < nextHurtTime) return;
        nextHurtTime = Time.time + audioSet.HurtCooldown;
        Play(audioSet.Hurt);
    }

    // 사망 소리는 다른 소리에 끊기지 않게 마지막에 한 번만 낸다.
    private void OnDied()
    {
        if (dead) return;
        dead = true;
        if (audioSet != null) Play(audioSet.Death);
    }

    // 새 소리는 재생 중인 소리를 끊고 낸다(좀비 한 마리는 한 번에 한 소리).
    private void Play(SoundGroup group)
    {
        AudioClip clip = group.PickClip();
        if (clip == null) return;

        voice.Stop();
        voice.clip = clip;
        voice.volume = group.volume * AudioBus.Volume(AudioCategory.Voices);
        voice.pitch = group.PickPitch() * PitchScale;
        voice.Play();
    }
}
