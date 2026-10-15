// 접속자 아바타의 몸짓(사망해서 쓰러지는 모습과 부활, 맨손 공격)을 동기화하는 표현 전용 컴포넌트다. 소유자 쪽에서는 로컬 플레이어의 펀치 시작을 횟수로 올리고, 다른 접속자 쪽에서는 NetworkCombat의 사망 값과 펀치 횟수로 아바타의 모습을 만든다. 게임 규칙(피해, 부활 위치)은 갖지 않는다.
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkCombat))]
public class AvatarActionSync : NetworkBehaviour
{
    // PlayerRespawn의 쓰러짐 연출과 같은 값이다(모델 루트를 앞으로 눕히는 각도와 걸리는 시간).
    private const float FallAngle = -85f;
    private const float FallSeconds = 0.5f;

    // 펀치를 시작할 때마다 하나씩 올라간다. 값이 바뀌면 다른 접속자 쪽에서 팔과 상체 동작을 재생한다.
    private readonly NetworkVariable<int> punchCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private NetworkCombat combat;
    private UnarmedPunch view;
    // 소유자 쪽: 로컬 플레이어의 펀치 컴포넌트
    private UnarmedPunch source;
    private Transform[] modelRoots;
    // 보는 쪽: 쓰러짐 진행도(0 서 있음 ~ 1 쓰러짐)
    private float fallProgress;
    // 몸 충돌체: 다른 접속자의 아바타만 켠다(내 아바타는 내 플레이어와 겹쳐 막히지 않게 끈다). 사망하면 눕힌 모습에 맞춘다.
    private CapsuleCollider body;
    private bool bodyLying;

    public override void OnNetworkSpawn()
    {
        combat = GetComponent<NetworkCombat>();
        view = GetComponent<UnarmedPunch>();
        body = GetComponent<CapsuleCollider>();
        if (body != null) body.enabled = !IsOwner;
        if (IsOwner) return;

        // 다른 접속자의 아바타는 입력 없이 받은 값으로만 몸짓을 만든다.
        if (view != null) view.EnterRemoteView();
        punchCount.OnValueChanged += OnPunchChanged;
        modelRoots = FindModelRoots();
        // 이미 죽어 있는 접속자를 늦게 본 경우 바로 쓰러진 모습으로 둔다.
        fallProgress = combat != null && combat.IsDead ? 1f : 0f;
        ApplyFall();
        ShapeBody(fallProgress >= 1f);
    }

    public override void OnNetworkDespawn()
    {
        punchCount.OnValueChanged -= OnPunchChanged;
        if (source != null) source.Punched -= OnLocalPunched;
        source = null;
    }

    private void Update()
    {
        if (!IsSpawned) return;
        if (IsOwner) UpdateOwner();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        RunDebug();
#endif
    }

    private void LateUpdate()
    {
        if (!IsSpawned || IsOwner || combat == null) return;

        // 죽으면 0.5초에 걸쳐 눕고 되살아나면 바로 선다(PlayerRespawn과 같다).
        if (combat.IsDead) fallProgress = Mathf.MoveTowards(fallProgress, 1f, Time.deltaTime / FallSeconds);
        else fallProgress = 0f;
        ApplyFall();
        ShapeBody(combat.IsDead);
    }

    // 서 있을 때는 CharacterController와 같은 세운 캡슐이고, 죽으면 쓰러진 몸(머리가 뒤쪽 -Z)에 맞춰 눕힌 캡슐이다.
    private void ShapeBody(bool lying)
    {
        if (body == null || bodyLying == lying) return;
        bodyLying = lying;
        body.direction = lying ? 2 : 1;
        body.center = lying ? new Vector3(0f, body.radius, -body.height * 0.5f) : new Vector3(0f, 0.93f, 0f);
    }

    // ---- 사망/부활(보는 쪽) ----

    private Transform[] FindModelRoots()
    {
        // 플레이어 프리팹의 쓰러짐 대상(PlayerRespawn.modelRoots)과 같은 두 루트다.
        return new[] { transform.Find("Geometry"), transform.Find("Skeleton") };
    }

    private void ApplyFall()
    {
        if (modelRoots == null) return;
        float pitch = Mathf.Lerp(0f, FallAngle, Mathf.SmoothStep(0f, 1f, fallProgress));
        foreach (Transform root in modelRoots)
            if (root != null) root.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    // ---- 맨손 공격 ----

    private void UpdateOwner()
    {
        if (source != null) return;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        UnarmedPunch found = player != null ? player.GetComponent<UnarmedPunch>() : null;
        if (found == null) return;
        source = found;
        source.Punched += OnLocalPunched;
    }

    private void OnLocalPunched()
    {
        if (IsSpawned && IsOwner) punchCount.Value++;
    }

    private void OnPunchChanged(int previous, int now)
    {
        if (view != null && !combat.IsDead) view.PlayRemote();
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // 테스트용: -mp-action-test면 소유자가 4초마다 펀치하고 죽은 지 12초 뒤에 부활한다. -mp-action-log면 다른 접속자의 아바타가 보여 주는 몸짓 상태를 2초마다 남긴다.
    private static readonly bool actionTest = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-mp-action-test") >= 0;
    private static readonly bool actionLog = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-mp-action-log") >= 0;
    private float nextPunchTest;
    private float nextActionLog;
    private float deadSince = -1f;

    private void RunDebug()
    {
        if (IsOwner)
        {
            if (!actionTest || source == null) return;
            GameObject player = source.gameObject;
            Health health = player.GetComponent<Health>();
            if (health != null && health.IsDead)
            {
                if (deadSince < 0f) deadSince = Time.time;
                if (Time.time - deadSince >= 12f)
                {
                    PlayerRespawn respawn = player.GetComponent<PlayerRespawn>();
                    if (respawn != null && respawn.CanRespawn) respawn.Respawn();
                    deadSince = -1f;
                }
                return;
            }
            deadSince = -1f;
            if (Time.time >= nextPunchTest)
            {
                nextPunchTest = Time.time + 4f;
                source.TryStart();
            }
            return;
        }

        if (!actionLog || Time.time < nextActionLog) return;
        nextActionLog = Time.time + 2f;
        float pitch = modelRoots != null && modelRoots.Length > 0 && modelRoots[0] != null ? modelRoots[0].localEulerAngles.x : 0f;
        pitch = Mathf.DeltaAngle(0f, pitch);
        Debug.Log($"[mp-action] {name} 사망={(combat != null && combat.IsDead)} 눕힘각도={pitch:F0} 펀치횟수={punchCount.Value} 펀치중={(view != null && view.IsPunching)} 충돌체={(body != null && body.enabled)} 눕힘충돌체={bodyLying}");
    }
#endif
}
