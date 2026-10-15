// 플레이어 주변의 상호작용 대상(채집 노드, 문, 사다리, 보관함, 침대, 말)에서 가장 가까운 사용 가능 대상을 골라 테두리와 안내 문구로 알리고, E 입력을 그 대상에게만 전달하는 컴포넌트다.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionFocus : MonoBehaviour
{
    [SerializeField] private Shader outlineShader;
    // 닿을 수 있는 가장 가까운 대상의 테두리(밝은 색)와, 근처에 있지만 아직 닿지 않는 대상의 테두리(흐린 색)다.
    [SerializeField] private Color reachColor = new Color(1f, 0.85f, 0.2f, 0.75f);
    [SerializeField] private Color nearColor = new Color(0.45f, 0.85f, 1f, 0.45f);
    [SerializeField] private float reachWidth = 0.04f;
    [SerializeField] private float nearWidth = 0.02f;
    // 이 거리 안의 대상은 흐린 테두리로 "상호작용 대상"임을 알려 준다.
    [SerializeField] private float nearRange = 7f;
    [SerializeField] private float doorRange = 3f;
    [SerializeField] private float storageRange = 3f;

    private enum Kind { None, Gather, Door, Ladder, Storage, Bed, Mount, Dismount }

    private struct Candidate
    {
        public Kind kind;
        public Component component;
        public float distance;
        public bool inReach;
    }

    private readonly List<Candidate> candidates = new List<Candidate>();
    // MaterialPropertyBlock을 가진 도우미는 컴포넌트 생성 시점(필드 초기화)이 아니라 Awake에서 만들어야 한다.
    private OutlineDrawer outlineDrawer;

    private ResourceGatherer gatherer;
    private LadderClimber climber;
    private HorseRider rider;
    private PlayerRespawn respawn;
    private Health health;
    private Material reachMaterial;
    private Material nearMaterial;
    private Candidate focus;
    private string prompt = "";

    // Interact는 길게 눌러야 실행되는 입력이다. 눌린 시간을 따로 재서 진행 링에 쓴다.
    private InputAction interactAction;
    private float heldTime;
    private bool holdConsumed;
    private bool holdStartedOnTarget;

    // 철거 대상 강조처럼 같은 외곽선 셰이더를 쓰는 다른 컴포넌트가 가져다 쓴다.
    public Shader OutlineShader => outlineShader;

    // 길게 누르기 진행(0~1)이다. 대상이 있는 채로 눌러 아직 실행되기 전일 때만 0보다 크다.
    public float HoldProgress { get; private set; }

    // 보관함을 열라는 요청이다. HUD가 구독해 보관함 패널을 연다.
    public event Action<StorageContainer> StorageRequested;

    // 안내 문구가 바뀔 때 알린다(빈 문자열이면 숨긴다). HUD가 구독한다.
    public event Action<string> PromptChanged;

    private void Awake()
    {
        outlineDrawer = new OutlineDrawer();
        gatherer = GetComponent<ResourceGatherer>();
        climber = GetComponent<LadderClimber>();
        rider = GetComponent<HorseRider>();
        respawn = GetComponent<PlayerRespawn>();
        health = GetComponent<Health>();
        PlayerInput playerInput = GetComponent<PlayerInput>();
        if (playerInput != null && playerInput.actions != null) interactAction = playerInput.actions.FindAction("Interact");

        if (outlineShader != null)
        {
            reachMaterial = new Material(outlineShader) { color = reachColor, hideFlags = HideFlags.HideAndDontSave };
            nearMaterial = new Material(outlineShader) { color = nearColor, hideFlags = HideFlags.HideAndDontSave };
        }
    }

    private void OnDestroy()
    {
        if (reachMaterial != null) Destroy(reachMaterial);
        if (nearMaterial != null) Destroy(nearMaterial);
    }

    private void Update()
    {
        candidates.Clear();
        focus = default;

        bool climbing = climber != null && climber.IsClimbing;
        bool blocked = HudController.AnyPanelOpen || (health != null && health.IsDead) || climbing;
        bool riding = rider != null && rider.IsMounted;
        if (!blocked && riding)
        {
            // 말을 타는 동안은 내리기만 할 수 있다.
            focus = new Candidate { kind = Kind.Dismount, component = rider.Mount, distance = 0f, inReach = true };
        }
        else if (!blocked)
        {
            CollectGatherNodes();
            CollectDoors();
            CollectLadders();
            CollectStorages();
            CollectBeds();
            CollectMounts();

            float best = float.MaxValue;
            foreach (Candidate candidate in candidates)
            {
                if (!candidate.inReach || candidate.distance >= best) continue;
                best = candidate.distance;
                focus = candidate;
            }
        }

        string text = focus.kind == Kind.None ? "" : BuildPrompt(focus);
        // 사다리를 타는 동안은 대상이 없으므로 조작 안내를 대신 보인다.
        if (climbing && !(health != null && health.IsDead)) text = $"[W/S] 오르내리기  ·  [Space] 뛰어내리기  ·  [{InteractKey} 길게] 놓기";
        SetPrompt(text);
        UpdateHoldProgress(focus.kind != Kind.None || climbing);
    }

    // 눌려 있는 시간을 재서 HoldProgress를 갱신한다. 대상이 있을 때 누르기 시작한 경우만 센다(다른 곳에서 누른 채로 걸어와도 링이 뜨지 않는다).
    private void UpdateHoldProgress(bool hasTarget)
    {
        if (interactAction == null) return;

        bool pressed = interactAction.IsPressed();
        if (!pressed)
        {
            heldTime = 0f;
            holdConsumed = false;
            holdStartedOnTarget = false;
            HoldProgress = 0f;
            return;
        }

        if (interactAction.WasPressedThisFrame()) holdStartedOnTarget = hasTarget;
        if (!hasTarget || !holdStartedOnTarget || holdConsumed)
        {
            HoldProgress = 0f;
            return;
        }

        heldTime += Time.unscaledDeltaTime;
        float duration = Mathf.Max(0.05f, InputSystem.settings.defaultHoldTime);
        HoldProgress = Mathf.Clamp01(heldTime / duration);
        if (HoldProgress >= 1f)
        {
            holdConsumed = true;
            HoldProgress = 0f;
        }
    }

    private void LateUpdate()
    {
        if (reachMaterial == null) return;

        foreach (Candidate candidate in candidates)
        {
            // 채집 대상은 반짝임(ResourceSparkleField)이 알려 주므로 테두리를 그리지 않는다(안내 문구와 채집은 그대로다).
            if (candidate.kind == Kind.Gather) continue;

            bool isFocus = candidate.component == focus.component && focus.kind != Kind.None;
            // 눈에 띄도록 가까운 대상의 테두리는 살짝 맥동한다.
            float pulse = isFocus ? 1f + 0.25f * Mathf.Sin(Time.time * 6f) : 1f;
            outlineDrawer.Draw(candidate.component, isFocus ? reachMaterial : nearMaterial, (isFocus ? reachWidth : nearWidth) * pulse);
        }
    }

    private void CollectGatherNodes()
    {
        float reach = gatherer != null ? gatherer.GatherRange : 2.5f;
        foreach (ResourceNode node in ResourceNode.Active)
        {
            if (node == null || !node.HasYield) continue;

            float distance = DistanceTo(node);
            if (distance > nearRange) continue;
            candidates.Add(new Candidate { kind = Kind.Gather, component = node, distance = distance, inReach = distance <= reach });
        }
    }

    private void CollectDoors()
    {
        foreach (BuildableDoor door in BuildableDoor.Active)
        {
            if (door == null || !IsPlaced(door)) continue;

            float distance = Vector3.Distance(door.transform.position, transform.position);
            if (distance > nearRange) continue;
            candidates.Add(new Candidate { kind = Kind.Door, component = door, distance = distance, inReach = distance <= doorRange });
        }
    }

    private void CollectLadders()
    {
        foreach (BuildableLadder ladder in BuildableLadder.Active)
        {
            if (ladder == null || !IsPlaced(ladder)) continue;

            float distance = Vector3.Distance(ladder.transform.position, transform.position);
            if (distance > nearRange) continue;

            float grabDistance = float.MaxValue;
            bool reach = climber != null && climber.CanGrab(ladder, out grabDistance);
            candidates.Add(new Candidate { kind = Kind.Ladder, component = ladder, distance = reach ? grabDistance : distance, inReach = reach });
        }
    }

    private void CollectMounts()
    {
        foreach (Mountable horse in Mountable.Active)
        {
            if (horse == null || !horse.CanBeMounted) continue;

            float distance = Vector3.Distance(horse.transform.position, transform.position);
            if (distance > nearRange) continue;

            float reachDistance = distance;
            bool reach = rider != null && rider.CanMount(horse, out reachDistance);
            candidates.Add(new Candidate { kind = Kind.Mount, component = horse, distance = reach ? reachDistance : distance, inReach = reach });
        }
    }

    private void CollectBeds()
    {
        foreach (RespawnPoint bed in RespawnPoint.Active)
        {
            if (bed == null || !IsPlaced(bed)) continue;

            float distance = Vector3.Distance(bed.transform.position, transform.position);
            if (distance > nearRange) continue;
            candidates.Add(new Candidate { kind = Kind.Bed, component = bed, distance = distance, inReach = distance <= storageRange });
        }
    }

    private void CollectStorages()
    {
        foreach (StorageContainer storage in StorageContainer.Active)
        {
            if (storage == null || !IsPlaced(storage)) continue;

            float distance = Vector3.Distance(storage.transform.position, transform.position);
            if (distance > nearRange) continue;
            candidates.Add(new Candidate { kind = Kind.Storage, component = storage, distance = distance, inReach = distance <= storageRange });
        }
    }

    // 건축 고스트(미리보기)는 월드 id가 없어서 상호작용 대상이 아니다.
    private static bool IsPlaced(Component component)
    {
        BuildableInstance instance = component.GetComponent<BuildableInstance>();
        return instance == null || !string.IsNullOrEmpty(instance.WorldId);
    }

    // 노드 콜라이더의 가장 가까운 점까지의 거리다(채집 범위 판정과 같은 기준).
    private float DistanceTo(ResourceNode node)
    {
        Collider collider = node.GetComponent<Collider>();
        Vector3 point = collider != null ? collider.ClosestPoint(transform.position) : node.transform.position;
        return Vector3.Distance(point, transform.position);
    }

    // 상호작용 키 이름이다(옵션에서 바꾼 키를 따라간다).
    private static string InteractKey => KeyBindings.Label(GameKey.Interact);

    private string BuildPrompt(Candidate target)
    {
        switch (target.kind)
        {
            case Kind.Gather:
                ResourceNode node = (ResourceNode)target.component;
                return $"[{InteractKey} 길게] 채집  ·  {node.Label} (남은 {node.RemainingYield})";
            case Kind.Door:
                return ((BuildableDoor)target.component).IsOpen ? $"[{InteractKey} 길게] 문 닫기" : $"[{InteractKey} 길게] 문 열기";
            case Kind.Ladder:
                return $"[{InteractKey} 길게] 사다리 오르기";
            case Kind.Storage:
            {
                StorageContainer storage = (StorageContainer)target.component;
                string text = $"[{InteractKey} 길게] {storage.DisplayName} 열기";
                if (storage.RemainingSeconds >= 0f)
                {
                    int seconds = Mathf.CeilToInt(storage.RemainingSeconds);
                    text += $"  ·  남은 {seconds / 60}:{seconds % 60:00}";
                }
                return text;
            }
            case Kind.Mount:
                return $"[{InteractKey} 길게] {((Mountable)target.component).DisplayName} 타기";
            case Kind.Dismount:
                return $"[{InteractKey} 길게] 내리기  ·  [W/A/S/D] 이동  ·  [Shift] 질주  ·  [Space] 점프";
            case Kind.Bed:
                return respawn != null && respawn.Bed == (RespawnPoint)target.component
                    ? $"[{InteractKey} 길게] 침대  ·  부활 위치로 지정됨"
                    : $"[{InteractKey} 길게] 침대 사용  ·  부활 위치로 지정";
            default:
                return "";
        }
    }

    private void SetPrompt(string value)
    {
        if (value == prompt) return;
        prompt = value;
        PromptChanged?.Invoke(prompt);
    }

    // E 입력은 가장 가까운 대상 하나에만 전달한다. 오르는 중의 놓기는 LadderClimber가 처리한다.
    private void OnInteract(InputValue value)
    {
        if (value.isPressed) Activate();
    }

    // 지금 강조된 대상에게 동작을 실행한다. 실행했으면 true다.
    public bool Activate()
    {
        switch (focus.kind)
        {
            case Kind.Gather:
                return gatherer != null && gatherer.GatherFrom((ResourceNode)focus.component);
            case Kind.Door:
                BuildableDoor door = (BuildableDoor)focus.component;
                // 참가자는 서버에 요청하고, 문이 움직이는 것은 서버가 알려 준다.
                if (!MultiplayerSession.IsClientOnly) door.Toggle(transform.position);
                else if (BuildSync.Instance != null) BuildSync.Instance.RequestDoor(door.GetComponent<BuildableInstance>().WorldId);
                return true;
            case Kind.Ladder:
                return climber != null && climber.TryGrab((BuildableLadder)focus.component);
            case Kind.Storage:
                // 참가자는 HUD가 서버에 열기를 요청하고 내용은 서버 스냅샷으로 받는다.
                StorageRequested?.Invoke((StorageContainer)focus.component);
                return true;
            case Kind.Bed:
                // 침대 복제본이 부활 위치로 지정된다(부활은 소유자가 자기 플레이어를 옮기는 기존 방식이다).
                return respawn != null && respawn.SetBed((RespawnPoint)focus.component);
            case Kind.Mount:
                return rider != null && rider.TryMount((Mountable)focus.component);
            case Kind.Dismount:
                if (rider == null) return false;
                rider.Dismount();
                return true;
            default:
                return false;
        }
    }
}
