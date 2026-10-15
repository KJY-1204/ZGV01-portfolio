// 걷거나 달릴 때 발이 땅에 닿는 순간에 맞춰 발소리를 내고, 점프/착지 소리도 내는 컴포넌트다. 애니메이션 이벤트가 없는 캐릭터라 발 본의 높이로 닿는 순간을 잡는다.
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterController))]
public class PlayerFootsteps : MonoBehaviour
{
    // 기본 걸음 소리와 바닥 재질별 겹침 소리 묶음이다.
    [SerializeField] private FootstepSoundSet footstepSet;

    // 발 본(발목)의 캐릭터 발밑 기준 높이가 plantHeight 아래로 내려오면 닿은 것, liftHeight 위로 올라가면 뗀 것으로 본다.
    // 발목 높이는 걷기에서 0.13(닿음)~0.33(듦), 달리기에서 0.14~0.68이라 그 사이에 문턱을 둔다.
    [Header("Foot Contact")]
    [SerializeField] private float plantHeight = 0.16f;
    [SerializeField] private float liftHeight = 0.22f;
    [SerializeField] private float minSpeed = 0.4f;
    [SerializeField] private float minStepInterval = 0.2f;

    // 걷는 속도에서는 작게, 달리는 속도에서는 크게 낸다.
    [Header("Volume")]
    [SerializeField] private float quietSpeed = 2f;
    [SerializeField] private float loudSpeed = 5.3f;
    [SerializeField] private float quietVolume = 0.35f;
    [SerializeField] private float loudVolume = 0.75f;

    // 점프는 땅에서 떨어지며 위로 올라갈 때, 착지는 일정 시간 이상 공중에 있다가 땅에 닿을 때 난다.
    // 착지 소리는 떨어진 속도가 빠를수록 크게 내고, 바닥 재질 소리도 같이 얹는다.
    [Header("Jump & Land")]
    [SerializeField] private SoundGroup jumpSounds = new SoundGroup();
    [SerializeField] private SoundGroup landSounds = new SoundGroup();
    [SerializeField] private float minAirTimeForLanding = 0.2f;
    [SerializeField] private float jumpMinUpSpeed = 1f;
    [SerializeField] private float softLandSpeed = 2f;
    [SerializeField] private float hardLandSpeed = 9f;
    [SerializeField] private float softLandVolume = 0.5f;
    [SerializeField] private float hardLandVolume = 1f;

    private Animator animator;
    private CharacterController controller;
    private StarterAssets.ThirdPersonController movement;
    // 걸음/재질 소리/점프·착지 소리는 서로 피치를 바꿔도 영향이 없게 소스를 따로 둔다.
    private AudioSource stepSource;
    private AudioSource layerSource;
    private AudioSource airSource;
    private readonly Transform[] feet = new Transform[2];
    private readonly bool[] planted = new bool[2];
    private readonly float[] nextStepTime = new float[2];

    private bool wasGrounded = true;
    private float airborneSince;
    private float fastestFall;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
        movement = GetComponent<StarterAssets.ThirdPersonController>();

        stepSource = CreateSource();
        layerSource = CreateSource();
        airSource = CreateSource();
    }

    private AudioSource CreateSource()
    {
        AudioSource created = gameObject.AddComponent<AudioSource>();
        created.playOnAwake = false;
        created.spatialBlend = 0f;
        return created;
    }

    private void Start()
    {
        feet[0] = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        feet[1] = animator.GetBoneTransform(HumanBodyBones.RightFoot);
    }

    // 애니메이션이 이번 프레임 포즈를 쓴 뒤의 발 위치를 읽어야 해서 LateUpdate에서 확인한다.
    private void LateUpdate()
    {
        Vector3 velocity = controller.velocity;
        float speed = new Vector2(velocity.x, velocity.z).magnitude;
        bool grounded = movement != null ? movement.Grounded : controller.isGrounded;
        bool moving = grounded && speed >= minSpeed;

        UpdateAirState(grounded, velocity.y);

        for (int i = 0; i < feet.Length; i++)
        {
            if (feet[i] == null) continue;

            float height = feet[i].position.y - transform.position.y;
            if (planted[i])
            {
                if (height > liftHeight) planted[i] = false;
                continue;
            }

            if (height < plantHeight && moving && Time.time >= nextStepTime[i])
            {
                planted[i] = true;
                nextStepTime[i] = Time.time + minStepInterval;
                PlayStep(speed);
            }
        }
    }

    // 접지 상태가 바뀌는 순간을 잡아 점프/착지 소리를 낸다.
    private void UpdateAirState(bool grounded, float verticalSpeed)
    {
        if (wasGrounded && !grounded)
        {
            airborneSince = Time.time;
            fastestFall = 0f;
            if (verticalSpeed > jumpMinUpSpeed) PlayAir(jumpSounds, 1f);
        }
        else if (!grounded)
        {
            fastestFall = Mathf.Max(fastestFall, -verticalSpeed);
        }
        else if (!wasGrounded && Time.time - airborneSince >= minAirTimeForLanding)
        {
            float volume = Mathf.Lerp(softLandVolume, hardLandVolume, Mathf.InverseLerp(softLandSpeed, hardLandSpeed, fastestFall));
            PlayAir(landSounds, volume);
            if (footstepSet != null)
                FootstepSounds.PlayLayer(layerSource, footstepSet, FootstepSounds.SurfaceUnder(transform.position, transform, footstepSet), volume);
        }

        wasGrounded = grounded;
    }

    private void PlayAir(SoundGroup group, float volume)
    {
        AudioClip clip = group.PickClip();
        if (clip == null) return;
        airSource.pitch = group.PickPitch();
        airSource.PlayOneShot(clip, volume * group.volume * AudioBus.Volume(AudioCategory.Footsteps));
    }

    private void PlayStep(float speed)
    {
        if (footstepSet == null) return;

        float volume = Mathf.Lerp(quietVolume, loudVolume, Mathf.InverseLerp(quietSpeed, loudSpeed, speed));
        // 걸을 때는 조용하고(2.5m) 달릴 때는 멀리(9m) 들린다.
        NoiseSystem.Emit(transform.position, Mathf.Lerp(2.5f, 9f, Mathf.InverseLerp(quietSpeed, loudSpeed, speed)), this);
        ImpactSurface surface = FootstepSounds.SurfaceUnder(transform.position, transform, footstepSet);
        FootstepSounds.Play(stepSource, layerSource, footstepSet, surface, volume);
    }
}
