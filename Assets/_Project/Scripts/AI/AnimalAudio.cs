// 동물의 경계/추격 시작, 공격, 피격, 사망, 주기적인 울음, 걸음에 맞춰 3D 소리를 내는 컴포넌트다. 소리만 담당하고 AI 상태를 바꾸지 않는다.
using UnityEngine;

[RequireComponent(typeof(Health))]
public class AnimalAudio : MonoBehaviour
{
    [SerializeField] private AnimalAudioSet audioSet;
    [SerializeField] private float minDistance = 3f;
    [SerializeField] private float maxDistance = 35f;

    private AudioSource voice;
    private AudioSource steps;
    private IAnimalVoiceSource source;
    private Health health;
    private Vector3 lastPosition;
    private float stepDistance;
    private float nextCallTime;
    private float nextHurtTime;
    private float lastHurtTime = -10f;
    private bool dead;

    private void Awake()
    {
        source = GetComponent<IAnimalVoiceSource>();
        health = GetComponent<Health>();
        voice = CreateSource(1f);
        steps = CreateSource(0.7f);

        if (source != null)
        {
            source.Alarmed += OnAlarmed;
            source.Attacked += OnAttacked;
        }
        health.Damaged += OnDamaged;
        health.Died += OnDied;
    }

    private AudioSource CreateSource(float volumeScale)
    {
        // 동물마다 위치에서 들리도록 3D 소리로 두고 거리에 따라 선형으로 작아지게 한다.
        AudioSource created = gameObject.AddComponent<AudioSource>();
        created.playOnAwake = false;
        created.spatialBlend = 1f;
        created.rolloffMode = AudioRolloffMode.Linear;
        created.minDistance = minDistance;
        created.maxDistance = maxDistance;
        created.volume = volumeScale;
        return created;
    }

    private void OnDestroy()
    {
        if (source != null)
        {
            source.Alarmed -= OnAlarmed;
            source.Attacked -= OnAttacked;
        }
        if (health != null)
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }
    }

    private void Start()
    {
        lastPosition = transform.position;
        ScheduleCall();
    }

    private void Update()
    {
        if (dead || audioSet == null || source == null) return;

        UpdateFootsteps();

        if (Time.time < nextCallTime) return;
        // 이미 다른 소리(비명 등)를 내는 중이거나 도망/추격 중이면 이번 울음은 건너뛴다.
        if (!voice.isPlaying && source.CanCallIdly) Play(voice, audioSet.Idle);
        ScheduleCall();
    }

    private void ScheduleCall()
    {
        if (audioSet == null) return;
        Vector2 interval = audioSet.IdleCallInterval;
        nextCallTime = Time.time + Random.Range(interval.x, interval.y);
    }

    // 평면으로 움직인 거리가 보폭을 넘을 때마다 발소리를 한 번 낸다(제자리에서는 나지 않는다).
    private void UpdateFootsteps()
    {
        Vector3 delta = transform.position - lastPosition;
        lastPosition = transform.position;
        delta.y = 0f;

        AnimalMotion motion = source.Motion;
        if (motion == AnimalMotion.None)
        {
            stepDistance = 0f;
            return;
        }

        stepDistance += delta.magnitude;
        float stride = motion == AnimalMotion.Run ? audioSet.RunStride : audioSet.WalkStride;
        if (stepDistance < stride) return;

        stepDistance = 0f;
        Play(steps, audioSet.Footstep, false, AudioCategory.Footsteps);
    }

    private void OnAlarmed()
    {
        if (dead || audioSet == null) return;
        // 방금 맞아서 비명을 지른 직후라면 경계 소리를 겹치지 않는다.
        if (Time.time - lastHurtTime < 0.6f) return;
        Play(voice, audioSet.Alarm, true);
        ScheduleCall();
    }

    private void OnAttacked()
    {
        if (dead || audioSet == null) return;
        Play(voice, audioSet.Attack, true);
        ScheduleCall();
    }

    private void OnDamaged(float amount, float remaining)
    {
        if (dead || audioSet == null || remaining <= 0f || Time.time < nextHurtTime) return;
        nextHurtTime = Time.time + audioSet.HurtCooldown;
        lastHurtTime = Time.time;
        Play(voice, audioSet.Hurt, true);
        ScheduleCall();
    }

    private void OnDied()
    {
        if (audioSet == null) return;
        dead = true;
        Play(voice, audioSet.Death, true);
    }

    // interrupt가 true면 재생 중인 소리를 끊고 새로 낸다(비명이 우선이다).
    private static void Play(AudioSource target, SoundGroup group, bool interrupt = false, AudioCategory category = AudioCategory.Voices)
    {
        if (!group.HasClips) return;
        if (target.isPlaying)
        {
            if (!interrupt) return;
            target.Stop();
        }

        target.pitch = group.PickPitch();
        target.PlayOneShot(group.PickClip(), group.volume * AudioBus.Volume(category));
    }
}
