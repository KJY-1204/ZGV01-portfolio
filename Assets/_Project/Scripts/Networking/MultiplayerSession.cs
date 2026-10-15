// 멀티플레이 접속(호스트/참가), 접속자별 아바타 소환, 종료를 맡는 컴포넌트다. NetworkRoot 프리팹에 NetworkManager와 함께 붙어 있고 게임을 시작할 때 스스로 만들어진다. 직접 IP 접속만 지원한다.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MultiplayerSession : MonoBehaviour
{
    public const ushort DefaultPort = 7777;
    private const string RootResource = "NetworkRoot";

    public static MultiplayerSession Instance { get; private set; }

    // 네트워크가 시작돼 있는지(호스트이거나 클라이언트)다. 멀티 중에는 일시정지가 시간을 멈추지 않는다.
    public static bool Active => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
    public static bool IsHost => Active && NetworkManager.Singleton.IsHost;
    // 호스트가 아닌 참가자다(저장/불러오기를 할 수 없다).
    public static bool IsClientOnly => Active && !NetworkManager.Singleton.IsServer;
    public static int PlayerCount => Active ? NetworkManager.Singleton.ConnectedClientsIds.Count : 0;

    // 접속 진행 상황/실패 이유 문구다. 메인 메뉴가 보여 준다.
    public static string Status { get; private set; } = "";
    public static event Action<string> StatusChanged;

    [SerializeField] private NetworkObject avatarPrefab;
    // 서버가 월드에 소환하는 네트워크 프리팹들(좀비, 전리품)이다. 모두 NetworkManager에 등록한다.
    [SerializeField] private NetworkObject[] spawnablePrefabs = System.Array.Empty<NetworkObject>();
    // 게임 씬이 열린 뒤 서버가 한 번 소환하는 월드 공유 오브젝트(WorldSync)다.
    [SerializeField] private NetworkObject worldPrefab;
    private NetworkObject worldInstance;

    private NetworkManager manager;
    private UnityTransport transport;
    private readonly HashSet<ulong> avatarRequested = new HashSet<ulong>();
    // 끊김/실패 이유는 종료 후에도 메인 메뉴에서 보이도록 한 번은 지우지 않는다.
    private bool stickyStatus;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // 테스트용: 명령줄에 -mp-host 또는 -mp-join 주소:포트가 있으면 메인 메뉴에서 자동으로 시작한다.
    public static string AutoCommand { get; private set; } = "";
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        GameObject prefab = Resources.Load<GameObject>(RootResource);
        if (prefab == null) return;

        GameObject root = Instantiate(prefab);
        root.name = "NetworkRoot";
        DontDestroyOnLoad(root);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        manager = GetComponent<NetworkManager>();
        transport = GetComponent<UnityTransport>();
        // NetworkManager 설정에 기본 프리팹 목록이 이미 아바타를 들고 있을 수 있어, 없을 때만 더한다(중복 등록은 오류다).
        if (avatarPrefab != null && !manager.NetworkConfig.Prefabs.Contains(avatarPrefab.gameObject)) manager.AddNetworkPrefab(avatarPrefab.gameObject);
        foreach (NetworkObject prefab in spawnablePrefabs)
            if (prefab != null && !manager.NetworkConfig.Prefabs.Contains(prefab.gameObject)) manager.AddNetworkPrefab(prefab.gameObject);
        if (worldPrefab != null && !manager.NetworkConfig.Prefabs.Contains(worldPrefab.gameObject)) manager.AddNetworkPrefab(worldPrefab.gameObject);
        transport.MaxConnectAttempts = 5;
        transport.ConnectTimeoutMS = 1000;
        // 상대가 비정상 종료했을 때 기본 30초 대신 10초 안에 끊김을 알아챈다.
        transport.DisconnectTimeoutMS = 10000;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        foreach (string arg in Environment.GetCommandLineArgs())
            if (arg == "-mp-host" || arg.StartsWith("-mp-join=")) AutoCommand = arg;
#endif
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ---- 시작/종료 ----

    public static bool StartHost(ushort port)
    {
        if (Instance == null) { SetStatus("멀티플레이를 사용할 수 없습니다"); return false; }
        return Instance.BeginHost(port);
    }

    public static bool StartClient(string address, ushort port)
    {
        if (Instance == null) { SetStatus("멀티플레이를 사용할 수 없습니다"); return false; }
        return Instance.BeginClient(address, port);
    }

    // 호스트가 게임 씬을 불러 접속자 모두를 그 씬으로 데려간다.
    public static void LoadGameScene()
    {
        if (Instance == null || !IsHost) return;
        NetworkManager.Singleton.SceneManager.LoadScene(GameSession.GameScene, LoadSceneMode.Single);
    }

    public static void Shutdown()
    {
        if (Instance != null) Instance.EndSession();
    }

    private bool BeginHost(ushort port)
    {
        if (manager.IsListening) return false;

        transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");
        avatarRequested.Clear();
        if (!manager.StartHost())
        {
            SetStatus("호스트를 시작하지 못했습니다(포트가 이미 쓰이고 있을 수 있습니다)");
            return false;
        }

        manager.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
        manager.SceneManager.OnSynchronizeComplete += OnClientSynchronized;
        manager.OnClientDisconnectCallback += OnAnyClientDisconnected;
        SetStatus("호스트 중");
        return true;
    }

    private bool BeginClient(string address, ushort port)
    {
        if (manager.IsListening) return false;
        if (string.IsNullOrWhiteSpace(address)) { SetStatus("주소를 입력하세요"); return false; }

        transport.SetConnectionData(address.Trim(), port);
        manager.OnClientConnectedCallback += OnLocalClientConnected;
        manager.OnClientDisconnectCallback += OnAnyClientDisconnected;
        if (!manager.StartClient())
        {
            manager.OnClientConnectedCallback -= OnLocalClientConnected;
            manager.OnClientDisconnectCallback -= OnAnyClientDisconnected;
            SetStatus("접속을 시작하지 못했습니다");
            return false;
        }
        SetStatus("접속 중...");
        return true;
    }

    private void EndSession()
    {
        if (manager.SceneManager != null)
        {
            manager.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
            manager.SceneManager.OnSynchronizeComplete -= OnClientSynchronized;
        }
        manager.OnClientConnectedCallback -= OnLocalClientConnected;
        manager.OnClientDisconnectCallback -= OnAnyClientDisconnected;
        if (manager.IsListening) manager.Shutdown();
        avatarRequested.Clear();
        worldInstance = null;
        // 세션이 끝나면 참가자 저장 기록도 비운다(저장하지 않은 변화는 사라진다).
        ParticipantStore.Clear();
        NetworkPlayerAvatar.ResetStartSlots();
        if (stickyStatus) stickyStatus = false;
        else SetStatus("");
    }

    // ---- 접속 이벤트 ----

    private void OnLocalClientConnected(ulong clientId)
    {
        if (clientId == manager.LocalClientId) SetStatus("접속했습니다. 월드를 불러오는 중...");
    }

    // 클라이언트: 접속에 실패했거나 서버가 끊겼다. 호스트: 접속자가 나갔다(아바타는 네트워크가 알아서 지운다).
    private void OnAnyClientDisconnected(ulong clientId)
    {
        if (manager.IsServer) return;
        if (clientId != manager.LocalClientId) return;

        bool inGame = SceneManager.GetActiveScene().name == GameSession.GameScene;
        string reason = manager.DisconnectReason;
        stickyStatus = true;
        SetStatus(!string.IsNullOrEmpty(reason) ? reason : inGame ? "서버와의 연결이 끊어졌습니다" : "접속하지 못했습니다(주소와 포트를 확인하세요)");
        // 콜백 안에서 바로 종료하지 않고 다음 프레임에 정리한다.
        StartCoroutine(LeaveNextFrame(inGame));
    }

    private IEnumerator LeaveNextFrame(bool inGame)
    {
        yield return null;
        if (inGame) GameSession.ReturnToMenu();
        else EndSession();
    }

    // ---- 아바타 소환(서버) ----

    private void OnLoadEventCompleted(string sceneName, LoadSceneMode mode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (sceneName != GameSession.GameScene) return;
        SpawnWorld();
        foreach (ulong clientId in clientsCompleted) SpawnAvatar(clientId);
    }

    // 게임이 이미 진행 중일 때 늦게 들어온 접속자다.
    private void OnClientSynchronized(ulong clientId)
    {
        if (SceneManager.GetActiveScene().name == GameSession.GameScene) SpawnAvatar(clientId);
    }

    // 월드 공유 상태 오브젝트를 한 번만 소환한다.
    private void SpawnWorld()
    {
        if (!manager.IsServer || worldPrefab == null || worldInstance != null) return;
        worldInstance = Instantiate(worldPrefab);
        worldInstance.Spawn(true);
    }

    private void SpawnAvatar(ulong clientId)
    {
        if (!manager.IsServer || avatarPrefab == null || !avatarRequested.Add(clientId)) return;

        // 아바타는 처음부터 접속자의 시작 위치 칸에 소환한다(모두 호스트 위치에 겹쳐 소환되면 몸 충돌체가 호스트를 밀어낸다).
        GameObject local = GameObject.FindGameObjectWithTag("Player");
        PlayerRespawn respawn = local != null ? local.GetComponent<PlayerRespawn>() : null;
        Vector3 origin = respawn != null ? respawn.StartPosition : (local != null ? local.transform.position : Vector3.zero);
        Vector3 position = NetworkPlayerAvatar.StartPositionFor(origin, NetworkPlayerAvatar.ReserveStartSlot(clientId));
        NetworkObject avatar = Instantiate(avatarPrefab, position, Quaternion.identity);
        avatar.SpawnAsPlayerObject(clientId, true);
    }

    // ---- 도우미 ----

    // 같은 네트워크의 다른 사람이 접속할 때 쓸 이 컴퓨터의 주소다(찾지 못하면 빈 문자열).
    public static string LocalAddress()
    {
        try
        {
            foreach (IPAddress address in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                if (address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address)) return address.ToString();
        }
        catch (SocketException)
        {
        }
        return "";
    }

    private static void SetStatus(string text)
    {
        Status = text;
        StatusChanged?.Invoke(text);
    }
}
