// 접속자 한 명의 모습(아바타)이다. 소유자 쪽에서는 로컬 플레이어의 위치/회전/애니메이터 값을 따라가고 숨겨지며, 다른 접속자 쪽에서는 네트워크로 받은 값으로 움직이는 캐릭터로 보인다. 게임 규칙은 갖지 않는 표현 전용이다.
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Animator))]
public class NetworkPlayerAvatar : NetworkBehaviour
{
    private Animator avatarAnimator;
    private Animator sourceAnimator;
    private Transform source;
    private TextMesh nameTag;
    private Renderer[] renderers;
    private NetworkCombat combat;
    private int shownPercent = -1;
    private bool shownDead;
    // 서버: 접속자마다 나눠 준 시작 위치 번호다(0은 호스트라 쓰지 않는다).
    private static readonly Dictionary<ulong, int> startSlots = new Dictionary<ulong, int>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private static readonly bool wiggle = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-mp-wiggle") >= 0;
#endif

    public override void OnNetworkSpawn()
    {
        avatarAnimator = GetComponent<Animator>();
        combat = GetComponent<NetworkCombat>();
        renderers = GetComponentsInChildren<Renderer>(true);
        name = IsOwner ? "Avatar (local)" : $"Avatar (client {OwnerClientId})";

        // 내 아바타는 내 화면에서 보이지 않는다(내 캐릭터가 이미 있다). 남의 아바타만 보인다.
        foreach (Renderer renderer in renderers) renderer.enabled = !IsOwner;
        if (!IsOwner) CreateNameTag();

        // 모든 참가자의 로컬 플레이어가 같은 시작 위치에서 시작하면 몸 충돌체가 서로를 밀어내므로 서버가 접속자마다 시작 위치 번호를 나눠 준다.
        if (IsServer && !IsOwner) StartSlotRpc(ReserveStartSlot(OwnerClientId));
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer) startSlots.Remove(OwnerClientId);
    }

    // 세션이 끝나면 번호를 모두 반납한다.
    public static void ResetStartSlots() => startSlots.Clear();

    // 서버: 접속자의 시작 위치 번호를 정한다(이미 정했으면 그대로). 호스트 자신은 0이다.
    public static int ReserveStartSlot(ulong clientId)
    {
        NetworkManager manager = NetworkManager.Singleton;
        if (manager != null && clientId == manager.LocalClientId) return 0;
        if (startSlots.TryGetValue(clientId, out int existing)) return existing;

        int slot = StartSlotLayout.NextFree(startSlots.Values);
        startSlots[clientId] = slot;
        return slot;
    }

    // 기준 위치(씬 시작 위치)에서 번호의 고리 위 칸으로, 지면과 NavMesh에 맞춘 위치다.
    public static Vector3 StartPositionFor(Vector3 origin, int slot)
    {
        StartSlotLayout.Offset(slot, out float x, out float z);
        Vector3 target = origin + new Vector3(x, 0f, z);
        if (slot <= 0) return target;

        target = GroundSnap.OnGround(target);
        return NavMesh.SamplePosition(target, out NavMeshHit hit, 3f, NavMesh.AllAreas) ? hit.position : target;
    }

    // 소유자: 받은 번호의 위치로 자기 플레이어와 침대 없는 부활 위치를 옮긴다. 시작 위치 둘레의 고리 위, 지면과 NavMesh에 맞춘다.
    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
    private void StartSlotRpc(int slot)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        // 기준은 지금 위치가 아니라 씬 시작 위치다(시작 직후 다른 충돌체에 밀려난 위치가 기준이 되지 않게).
        PlayerRespawn respawn = player.GetComponent<PlayerRespawn>();
        Vector3 origin = respawn != null ? respawn.StartPosition : player.transform.position;
        Vector3 target = StartPositionFor(origin, slot);

        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        player.transform.position = target;
        if (controller != null) controller.enabled = true;

        if (respawn != null) respawn.SetSpawnPoint(target);
    }

    private void LateUpdate()
    {
        if (!IsSpawned) return;

        if (IsOwner) FollowLocalPlayer();
        else FaceNameTag();
    }

    private void FollowLocalPlayer()
    {
        if (source == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;
            source = player.transform;
            sourceAnimator = player.GetComponent<Animator>();
        }

        transform.SetPositionAndRotation(source.position, source.rotation);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 테스트용: -mp-wiggle 인자가 있으면 아바타를 좌우로 오가게 해 이동 동기화를 눈으로 확인한다(로컬 플레이어는 그대로).
        if (wiggle) transform.position += Vector3.right * Mathf.PingPong(Time.time * 2f, 8f);
#endif
        if (sourceAnimator != null && avatarAnimator != null) CopyAnimatorValues(sourceAnimator, avatarAnimator);
    }

    // 같은 컨트롤러를 쓰는 두 애니메이터의 값 매개변수를 복사한다(트리거는 건너뛴다).
    private static void CopyAnimatorValues(Animator from, Animator to)
    {
        foreach (AnimatorControllerParameter parameter in from.parameters)
        {
            switch (parameter.type)
            {
                case AnimatorControllerParameterType.Float: to.SetFloat(parameter.nameHash, from.GetFloat(parameter.nameHash)); break;
                case AnimatorControllerParameterType.Int: to.SetInteger(parameter.nameHash, from.GetInteger(parameter.nameHash)); break;
                case AnimatorControllerParameterType.Bool: to.SetBool(parameter.nameHash, from.GetBool(parameter.nameHash)); break;
            }
        }
    }

    // ---- 이름표 ----

    private void CreateNameTag()
    {
        GameObject tag = new GameObject("NameTag");
        tag.transform.SetParent(transform, false);
        tag.transform.localPosition = new Vector3(0f, 2.15f, 0f);
        nameTag = tag.AddComponent<TextMesh>();
        nameTag.text = "Player " + (OwnerClientId + 1);
        nameTag.fontSize = 48;
        nameTag.characterSize = 0.05f;
        nameTag.anchor = TextAnchor.MiddleCenter;
        nameTag.alignment = TextAlignment.Center;
        nameTag.color = new Color(0.78f, 0.96f, 1f);
        nameTag.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tag.GetComponent<MeshRenderer>().sharedMaterial = nameTag.font.material;
    }

    // 이름표에 체력 비율을 보여 주고, 사망하면 회색 "(사망)"으로 바꾼다.
    private void UpdateNameTagStatus()
    {
        if (nameTag == null || combat == null) return;

        int percent = Mathf.RoundToInt(combat.HealthRatio * 100f);
        bool dead = combat.IsDead;
        if (percent == shownPercent && dead == shownDead) return;

        shownPercent = percent;
        shownDead = dead;
        string baseName = "Player " + (OwnerClientId + 1);
        nameTag.text = dead ? baseName + " (사망)" : baseName + "  " + percent + "%";
        nameTag.color = dead ? new Color(0.6f, 0.6f, 0.6f) : Color.Lerp(new Color(1f, 0.35f, 0.35f), new Color(0.78f, 0.96f, 1f), combat.HealthRatio);
    }

    private void FaceNameTag()
    {
        UpdateNameTagStatus();
        if (nameTag == null || Camera.main == null) return;
        // 글자가 항상 카메라를 보게 한다(뒤집히지 않게 카메라 회전을 그대로 쓴다).
        nameTag.transform.rotation = Camera.main.transform.rotation;
    }
}
