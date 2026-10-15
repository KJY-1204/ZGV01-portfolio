// 좀비가 걸어서 이동한 거리에 맞춰 3D 발소리를 내는 컴포넌트다. 뼈대가 없는 좀비도 쓸 수 있게 이동 거리로 걸음을 센다.
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))]
public class ZombieFootsteps : MonoBehaviour
{
    [SerializeField] private FootstepSoundSet footstepSet;
    // 이 거리를 이동할 때마다 한 걸음 소리를 낸다.
    [SerializeField] private float strideLength = 1.1f;
    [SerializeField] private float volume = 0.8f;
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 25f;

    private AudioSource stepSource;
    private AudioSource layerSource;
    private Health health;
    private Vector3 lastPosition;
    private float travelled;
    private bool dead;

    private void Awake()
    {
        health = GetComponent<Health>();
        stepSource = CreateSource();
        layerSource = CreateSource();
        health.Died += OnDied;
    }

    private AudioSource CreateSource()
    {
        AudioSource created = gameObject.AddComponent<AudioSource>();
        created.playOnAwake = false;
        created.spatialBlend = 1f;
        created.rolloffMode = AudioRolloffMode.Linear;
        created.minDistance = minDistance;
        created.maxDistance = maxDistance;
        return created;
    }

    private void OnDestroy()
    {
        if (health != null) health.Died -= OnDied;
    }

    private void Start()
    {
        lastPosition = transform.position;
    }

    private void OnDied()
    {
        dead = true;
    }

    private void Update()
    {
        Vector3 position = transform.position;
        // 순간이동처럼 한 프레임에 크게 움직인 경우는 걸음으로 세지 않는다.
        Vector3 delta = position - lastPosition;
        delta.y = 0f;
        lastPosition = position;
        if (dead || footstepSet == null || delta.magnitude > strideLength * 2f) return;

        travelled += delta.magnitude;
        if (travelled < strideLength) return;

        travelled -= strideLength;
        ImpactSurface surface = FootstepSounds.SurfaceUnder(position, transform, footstepSet);
        FootstepSounds.Play(stepSource, layerSource, footstepSet, surface, volume);
    }
}
