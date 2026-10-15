// 건축 배치 흐름(프리뷰 -> 설치 요청 -> 자원 차감 -> 스폰)을 처리하는 컴포넌트다. 싱글과 호스트는 로컬 권위, 멀티플레이 참가자는 서버에 요청하고 서버가 같은 규칙으로 검증해 확정한다.
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// 설치/철거를 시도할 때의 판정 결과다. 설치가 Ok가 아니면 고스트를 붉게 칠하고, 거부되면 이유를 알린다.
public enum BuildPlacementStatus
{
    Ok,
    Dead,
    NoResources,
    NeedsUpperLevel,
    NoSupport,
    Occupied,
    // 철거 시도 전용: 조준한 건축물이 없거나, 위에 다른 건축물이 얹혀 있다.
    NothingToDemolish,
    HasDependents,
    // 가구가 다른 건축물과 겹친다.
    Overlapping,
}

[RequireComponent(typeof(Inventory))]
public class BuildingSystem : MonoBehaviour
{
    [SerializeField] private BuildableDefinition[] buildables = System.Array.Empty<BuildableDefinition>();
    [SerializeField] private float placeDistance = 3f;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float gridSize = 2f;
    [SerializeField] private float rotationStep = 90f;
    // 한 층의 높이(m)다. 벽 높이와 같아서 위층 바닥이 아래층 벽 위에 얹힌다.
    [SerializeField] private float levelHeight = 2f;
    [SerializeField] private int maxLevel = 3;
    // 벽에 붙여 놓는 조각이 벽을 찾는 거리(m)다.
    [SerializeField] private float wallAttachRange = 1.6f;
    // 철거할 때 설치 비용의 얼마를 돌려받는지(올림)다.
    [SerializeField, Range(0f, 1f)] private float refundRatio = 0.5f;
    [SerializeField] private float demolishRange = 8f;
    // 서버가 참가자의 요청 위치를 검증할 때, 지연과 이동 때문에 클라이언트 값이 어긋날 수 있어 봐주는 거리(m)다.
    private const float RemoteTolerance = 4f;

    // 숫자 키 1~9로도 buildables 배열 인덱스를 고를 수 있다(건축 메뉴 UI의 보조 단축키).
    private static readonly Key[] SelectionKeys =
    {
        Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
        Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9,
    };

    private Inventory inventory;
    private Health health;
    private WeaponEquipment weaponEquipment;
    private VisualElement hudRoot;
    // 홀로그램 고스트 재질(Resources 안이라 빌드에서 셰이더가 빠지지 않는다)과 색이다. 설치 가능은 파랑, 불가능은 빨강이다.
    private const string HologramMaterialPath = "BuildGhost";
    private static readonly Color GhostOkColor = new Color(0.25f, 0.85f, 1f, 0.6f);
    private static readonly Color GhostBlockedColor = new Color(1f, 0.2f, 0.18f, 0.6f);

    private GameObject ghost;
    // 고스트 재질과 원래 색이다. 설치할 수 없을 때 붉게 칠했다가 되돌리는 데 쓴다.
    private readonly System.Collections.Generic.List<Material> ghostMaterials = new System.Collections.Generic.List<Material>();
    private readonly System.Collections.Generic.List<Color> ghostBaseColors = new System.Collections.Generic.List<Color>();
    private BuildPlacementStatus ghostStatus = BuildPlacementStatus.Ok;
    private float nextStatusTime;
    // 벽에 붙는 조각을 놓으려는 자리의 벽이다(설치하면 이 벽에 의존한다).
    private BuildableInstance attachedWall;
    // 철거 대상 강조: 외곽선 도우미와 색 재질, 대상이 바뀔 때만 다시 계산하는 받침 여부다.
    private OutlineDrawer outlineDrawer;
    private Material demolishMaterial;
    private Material demolishBlockedMaterial;
    private BuildableInstance demolishTarget;
    private bool demolishTargetBlocked;
    private float ghostYaw;
    private int selectedIndex;
    // 플레이어가 선 높이로 정해지는 기본 층에 R/F로 더하는 값이다.
    private int levelOffset;
    private int ghostLevel;

    public int GhostLevel => ghostLevel;
    public bool BuildModeActive { get; private set; }
    // 건축 모드는 유지한 채 선택 메뉴만 닫을 수 있으므로 모드와 별도로 추적한다.
    public bool BuildMenuOpen { get; private set; }
    public BuildableDefinition[] Buildables => buildables;
    public int SelectedIndex => selectedIndex;

    // 인벤토리/제작 패널을 열 때 건축 모드와 겹치지 않도록 HudController가 호출한다.
    public void ExitBuildMode()
    {
        if (BuildModeActive) SetBuildMode(false);
    }

    // 건축 메뉴가 열리고 닫힐 때, 선택이 바뀔 때 UI가 이 이벤트로 갱신한다.
    public event System.Action<bool> BuildMenuChanged;
    public event System.Action<int> BuildableSelected;
    // 설치 시도가 거부될 때 이유와 함께 알린다.
    public event System.Action<BuildPlacementStatus> PlacementRejected;

    public void SetBuildMenuOpen(bool open)
    {
        if (BuildMenuOpen == open) return;
        BuildMenuOpen = open;
        BuildMenuChanged?.Invoke(open);
    }

    private BuildableDefinition CurrentBuildable =>
        selectedIndex >= 0 && selectedIndex < buildables.Length ? buildables[selectedIndex] : null;

    private void Awake()
    {
        inventory = GetComponent<Inventory>();
        health = GetComponent<Health>();
        weaponEquipment = GetComponent<WeaponEquipment>();
        outlineDrawer = new OutlineDrawer();
        UIDocument hudDocument = FindFirstObjectByType<UIDocument>();
        if (hudDocument != null) hudRoot = hudDocument.rootVisualElement;
    }

    private void OnDestroy()
    {
        if (demolishMaterial != null) Destroy(demolishMaterial);
        if (demolishBlockedMaterial != null) Destroy(demolishBlockedMaterial);
    }

    // 건축 모드에서 조준한 건축물에 테두리를 그려 X로 철거될 대상을 알린다(위에 얹힌 것이 있으면 흐린 색).
    private void LateUpdate()
    {
        if (!BuildModeActive || BuildMenuOpen)
        {
            demolishTarget = null;
            return;
        }

        BuildableInstance aimed = FindAimedBuildable();
        if (aimed != demolishTarget)
        {
            demolishTarget = aimed;
            demolishTargetBlocked = aimed != null && HasDependents(aimed);
        }
        if (demolishTarget == null) return;

        EnsureDemolishMaterials();
        if (demolishMaterial == null) return;
        float pulse = 1f + 0.25f * Mathf.Sin(Time.time * 6f);
        outlineDrawer.Draw(demolishTarget, demolishTargetBlocked ? demolishBlockedMaterial : demolishMaterial, 0.06f * pulse);
    }

    private void EnsureDemolishMaterials()
    {
        if (demolishMaterial != null) return;

        InteractionFocus focus = GetComponent<InteractionFocus>();
        if (focus == null || focus.OutlineShader == null) return;
        demolishMaterial = new Material(focus.OutlineShader) { color = new Color(1f, 0.2f, 0.15f, 0.95f), hideFlags = HideFlags.HideAndDontSave };
        demolishBlockedMaterial = new Material(focus.OutlineShader) { color = new Color(0.7f, 0.7f, 0.7f, 0.5f), hideFlags = HideFlags.HideAndDontSave };
    }

    private void OnBuildMode(InputValue value)
    {
        if (!value.isPressed) return;
        // 재장전 중에는 건축 모드로 들어가지 않는다(재장전이 어중간하게 끊기지 않게).
        if (!BuildModeActive && weaponEquipment != null && weaponEquipment.IsReloading) return;
        // 모드 꺼짐 -> 모드+메뉴 켜기, 메뉴만 닫힌 상태 -> 메뉴 다시 열기, 메뉴 열림 -> 모드 끄기.
        if (!BuildModeActive) SetBuildMode(true);
        else if (!BuildMenuOpen) SetBuildMenuOpen(true);
        else SetBuildMode(false);
    }

    private void OnFire(InputValue value)
    {
        if (!BuildModeActive || !value.isPressed) return;
        // 건축 메뉴의 "선택" 버튼을 클릭한 것이 동시에 배치 시도로 처리되지 않게 막는다.
        if (IsPointerOverInteractiveUI()) return;
        TryPlace();
    }

    // UI Toolkit은 UGUI EventSystem을 쓰지 않으므로 패널 자체의 피킹으로 UI 위 클릭인지 판단한다.
    // picking-mode가 Ignore인 장식 요소(스캔라인/노이즈/코너)는 자동으로 무시된다.
    private bool IsPointerOverInteractiveUI()
    {
        if (hudRoot == null || Mouse.current == null) return false;
        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(hudRoot.panel, screenPos);
        VisualElement picked = hudRoot.panel.Pick(panelPos);
        return picked != null && picked != hudRoot;
    }

    private void Update()
    {
        if (!BuildModeActive) return;

        if (Keyboard.current != null)
        {
            for (int i = 0; i < buildables.Length && i < SelectionKeys.Length; i++)
            {
                if (Keyboard.current[SelectionKeys[i]].wasPressedThisFrame)
                {
                    SelectBuildable(i);
                    break;
                }
            }

            if (ghost != null)
            {
                if (KeyBindings.Pressed(GameKey.RotateLeft)) ghostYaw -= rotationStep;
                if (KeyBindings.Pressed(GameKey.RotateRight)) ghostYaw += rotationStep;
                if (KeyBindings.Pressed(GameKey.LevelUp)) levelOffset++;
                if (KeyBindings.Pressed(GameKey.LevelDown)) levelOffset--;
                if (KeyBindings.Pressed(GameKey.Demolish)) TryDemolish();
            }
        }

        if (ghost != null)
        {
            UpdateGhostTransform();
            // 판정은 건축물 목록을 훑으므로 매 프레임이 아니라 0.1초마다 갱신한다.
            if (Time.time >= nextStatusTime)
            {
                nextStatusTime = Time.time + 0.1f;
                UpdateGhostTint(GetPlacementStatus());
            }
        }
    }

    // 건축 메뉴 UI의 목록 버튼이 이 오버로드를 호출한다.
    public void SelectBuildable(BuildableDefinition definition)
    {
        int index = System.Array.IndexOf(buildables, definition);
        if (index < 0) return;
        SelectBuildable(index);
    }

    public void SelectBuildable(int index)
    {
        if (index == selectedIndex && ghost != null) return;
        selectedIndex = index;

        if (ghost != null)
        {
            Destroy(ghost);
            ghost = null;
        }
        SpawnGhost();
        BuildableSelected?.Invoke(selectedIndex);
    }

    private void SpawnGhost()
    {
        BuildableDefinition current = CurrentBuildable;
        if (current == null || current.Prefab == null) return;

        ghost = Instantiate(current.Prefab);
        SetGhostVisual(ghost);
    }

    private void SetBuildMode(bool active)
    {
        BuildModeActive = active;

        if (active && ghost == null)
        {
            ghostYaw = 0f;
            SpawnGhost();
        }
        else if (!active && ghost != null)
        {
            Destroy(ghost);
            ghost = null;
        }

        // 커서 잠금 해제는 메뉴가 열릴 때 HudController가 처리한다.
        SetBuildMenuOpen(active);
    }

    private void UpdateGhostTransform()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // 카메라 위치를 기준으로 계산하면 3인칭 카메라가 플레이어 뒤에 떨어져 있어
        // placeDistance가 카메라-플레이어 거리와 비슷할 때 고스트가 플레이어 근처에서만 맴돈다.
        // 플레이어 위치를 기준으로 카메라가 보는 방향으로 투사해야 시점과 무관하게 정확히 앞으로 나간다.
        Vector3 aimDirection = cam.transform.forward;
        Vector3 targetPoint = transform.position + aimDirection * placeDistance;
        float groundY = FindGroundHeight(targetPoint, transform.position.y);
        targetPoint.y = groundY;

        // 층은 지면에서 플레이어가 선 높이로 정하고 R/F로 조절한다. 고스트는 다른 건축물 위로 올라타지 않는다.
        int autoLevel = Mathf.Max(0, Mathf.RoundToInt((transform.position.y - groundY) / levelHeight));
        ghostLevel = Mathf.Clamp(autoLevel + levelOffset, 0, maxLevel);
        levelOffset = ghostLevel - autoLevel;
        targetPoint.y = groundY + ghostLevel * levelHeight;

        BuildableDefinition definition = CurrentBuildable;
        if (definition != null && definition.WallAttach != WallAttach.None
            && TryAttachToWall(definition, targetPoint, out Vector3 attachedPosition, out float attachedYaw))
        {
            ghost.transform.position = attachedPosition;
            ghost.transform.rotation = Quaternion.Euler(0f, attachedYaw, 0f);
            return;
        }

        targetPoint = SnapPosition(targetPoint, definition, ghostYaw);

        ghost.transform.position = targetPoint;
        ghost.transform.rotation = Quaternion.Euler(0f, ghostYaw, 0f);
    }

    // 건축물과 자기 자신을 건너뛴 첫 지면의 높이다. 아무것도 맞지 않으면 fallbackY를 쓴다.
    private float FindGroundHeight(Vector3 point, float fallbackY)
    {
        RaycastHit[] hits = Physics.RaycastAll(point + Vector3.up * 5f, Vector3.down, 30f, groundMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.GetComponentInParent<BuildableInstance>() != null) continue;
            if (hit.collider.transform.IsChildOf(transform)) continue;
            // 멀티플레이 접속자 아바타 위에 서 있는 것을 지면으로 착각하지 않는다.
            if (hit.collider.GetComponentInParent<NetworkPlayerAvatar>() != null) continue;
            return hit.point.y;
        }
        return fallbackY;
    }

    // 지면보다 위층이면 발판이 될 건축물이 바로 아래에 있어야 한다.
    private bool HasSupport(GameObject target, int level)
    {
        if (level == 0) return true;

        Bounds bounds = BoundsOf(target);

        Vector3 center = new Vector3(bounds.center.x, target.transform.position.y - 0.05f, bounds.center.z);
        Vector3 halfExtents = new Vector3(Mathf.Max(0.05f, bounds.extents.x - 0.05f), 0.04f, Mathf.Max(0.05f, bounds.extents.z - 0.05f));
        foreach (Collider collider in Physics.OverlapBox(center, halfExtents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
        {
            if (collider.transform.IsChildOf(target.transform)) continue;
            if (collider.GetComponentInParent<BuildableInstance>() != null) return true;
        }
        return false;
    }

    // 격자 자리 규칙은 BuildSnapMode를 따른다. Edge는 조각의 긴 변이 가리키는 축(yaw 0/180은 x, 90/270은 z)을 격자 배수에 맞춘다.
    public static Vector3 SnapPosition(Vector3 point, BuildableDefinition definition, float yaw, float defaultGrid = 2f)
    {
        float snap = definition != null && definition.SnapSize > 0f ? definition.SnapSize : defaultGrid;
        float half = snap * 0.5f;
        BuildSnapMode mode = definition != null ? definition.SnapMode : BuildSnapMode.Center;

        bool alongX = Mathf.Abs(Mathf.Sin(yaw * Mathf.Deg2Rad)) < 0.5f;
        float offsetX = 0f, offsetZ = 0f;
        if (mode == BuildSnapMode.Vertex) { offsetX = half; offsetZ = half; }
        else if (mode == BuildSnapMode.Edge)
        {
            if (alongX) offsetZ = half;
            else offsetX = half;
        }

        point.x = Mathf.Round((point.x - offsetX) / snap) * snap + offsetX;
        point.z = Mathf.Round((point.z - offsetZ) / snap) * snap + offsetZ;
        return point;
    }

    // 조준점 근처의 벽/창문/문(같은 층)에 뒷면을 붙이고 앞면이 벽에서 멀어지게 놓는다. 벽을 따라서는 조각의 격자(0.5m)로 스냅한다.
    private bool TryAttachToWall(BuildableDefinition definition, Vector3 target, out Vector3 position, out float yaw)
    {
        position = target;
        yaw = ghostYaw;

        attachedWall = null;
        Bounds wallBounds = default;
        BuildableInstance wallInstance = null;
        bool found = false;
        float bestSqr = wallAttachRange * wallAttachRange;
        foreach (BuildableInstance instance in FindObjectsByType<BuildableInstance>(FindObjectsSortMode.None))
        {
            if (instance.gameObject == ghost || string.IsNullOrEmpty(instance.WorldId)) continue;
            if (!IsWallLike(instance) || !TryGetColliderBounds(instance, out Bounds bounds)) continue;
            // 같은 층의 벽만 본다(벽 바닥이 고스트 높이 근처).
            if (Mathf.Abs(bounds.min.y - target.y) > levelHeight * 0.75f) continue;

            float sqr = bounds.SqrDistance(new Vector3(target.x, bounds.center.y, target.z));
            if (sqr >= bestSqr) continue;
            bestSqr = sqr;
            wallBounds = bounds;
            wallInstance = instance;
            found = true;
        }
        if (!found) return false;
        attachedWall = wallInstance;

        // 벽의 긴 변이 x면 벽면은 z 방향이다. 조준점이 있는 쪽 면에 붙인다.
        bool longAlongX = wallBounds.size.x >= wallBounds.size.z;
        Vector3 normal = longAlongX ? Vector3.forward : Vector3.right;
        float side = Vector3.Dot(target - wallBounds.center, normal) >= 0f ? 1f : -1f;
        Vector3 away = normal * side;

        Vector3 front = definition.WallAttach == WallAttach.FrontPlusZ ? away : -away;
        yaw = Mathf.Atan2(front.x, front.z) * Mathf.Rad2Deg;

        float grid = SnapOf(definition);
        float along = longAlongX ? target.x : target.z;
        float min = longAlongX ? wallBounds.min.x : wallBounds.min.z;
        float max = longAlongX ? wallBounds.max.x : wallBounds.max.z;
        along = Mathf.Clamp(Mathf.Round(along / grid) * grid, min, max);

        float halfThickness = longAlongX ? wallBounds.extents.z : wallBounds.extents.x;
        Vector3 facePoint = longAlongX
            ? new Vector3(along, target.y, wallBounds.center.z)
            : new Vector3(wallBounds.center.x, target.y, along);
        facePoint += away * halfThickness;

        // 회전을 적용한 고스트의 경계로 원점에서 벽 쪽 면까지의 거리를 구해 그만큼 벽에서 띄운다.
        ghost.transform.SetPositionAndRotation(facePoint, Quaternion.Euler(0f, yaw, 0f));
        Bounds ghostBounds = BoundsOf(ghost);
        Vector3 toWall = -away;
        float depth;
        if (Mathf.Abs(toWall.x) > 0.5f) depth = toWall.x > 0f ? ghostBounds.max.x - facePoint.x : facePoint.x - ghostBounds.min.x;
        else depth = toWall.z > 0f ? ghostBounds.max.z - facePoint.z : facePoint.z - ghostBounds.min.z;

        position = facePoint + away * (Mathf.Max(0f, depth) + 0.02f);
        return true;
    }

    private bool IsWallLike(BuildableInstance instance)
    {
        foreach (BuildableDefinition def in buildables)
        {
            if (def != null && def.BuildableId == instance.BuildableId) return OccupancyGroup(def.Category) == BuildableCategory.Wall;
        }
        return false;
    }

    private static bool TryGetColliderBounds(BuildableInstance instance, out Bounds bounds)
    {
        return BuildableInstance.TryGetColliderBounds(instance.gameObject, out bounds);
    }

    private static Bounds BoundsOf(GameObject target)
    {
        Bounds bounds = new Bounds(target.transform.position, Vector3.zero);
        foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>())
            bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    private float SnapOf(BuildableDefinition definition)
    {
        return definition != null && definition.SnapSize > 0f ? definition.SnapSize : gridSize;
    }

    // 고스트 위치(격자에 스냅된 칸)에 같은 종류(벽/바닥)의 건축물이 이미 있으면 true.
    // 고스트 자신은 프리팹에서 복제된 BuildableInstance를 갖고 있으므로 제외한다.
    private bool IsCellOccupied(BuildableDefinition current, GameObject placing)
    {
        Vector3 target = placing.transform.position;
        float half = SnapOf(current) * 0.5f;
        float maxDistanceSqr = half * half;

        foreach (BuildableInstance instance in FindObjectsByType<BuildableInstance>(FindObjectsSortMode.None))
        {
            if (instance.gameObject == placing) continue;
            if (!IsSameCategory(instance, current)) continue;

            Vector3 offset = instance.transform.position - target;
            // 다른 층의 같은 자리는 겹치지 않는다.
            if (Mathf.Abs(offset.y) > levelHeight * 0.5f) continue;
            offset.y = 0f;
            if (offset.sqrMagnitude < maxDistanceSqr) return true;
        }
        return false;
    }

    // 문과 창문은 벽이 서는 자리에 서므로 벽과 같은 칸을 다툰다.
    private static BuildableCategory OccupancyGroup(BuildableCategory category)
    {
        return category == BuildableCategory.Door || category == BuildableCategory.Window ? BuildableCategory.Wall : category;
    }

    private bool IsSameCategory(BuildableInstance instance, BuildableDefinition current)
    {
        foreach (BuildableDefinition def in buildables)
        {
            if (def != null && def.BuildableId == instance.BuildableId) return OccupancyGroup(def.Category) == OccupancyGroup(current.Category);
        }
        return false;
    }

    private static bool CanAfford(BuildableDefinition current, Inventory payer)
    {
        if (current == null || payer == null) return false;
        foreach (BuildableDefinition.Cost cost in current.Costs)
        {
            if (cost.item == null) continue;
            if (payer.GetQuantity(cost.item) < cost.quantity) return false;
        }
        return true;
    }

    // 지금 고스트 자리에 설치할 수 있는지 순서대로 따져 첫 번째로 막는 이유를 돌려준다.
    private BuildPlacementStatus GetPlacementStatus()
    {
        if (health != null && health.IsDead) return BuildPlacementStatus.Dead;
        return EvaluatePlacement(CurrentBuildable, ghost, ghostLevel, inventory);
    }

    // placing 자리에 설치할 수 있는지 따진다. 로컬 고스트와 서버가 참가자의 요청을 검증할 때 같은 규칙을 쓴다.
    private BuildPlacementStatus EvaluatePlacement(BuildableDefinition current, GameObject placing, int level, Inventory payer)
    {
        if (current == null || placing == null) return BuildPlacementStatus.Dead;
        if (!CanAfford(current, payer)) return BuildPlacementStatus.NoResources;
        if (current.Category == BuildableCategory.Roof && level < 1) return BuildPlacementStatus.NeedsUpperLevel;
        if (!HasSupport(placing, level)) return BuildPlacementStatus.NoSupport;
        if (IsCellOccupied(current, placing)) return BuildPlacementStatus.Occupied;
        if (current.Category == BuildableCategory.Furniture && OverlapsOtherBuildable(placing)) return BuildPlacementStatus.Overlapping;
        return BuildPlacementStatus.Ok;
    }

    // 조준한 건축물을 철거하고 설치 비용의 일부를 돌려준다. 위에 다른 건축물이 얹혀 있으면 거부한다.
    private void TryDemolish()
    {
        if (health != null && health.IsDead) return;

        BuildableInstance target = FindAimedBuildable();
        if (target == null)
        {
            PlacementRejected?.Invoke(BuildPlacementStatus.NothingToDemolish);
            return;
        }
        if (HasDependents(target))
        {
            PlacementRejected?.Invoke(BuildPlacementStatus.HasDependents);
            return;
        }

        // 참가자는 서버에 요청만 한다. 철거와 환불은 서버가 확정하고 건축물이 사라지는 것은 서버가 알려 준다.
        if (MultiplayerSession.IsClientOnly)
        {
            if (BuildSync.Instance != null) BuildSync.Instance.RequestDemolish(target.WorldId);
            return;
        }

        DemolishNow(target, (item, quantity) => inventory.AddPickup(item, quantity));
    }

    // 환불을 give로 주고 파편을 튀기며 바로 사라지게 한다(꺼지므로 같은 프레임에 다시 조준/철거되지 않는다).
    private void DemolishNow(BuildableInstance target, System.Action<ItemDefinition, int> give)
    {
        BuildableDefinition definition = BuildableDatabase.Instance != null ? BuildableDatabase.Instance.GetById(target.BuildableId) : null;
        if (definition != null)
        {
            foreach (BuildableDefinition.Cost cost in definition.Costs)
            {
                if (cost.item == null) continue;
                int refund = Mathf.CeilToInt(cost.quantity * refundRatio);
                if (refund > 0) give(cost.item, refund);
            }
        }
        NoiseSystem.Emit(target.transform.position, 12f, this);
        BuildableDebris.Crumble(target, 0f);
    }

    // 서버가 참가자의 철거 요청을 검증하고 실행한다. 조용히 거부하는 경우(범위 밖 등)는 Dead를 돌려준다.
    public BuildPlacementStatus ServerDemolish(Vector3 avatarPosition, string worldId, System.Action<ItemDefinition, int> give)
    {
        BuildableInstance target = BuildableInstance.FindByWorldId(worldId);
        if (target == null) return BuildPlacementStatus.NothingToDemolish;
        if (!BuildableInstance.TryGetColliderBounds(target.gameObject, out Bounds bounds)
            || Vector3.Distance(avatarPosition, bounds.ClosestPoint(avatarPosition)) > demolishRange + RemoteTolerance)
            return BuildPlacementStatus.Dead;
        if (HasDependents(target)) return BuildPlacementStatus.HasDependents;

        DemolishNow(target, give);
        return BuildPlacementStatus.Ok;
    }

    // 화면 중앙에서 쏜 선이 처음 맞는 건축물이다. 건축물이 아닌 것에 먼저 막히면 대상이 없다.
    private BuildableInstance FindAimedBuildable()
    {
        return StructureInspector.FindAimed(transform, demolishRange);
    }

    // 철거하면 받침을 잃는 건축물(벽에 붙은 가구/사다리, 다른 받침이 없는 위층 조각)이 있으면 true.
    private static bool HasDependents(BuildableInstance target)
    {
        return BuildableSupport.FindOrphans(target, new[] { target }).Count > 0;
    }

    // 고스트가 이미 놓인 다른 건축물과 겹치는지 본다(가구가 벽이나 다른 가구를 파고들지 않게). 경계를 살짝 줄여 맞닿는 것은 허용한다.
    private bool OverlapsOtherBuildable(GameObject placing)
    {
        Bounds bounds = BoundsOf(placing);
        Vector3 halfExtents = new Vector3(Mathf.Max(0.02f, bounds.extents.x - 0.08f), Mathf.Max(0.02f, bounds.extents.y - 0.08f), Mathf.Max(0.02f, bounds.extents.z - 0.08f));
        foreach (Collider collider in Physics.OverlapBox(bounds.center, halfExtents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
        {
            if (collider.transform.IsChildOf(placing.transform)) continue;
            BuildableInstance other = collider.GetComponentInParent<BuildableInstance>();
            if (other != null && !string.IsNullOrEmpty(other.WorldId)) return true;
        }
        return false;
    }

    private void TryPlace()
    {
        BuildableDefinition current = CurrentBuildable;
        BuildPlacementStatus status = GetPlacementStatus();
        if (status != BuildPlacementStatus.Ok)
        {
            if (status != BuildPlacementStatus.Dead) PlacementRejected?.Invoke(status);
            return;
        }

        // 참가자는 서버에 요청만 한다. 자재 차감과 설치는 서버가 확정하고 건축물이 생기는 것은 서버가 알려 준다.
        if (MultiplayerSession.IsClientOnly)
        {
            if (BuildSync.Instance == null) return;
            string wallId = current.WallAttach != WallAttach.None && attachedWall != null ? attachedWall.WorldId : "";
            BuildSync.Instance.RequestPlace(current.BuildableId, ghost.transform.position, ghost.transform.eulerAngles.y, ghostLevel, wallId);
            return;
        }

        PlaceNow(current, ghost.transform.position, ghost.transform.rotation, inventory, current.WallAttach != WallAttach.None ? attachedWall : null);
    }

    // 자재를 차감하고 건축물을 실제로 세운다(싱글, 호스트, 서버가 참가자를 대신해 설치할 때 공용).
    private void PlaceNow(BuildableDefinition current, Vector3 position, Quaternion rotation, Inventory payer, BuildableInstance wall)
    {
        foreach (BuildableDefinition.Cost cost in current.Costs)
        {
            if (cost.item == null) continue;
            payer.Remove(cost.item, cost.quantity);
        }

        GameObject placed = Instantiate(current.Prefab, position, rotation);
        BuildableInstance instance = placed.GetComponent<BuildableInstance>();
        if (instance == null) instance = placed.AddComponent<BuildableInstance>();
        // 받침은 등록(접속자에게 알림)보다 먼저 정해 둔다.
        if (wall != null) instance.SetSupport(wall.WorldId);
        instance.Initialize(current.BuildableId);
        // 망치질 소리가 좀비를 끈다.
        NoiseSystem.Emit(position, 12f, this);
    }

    // 서버가 참가자의 설치 요청을 검증하고 설치한다. 같은 규칙을 임시 고스트로 검사한다. 조용히 거부하는 경우(범위 밖, 맞지 않는 값)는 Dead를 돌려준다.
    public BuildPlacementStatus ServerPlace(Inventory payer, Vector3 avatarPosition, string buildableId, Vector3 position, float yaw, int level, string wallId)
    {
        BuildableDefinition definition = null;
        foreach (BuildableDefinition candidate in buildables)
        {
            if (candidate != null && candidate.BuildableId == buildableId) { definition = candidate; break; }
        }
        if (definition == null || definition.Prefab == null || payer == null) return BuildPlacementStatus.Dead;
        if (!float.IsFinite(position.x) || !float.IsFinite(position.y) || !float.IsFinite(position.z) || !float.IsFinite(yaw)) return BuildPlacementStatus.Dead;
        if (level < 0 || level > maxLevel) return BuildPlacementStatus.Dead;
        if ((position - avatarPosition).sqrMagnitude > (placeDistance + RemoteTolerance) * (placeDistance + RemoteTolerance)) return BuildPlacementStatus.Dead;

        // 지면 높이와 층이 맞아야 한다(클라이언트가 스냅하기 전 조준점으로 층을 정하므로 한 층의 절반까지는 봐준다).
        float groundY = FindGroundHeight(position, position.y);
        if (Mathf.Abs(position.y - (groundY + level * levelHeight)) > levelHeight * 0.45f) return BuildPlacementStatus.Dead;

        // 벽에 붙는 조각은 그 벽이 실제로 가까이 있어야 하고, 아니면 격자에 맞아야 한다.
        BuildableInstance wall = null;
        if (!string.IsNullOrEmpty(wallId))
        {
            wall = BuildableInstance.FindByWorldId(wallId);
            if (definition.WallAttach == WallAttach.None || wall == null || !IsWallLike(wall)
                || !TryGetColliderBounds(wall, out Bounds wallBounds)
                || wallBounds.SqrDistance(position) > (wallAttachRange + 1f) * (wallAttachRange + 1f))
                return BuildPlacementStatus.Dead;
        }
        else if ((SnapPosition(position, definition, yaw) - position).sqrMagnitude > 0.0025f)
        {
            return BuildPlacementStatus.Dead;
        }

        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
        GameObject probe = Instantiate(definition.Prefab, position, rotation);
        foreach (Collider col in probe.GetComponentsInChildren<Collider>()) col.enabled = false;
        BuildPlacementStatus status = EvaluatePlacement(definition, probe, level, payer);
        // Destroy는 프레임 끝에 처리되므로 먼저 꺼서 다음 검사에 잡히지 않게 한다.
        probe.SetActive(false);
        Destroy(probe);
        if (status != BuildPlacementStatus.Ok) return status;

        PlaceNow(definition, position, rotation, payer, wall);
        return BuildPlacementStatus.Ok;
    }

    // 서버가 거부한 설치/철거 결과를 참가자 화면에 알린다(고스트가 붉어지는 것과 같은 안내).
    public void NotifyRejected(BuildPlacementStatus status) => PlacementRejected?.Invoke(status);

    private void SetGhostVisual(GameObject go)
    {
        ghostMaterials.Clear();
        ghostBaseColors.Clear();
        ghostStatus = BuildPlacementStatus.Ok;
        // 프리뷰 단계에서는 실제로 충돌하면 안 되므로 콜라이더만 끈다.
        foreach (Collider col in go.GetComponentsInChildren<Collider>())
            col.enabled = false;

        // 런타임 인스턴스에만 홀로그램 재질을 적용한다 (프리팹에 저장하지 않으므로
        // "프리팹에 런타임 Material을 대입하면 사라진다"는 문제와 무관하다).
        Material hologram = Resources.Load<Material>(HologramMaterialPath);
        foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>())
        {
            if (hologram != null)
            {
                SetHologramGhost(renderer, hologram);
                continue;
            }

            // 홀로그램 재질을 못 찾으면 예전처럼 원본 재질을 반투명으로 바꿔 쓴다.
            Material ghostMat = new Material(renderer.sharedMaterial);
            ghostMat.SetFloat("_Surface", 1f); // URP Lit: 0=Opaque, 1=Transparent
            ghostMat.SetFloat("_Blend", 0f); // Alpha blend
            ghostMat.SetOverrideTag("RenderType", "Transparent");
            ghostMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            ghostMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            ghostMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            ghostMat.SetInt("_ZWrite", 0);
            ghostMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            Color color = ghostMat.HasProperty("_BaseColor") ? ghostMat.GetColor("_BaseColor") : ghostMat.color;
            color.a = 0.45f;
            if (ghostMat.HasProperty("_BaseColor")) ghostMat.SetColor("_BaseColor", color);
            else ghostMat.color = color;

            renderer.sharedMaterial = ghostMat;
            ghostMaterials.Add(ghostMat);
            ghostBaseColors.Add(color);
        }
    }

    // 홀로그램 셰이더(ZGV01/BuildGhost)를 쓰는 고스트다. 원본 재질의 알베도 텍스처를 옅게 섞고 색은 _GhostColor로 바꾼다.
    private void SetHologramGhost(Renderer renderer, Material hologram)
    {
        // 불꽃 같은 파티클은 미리보기에서 숨긴다.
        if (renderer is ParticleSystemRenderer || renderer is TrailRenderer)
        {
            renderer.enabled = false;
            return;
        }

        Material[] originals = renderer.sharedMaterials;
        var materials = new Material[originals.Length];
        for (int i = 0; i < materials.Length; i++)
        {
            Material ghostMat = new Material(hologram);
            Material original = originals[i];
            if (original != null && original.HasProperty("_BaseMap") && original.GetTexture("_BaseMap") != null)
            {
                ghostMat.SetTexture("_BaseMap", original.GetTexture("_BaseMap"));
                ghostMat.SetTextureScale("_BaseMap", original.GetTextureScale("_BaseMap"));
                ghostMat.SetTextureOffset("_BaseMap", original.GetTextureOffset("_BaseMap"));
            }
            ghostMat.SetColor("_GhostColor", GhostOkColor);
            materials[i] = ghostMat;
            ghostMaterials.Add(ghostMat);
            ghostBaseColors.Add(GhostOkColor);
        }
        renderer.sharedMaterials = materials;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    // 설치할 수 없는 자리에서는 고스트를 붉게 칠한다. 상태가 바뀔 때만 재질을 건드린다.
    private void UpdateGhostTint(BuildPlacementStatus status)
    {
        if (status == ghostStatus) return;
        ghostStatus = status;

        bool blocked = status != BuildPlacementStatus.Ok && status != BuildPlacementStatus.Dead;
        for (int i = 0; i < ghostMaterials.Count; i++)
        {
            Material material = ghostMaterials[i];
            if (material == null) continue;
            if (material.HasProperty("_GhostColor"))
            {
                material.SetColor("_GhostColor", blocked ? GhostBlockedColor : ghostBaseColors[i]);
                continue;
            }
            Color color = blocked ? new Color(1f, 0.2f, 0.18f, ghostBaseColors[i].a) : ghostBaseColors[i];
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else material.color = color;
        }
    }
}
