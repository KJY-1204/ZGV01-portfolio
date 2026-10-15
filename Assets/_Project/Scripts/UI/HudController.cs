// 플레이어 Health/Inventory 상태를 UI Toolkit HUD에 표시하는 뷰 바인딩이다. 게임 상태를 직접 수정하지 않는다.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class HudController : MonoBehaviour
{
    [SerializeField] private RecipeDefinition[] recipes = System.Array.Empty<RecipeDefinition>();
    [SerializeField] private float maxNoiseOpacity = 0.3f;
    [SerializeField] private float maxScanlineOpacity = 0.16f;
    [SerializeField] private float noiseHealthThreshold = 0.4f;
    [SerializeField] private float compassPixelsPerDegree = 3f;

    // 인벤토리/제작/건축 패널 중 하나라도 열려있으면 true. 커서가 풀려 클릭 가능한 상태이므로
    // HitscanWeapon 같은 순수 게임플레이 입력이 이 값을 보고 사격을 억제한다(HUD는 하나뿐이라
    // 정적 필드로도 충분하다).
    public static bool AnyPanelOpen { get; private set; }

    private Health playerHealth;
    private Inventory playerInventory;
    private CraftingSystem craftingSystem;
    private BuildingSystem buildingSystem;
    private StarterAssets.StarterAssetsInputs playerInputs;
    private PauseMenu pauseMenu;
    // 인벤토리 아이템 메뉴(사용/버리기)와 지금 메뉴가 가리키는 아이템이다.
    private VisualElement itemMenu;
    private Label itemMenuTitle;
    private Button itemMenuUse, itemMenuDropOne, itemMenuDropAll;
    private ItemDefinition menuItem;
    private int menuQuantity;
    private ItemDropper itemDropper;
    private Vector2Int baseReferenceResolution;
    private WeaponEquipment weaponEquipment;
    private CombatAudio combatAudio;

    private VisualElement ammoReadout;
    private Label ammoMag;
    private Label ammoReserve;
    private bool ammoAttention;

    // 아이템을 주웠을 때 뜨는 알림. 같은 아이템을 연달아 주우면 한 알림에 수량을 합친다.
    private class PickupToast
    {
        public VisualElement Root;
        public Label Quantity;
        public Label Total;
        public int Amount;
        public IVisualElementScheduledItem Fade;
        public IVisualElementScheduledItem Remove;
    }

    private VisualElement pickupFeed;
    private readonly Dictionary<ItemDefinition, PickupToast> pickupToasts = new Dictionary<ItemDefinition, PickupToast>();
    private const int MaxPickupToasts = 5;
    private const long PickupFadeDelayMs = 2200;
    private const long PickupRemoveDelayMs = 2600;

    private VisualElement hudRoot;
    private Label healthLabel;
    private VisualElement healthProgressFill;
    private VisualElement hungerFill;
    private VisualElement thirstFill;
    private Label hungerLabel;
    private Label thirstLabel;
    private SurvivalStats survivalStats;
    private VisualElement interactPromptRow;
    private Label interactPrompt;
    private StructureBarOverlay structureBars;
    private Label clockLabel;
    private float nextClockRefresh;
    private VisualElement reticle;
    private IVisualElementScheduledItem reticleReset;
    private Transform playerTransform;
    private VisualElement structInfoRow;
    private Label structInfoTitle;
    private Label structInfoHint;
    private VisualElement structBarFill;
    private VisualElement buildNoticeRow;
    private Label buildNotice;
    private IVisualElementScheduledItem buildNoticeHide;
    private VisualElement inventoryPanel;
    private VisualElement inventoryGrid;
    private bool inventoryOpen;

    private VisualElement craftingPanel;
    private VisualElement craftingGrid;
    private bool craftingOpen;

    private VisualElement equipmentPanel;
    private VisualElement equipmentGrid;
    private VisualElement equipmentSlot;
    private Label equipmentSlotName;
    private Label equipmentSlotDetail;
    private bool equipmentOpen;
    private InteractionFocus interactionFocus;
    private HoldRing holdRing;
    private VisualElement deathBanner;
    private Label deathHint;
    private PlayerRespawn playerRespawn;
    private VisualElement storagePanel;
    private VisualElement storageGrid;
    private VisualElement storageInventoryGrid;
    private Label storageContentsTitle;
    private Label storageTitle;
    private StorageContainer openStorage;
    // 열려 있는 보관함이 파괴돼도(Unity 객체는 null로 비교된다) 닫는 처리를 놓치지 않도록 열림 상태를 따로 든다.
    private bool storageIsOpen;
    private Equipment armorEquipment;
    private VisualElement armorSlots;
    private Label armorSummary;

    private VisualElement buildPanel;
    private VisualElement buildTabsContainer;
    private VisualElement buildGrid;
    private BuildableCategory currentBuildCategory;

    private VisualElement tooltipPanel;
    private Label tooltipTitle;
    private Label tooltipBody;

    // 이름, 코너 브래킷 표시 여부, 경고색(마젠타) 코너 사용 여부.
    private static readonly (string name, bool showCorners, bool warmCorner)[] HologramPanels =
    {
        ("compass-bar", false, false),
        ("inventory-panel", true, false),
        ("crafting-panel", true, false),
        ("equipment-panel", true, false),
        ("storage-panel", true, false),
        ("build-panel", true, false),
        ("pause-window", true, false),
        ("control-hints", false, false),
    };

    private Texture2D scanlineTexture;
    private Texture2D noiseTexture;
    private VisualElement[] hologramPanels = System.Array.Empty<VisualElement>();
    private VisualElement[] noiseOverlays = System.Array.Empty<VisualElement>();
    private VisualElement[] scanlineOverlays = System.Array.Empty<VisualElement>();
    private float currentHealthRatio = 1f;

    private VisualElement compassStrip;
    private VisualElement compassViewport;
    private readonly Dictionary<CompassMarker, VisualElement> compassMarkers = new Dictionary<CompassMarker, VisualElement>();
    private readonly List<CompassMarker> staleMarkers = new List<CompassMarker>();
    private const float CompassViewportWidth = 320f;
    private const float CompassStartDegrees = -180f;
    private const float CompassTotalDegrees = 720f;

    private void Awake()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        SetupUiScale();
        hudRoot = root.Q<VisualElement>("hud-root");
        healthLabel = root.Q<Label>("health-label");
        healthProgressFill = root.Q<VisualElement>("health-progress-fill");
        hungerFill = root.Q<VisualElement>("hunger-fill");
        thirstFill = root.Q<VisualElement>("thirst-fill");
        hungerLabel = root.Q<Label>("hunger-label");
        thirstLabel = root.Q<Label>("thirst-label");
        interactPromptRow = root.Q<VisualElement>("interact-prompt-row");
        interactPrompt = root.Q<Label>("interact-prompt");
        reticle = root.Q<VisualElement>("reticle");
        clockLabel = root.Q<Label>("clock-label");
        structInfoRow = root.Q<VisualElement>("struct-info-row");
        structInfoTitle = root.Q<Label>("struct-info-title");
        structInfoHint = root.Q<Label>("struct-info-hint");
        structBarFill = root.Q<VisualElement>("struct-bar-fill");
        buildNoticeRow = root.Q<VisualElement>("build-notice-row");
        buildNotice = root.Q<Label>("build-notice");
        inventoryPanel = root.Q<VisualElement>("inventory-panel");
        inventoryGrid = root.Q<VisualElement>("inventory-grid");
        craftingPanel = root.Q<VisualElement>("crafting-panel");
        craftingGrid = root.Q<VisualElement>("crafting-grid");
        equipmentPanel = root.Q<VisualElement>("equipment-panel");
        equipmentGrid = root.Q<VisualElement>("equipment-grid");
        equipmentSlot = root.Q<VisualElement>("equipment-slot");
        equipmentSlotName = root.Q<Label>("equipment-slot-name");
        equipmentSlotDetail = root.Q<Label>("equipment-slot-detail");
        if (equipmentSlot != null) equipmentSlot.AddManipulator(new Clickable(OnEquipmentSlotClicked));
        deathBanner = root.Q<VisualElement>("death-banner");
        deathHint = root.Q<Label>("death-hint");
        SaveLoadDebugController.Notified += OnSaveLoadNotified;
        StorageContainer.Expired += OnStorageExpired;
        storagePanel = root.Q<VisualElement>("storage-panel");
        storageGrid = root.Q<VisualElement>("storage-grid");
        storageInventoryGrid = root.Q<VisualElement>("storage-inventory-grid");
        storageContentsTitle = root.Q<Label>("storage-contents-title");
        storageTitle = root.Q<Label>("storage-title");
        Button takeAll = root.Q<Button>("storage-take-all");
        if (takeAll != null) takeAll.clicked += TakeAllFromStorage;
        if (interactPromptRow != null)
        {
            holdRing = new HoldRing();
            holdRing.style.display = DisplayStyle.None;
            interactPromptRow.Insert(0, holdRing);
        }
        armorSlots = root.Q<VisualElement>("armor-slots");
        armorSummary = root.Q<Label>("armor-summary");
        buildPanel = root.Q<VisualElement>("build-panel");
        buildTabsContainer = root.Q<VisualElement>("build-tabs");
        buildGrid = root.Q<VisualElement>("build-grid");
        tooltipPanel = root.Q<VisualElement>("hud-tooltip");
        tooltipTitle = root.Q<Label>("tooltip-title");
        tooltipBody = root.Q<Label>("tooltip-body");
        compassStrip = root.Q<VisualElement>("compass-strip");
        compassViewport = root.Q<VisualElement>("compass-viewport");
        ammoReadout = root.Q<VisualElement>("ammo-readout");
        ammoMag = root.Q<Label>("ammo-mag");
        ammoReserve = root.Q<Label>("ammo-reserve");
        pickupFeed = root.Q<VisualElement>("pickup-feed");

        if (tooltipPanel != null) tooltipPanel.style.display = DisplayStyle.None;

        RefreshControlHints(root);
        GameSettingsStore.Changed += OnSettingsChangedForHints;
        GameSettingsStore.Changed += ApplyHudVisibility;
        ApplyHudVisibility();
        SetupItemMenu(root);
        pauseMenu = new PauseMenu(root, () => playerTransform != null ? playerTransform.gameObject : null);
        pauseMenu.OpenChanged += _ => UpdatePointerLock();

        SetInventoryOpen(false);
        SetCraftingOpen(false);
        SetEquipmentOpen(false);
        if (buildPanel != null) buildPanel.style.display = DisplayStyle.None;
        if (storagePanel != null) storagePanel.style.display = DisplayStyle.None;
        SetupHologramPanels(root);
        BuildCompassStrip();
        RefreshCraftingGrid();
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        playerTransform = player.transform;
        structureBars = new StructureBarOverlay(hudRoot != null ? hudRoot : GetComponent<UIDocument>().rootVisualElement);
        playerHealth = player.GetComponent<Health>();
        playerInventory = player.GetComponent<Inventory>();
        craftingSystem = player.GetComponent<CraftingSystem>();
        buildingSystem = player.GetComponent<BuildingSystem>();
        playerInputs = player.GetComponent<StarterAssets.StarterAssetsInputs>();
        itemDropper = player.GetComponent<ItemDropper>();
        weaponEquipment = player.GetComponent<WeaponEquipment>();
        armorEquipment = player.GetComponent<Equipment>();
        if (armorEquipment != null) armorEquipment.Changed += RefreshEquipment;
        survivalStats = player.GetComponent<SurvivalStats>();
        if (survivalStats != null)
        {
            survivalStats.Changed += RefreshSurvival;
            RefreshSurvival();
        }
        combatAudio = player.GetComponent<CombatAudio>();
        interactionFocus = player.GetComponent<InteractionFocus>();
        if (interactionFocus != null)
        {
            interactionFocus.PromptChanged += ShowInteractPrompt;
            interactionFocus.StorageRequested += OpenStorage;
        }
        if (weaponEquipment != null)
        {
            weaponEquipment.StatusChanged += RefreshWeapon;
            weaponEquipment.Changed += _ => RefreshEquipment();
            RefreshWeapon();
            RefreshEquipment();
        }

        HitscanWeapon hitscan = player.GetComponent<HitscanWeapon>();
        if (hitscan != null) hitscan.HitConfirmed += ShowHitMarker;

        StructureInspector inspector = player.GetComponent<StructureInspector>();
        if (inspector != null)
        {
            inspector.AimChanged += ShowStructureInfo;
            inspector.RepairFailed += () => ShowNotice("수리할 재료가 부족합니다");
        }

        if (buildingSystem != null)
        {
            buildingSystem.BuildMenuChanged += OnBuildMenuChanged;
            buildingSystem.BuildableSelected += (_) => RefreshBuildGrid();
            buildingSystem.PlacementRejected += ShowBuildNotice;
        }

        if (playerHealth != null)
        {
            playerHealth.Damaged += (_, __) => RefreshHealth();
            playerHealth.Died += RefreshHealth;
            playerHealth.Revived += RefreshHealth;
            playerRespawn = player.GetComponent<PlayerRespawn>();
            if (playerRespawn != null) playerRespawn.BedChanged += OnBedChanged;
            LanternEquipment lantern = player.GetComponent<LanternEquipment>();
            if (lantern != null)
            {
                lantern.Toggled += on => ShowNotice(on ? "랜턴을 켰습니다" : "랜턴을 껐습니다", on);
                lantern.NoLantern += () => ShowNotice("랜턴이 없습니다 (제작 메뉴에서 만들 수 있습니다)");
            }
            RefreshHealth();
        }

        if (playerInventory != null)
        {
            // 보관함으로 옮겨 줄어드는 경우도 있어서 추가가 아니라 변경 알림에 맞춰 갱신한다.
            playerInventory.Modified += () => { RefreshInventory(); RefreshEquipment(); };
            playerInventory.ItemPickedUp += ShowPickup;
            RefreshInventory();
        }

        StartCoroutine(NoiseRoutine());
        StartCoroutine(FlickerRoutine());
        StartCoroutine(GlitchRoutine());
    }

    private void OnDestroy()
    {
        if (pauseMenu != null) pauseMenu.Dispose();
        GameSettingsStore.Changed -= ApplyUiScale;
        GameSettingsStore.Changed -= ApplyHudVisibility;
        GameSettingsStore.Changed -= OnSettingsChangedForHints;
        PanelSettings restoring = GetComponent<UIDocument>() != null ? GetComponent<UIDocument>().panelSettings : null;
        if (restoring != null && baseReferenceResolution != Vector2Int.zero) restoring.referenceResolution = baseReferenceResolution;
        if (noiseTexture != null) Destroy(noiseTexture);
        if (scanlineTexture != null) Destroy(scanlineTexture);
        SaveLoadDebugController.Notified -= OnSaveLoadNotified;
        StorageContainer.Expired -= OnStorageExpired;
        AnyPanelOpen = false;
    }

    private void OnSettingsChangedForHints()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        if (root != null) RefreshControlHints(root);
    }

    // 화면 아래 조작 안내를 지금 키에 맞춰 다시 만든다(옵션에서 키를 바꾸면 따라간다).
    private static void RefreshControlHints(VisualElement root)
    {
        VisualElement container = root.Q<VisualElement>("control-hints");
        if (container == null) return;

        string[] hints =
        {
            $"{KeyBindings.Label(GameKey.Sprint)} 달리기",
            "Esc 닫기 · 취소 · 메뉴",
            $"{KeyBindings.Label(GameKey.Inventory)} 인벤토리",
            $"{KeyBindings.Label(GameKey.Craft)} 제작",
            $"{KeyBindings.Label(GameKey.Equipment)} 장비",
            $"{KeyBindings.Label(GameKey.Holster)} 무기",
            $"{KeyBindings.Label(GameKey.BuildMode)} 건축",
            $"{KeyBindings.Label(GameKey.Lantern)} 랜턴",
            $"{KeyBindings.Label(GameKey.Interact)} 길게 상호작용",
            "F5 저장 · F9 불러오기",
            $"{KeyBindings.Label(GameKey.RotateLeft)} / {KeyBindings.Label(GameKey.RotateRight)} 회전 · {KeyBindings.Label(GameKey.LevelUp)} / {KeyBindings.Label(GameKey.LevelDown)} 층 · {KeyBindings.Label(GameKey.Demolish)} 철거",
        };

        // 홀로그램 장식(스캔라인, 모서리)은 남기고 안내 문구만 지운다.
        foreach (Label old in container.Query<Label>(className: "control-hint").ToList()) old.RemoveFromHierarchy();
        for (int i = 0; i < hints.Length; i++)
        {
            var label = new Label(hints[i]);
            label.AddToClassList("control-hint");
            if (i == hints.Length - 1) label.AddToClassList("control-hint-last");
            container.Add(label);
        }
    }

    // 옵션의 UI 크기를 패널 기준 해상도에 반영한다(배율이 클수록 기준 해상도를 줄여 UI를 키운다). 패널 설정 에셋의 원래 값은 파괴할 때 되돌린다.
    private void SetupUiScale()
    {
        PanelSettings panelSettings = GetComponent<UIDocument>().panelSettings;
        if (panelSettings == null) return;

        panelSettings = GetComponent<UIDocument>().panelSettings;
        baseReferenceResolution = panelSettings.referenceResolution;
        ApplyUiScale();
        GameSettingsStore.Changed += ApplyUiScale;
    }

    // 옵션의 HUD 탭에서 숨기기로 한 요소를 감춘다(요소마다 hud-root의 "hide-이름" 클래스로 켜고 끈다. 열어 쓰는 창과 메뉴는 그대로).
    private static readonly (HudElement element, string cssClass)[] HudHideClasses =
    {
        (HudElement.Compass, "hide-compass"), (HudElement.Clock, "hide-clock"), (HudElement.Survival, "hide-survival"),
        (HudElement.Reticle, "hide-reticle"), (HudElement.Ammo, "hide-ammo"), (HudElement.InteractPrompt, "hide-prompt"),
        (HudElement.PickupFeed, "hide-pickup"), (HudElement.Notices, "hide-notices"), (HudElement.ControlHints, "hide-hints"),
    };

    private void ApplyHudVisibility()
    {
        if (hudRoot == null) return;
        GameSettings settings = GameSettingsStore.Current;
        foreach ((HudElement element, string cssClass) in HudHideClasses)
            hudRoot.EnableInClassList(cssClass, settings.IsHudHidden(element));
    }

    private void ApplyUiScale()
    {
        PanelSettings panelSettings = GetComponent<UIDocument>().panelSettings;
        if (panelSettings == null || baseReferenceResolution == Vector2Int.zero) return;

        float scale = Mathf.Max(0.1f, GameSettingsStore.Current.uiScale);
        panelSettings.referenceResolution = new Vector2Int(Mathf.RoundToInt(baseReferenceResolution.x / scale), Mathf.RoundToInt(baseReferenceResolution.y / scale));
    }

    private void OnSaveLoadNotified(string message, bool success) => ShowNotice(message, success);

    private void OnStorageExpired(string displayName) => ShowNotice($"{displayName}이(가) 시간이 지나 사라졌습니다");

    // 홀로그램 오버레이(스캔라인/노이즈)와 코너 브래킷을 각 UI 패널 "안쪽"에만 붙인다.
    // hud-root 전체에 붙이면 3D 게임 화면까지 덮어버리므로 패널 단위로만 적용한다.
    private void SetupHologramPanels(VisualElement root)
    {
        scanlineTexture = CreateScanlineTexture(240, 3);

        noiseTexture = new Texture2D(96, 96, TextureFormat.RGBA32, false);
        noiseTexture.wrapMode = TextureWrapMode.Repeat;
        noiseTexture.filterMode = FilterMode.Point;
        RandomizeNoiseTexture();

        List<VisualElement> panels = new List<VisualElement>();
        List<VisualElement> noises = new List<VisualElement>();
        List<VisualElement> scanlines = new List<VisualElement>();
        foreach ((string name, bool showCorners, bool warmCorner) entry in HologramPanels)
        {
            VisualElement panel = root.Q<VisualElement>(entry.name);
            if (panel == null) continue;
            panels.Add(panel);
            (VisualElement scanline, VisualElement noise) overlay = DecoratePanel(panel, entry.showCorners, entry.warmCorner);
            scanlines.Add(overlay.scanline);
            noises.Add(overlay.noise);
        }

        VisualElement survivorCard = root.Q<VisualElement>(className: "survivor-card");
        if (survivorCard != null)
        {
            panels.Add(survivorCard);
            (VisualElement scanline, VisualElement noise) overlay = DecoratePanel(survivorCard, true, false);
            scanlines.Add(overlay.scanline);
            noises.Add(overlay.noise);
        }

        hologramPanels = panels.ToArray();
        noiseOverlays = noises.ToArray();
        scanlineOverlays = scanlines.ToArray();
    }

    // 체력이 정상(noiseHealthThreshold 이상)일 때는 스캔라인/노이즈가 전혀 보이지 않아야
    // 화면이 깔끔하므로, 두 오버레이 모두 초기 불투명도를 0에서 시작하고
    // RefreshHealth가 실제 체력 비율에 맞춰 갱신한다.
    private (VisualElement scanline, VisualElement noise) DecoratePanel(VisualElement panel, bool showCorners, bool warmCorner)
    {
        VisualElement scanline = new VisualElement();
        scanline.pickingMode = PickingMode.Ignore;
        scanline.AddToClassList("hud-panel-overlay");
        scanline.style.backgroundImage = new StyleBackground(scanlineTexture);
        scanline.style.unityBackgroundImageTintColor = new Color(0.5f, 0.95f, 1f, 1f);
        scanline.style.opacity = 0f;
        panel.Add(scanline);

        VisualElement noise = new VisualElement();
        noise.pickingMode = PickingMode.Ignore;
        noise.AddToClassList("hud-panel-overlay");
        noise.style.backgroundImage = new StyleBackground(noiseTexture);
        noise.style.unityBackgroundImageTintColor = new Color(0.8f, 0.98f, 1f, 1f);
        noise.style.opacity = 0f;
        panel.Add(noise);

        if (!showCorners) return (scanline, noise);

        AddCorner(panel, "hud-corner-tl", warmCorner);
        AddCorner(panel, "hud-corner-tr", warmCorner);
        AddCorner(panel, "hud-corner-bl", warmCorner);
        AddCorner(panel, "hud-corner-br", warmCorner);
        return (scanline, noise);
    }

    private static void AddCorner(VisualElement panel, string cornerClass, bool warm)
    {
        VisualElement corner = new VisualElement();
        corner.pickingMode = PickingMode.Ignore;
        corner.AddToClassList("hud-corner");
        corner.AddToClassList(cornerClass);
        if (warm) corner.AddToClassList("hud-corner-warm");
        panel.Add(corner);
    }

    // 360도(+양쪽 여유 360도씩)를 커버하는 눈금 띠를 한 번만 만들어 두고,
    // 매 프레임 카메라 시점에 맞춰 좌우로 밀어서 실제로 도는 나침반처럼 보이게 한다.
    private void BuildCompassStrip()
    {
        if (compassStrip == null) return;

        string[] labels = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
        compassStrip.style.width = CompassTotalDegrees * compassPixelsPerDegree;

        for (int deg = 0; deg <= (int)CompassTotalDegrees; deg += 15)
        {
            float worldAngle = CompassStartDegrees + deg;
            float normalized = ((worldAngle % 360f) + 360f) % 360f;
            bool isCardinal = Mathf.Abs(normalized % 90f) < 0.01f;
            bool isLabel = Mathf.Abs(normalized % 45f) < 0.01f;

            VisualElement tick = new VisualElement();
            tick.AddToClassList("compass-tick");
            if (isCardinal) tick.AddToClassList("compass-tick-major");
            tick.style.left = deg * compassPixelsPerDegree;
            compassStrip.Add(tick);

            if (isLabel)
            {
                int labelIndex = Mathf.RoundToInt(normalized / 45f) % 8;
                string text = labels[labelIndex];
                Label label = new Label(text);
                label.AddToClassList("compass-label");
                label.AddToClassList(text == "N" ? "compass-label-north" : "compass-label-muted");
                label.style.left = deg * compassPixelsPerDegree - (text.Length * 3.5f);
                compassStrip.Add(label);
            }
        }
    }

    private void UpdateCompass()
    {
        if (compassStrip == null) return;
        Camera cam = Camera.main;
        if (cam == null) return;

        float yaw = cam.transform.eulerAngles.y;
        float tickPosition = (yaw - CompassStartDegrees) * compassPixelsPerDegree;
        compassStrip.style.left = 160f - tickPosition;
        UpdateCompassMarkers(cam, yaw);
    }

    // 활성 마커마다 요소를 두고 카메라 방향 대비 방위각으로 위치를 정한다. 시야 밖이면 양끝에 붙여 방향을 알린다.
    private void UpdateCompassMarkers(Camera cam, float cameraYaw)
    {
        if (compassViewport == null) return;

        foreach (CompassMarker marker in CompassMarker.Active)
        {
            if (marker == null) continue;
            if (!compassMarkers.TryGetValue(marker, out VisualElement element))
            {
                element = CreateCompassMarkerElement(marker);
                compassViewport.Add(element);
                compassMarkers[marker] = element;
            }

            Vector3 origin = playerTransform != null ? playerTransform.position : cam.transform.position;
            Vector3 offset = marker.transform.position - origin;
            float distance = new Vector2(offset.x, offset.z).magnitude;
            float bearing = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
            float relative = Mathf.DeltaAngle(cameraYaw, bearing);
            float x = CompassViewportWidth * 0.5f + relative * compassPixelsPerDegree;

            Label text = element.Q<Label>(className: "compass-marker-text");
            string meters = distance >= 1000f ? $"{distance / 1000f:0.0}km" : $"{Mathf.RoundToInt(distance)}m";
            if (x < 6f)
            {
                element.style.right = StyleKeyword.Auto;
                element.style.left = 2f;
                text.text = "< " + meters;
            }
            else if (x > CompassViewportWidth - 6f)
            {
                element.style.left = StyleKeyword.Auto;
                element.style.right = 2f;
                text.text = meters + " >";
            }
            else
            {
                element.style.right = StyleKeyword.Auto;
                element.style.left = x - 4f;
                text.text = meters;
            }

            bool urgent = marker.IsUrgent;
            element.EnableInClassList("compass-marker-urgent", urgent);
            // 마름모 색은 인라인으로 정하므로 임박 색도 여기서 같이 정한다.
            element.Q<VisualElement>(className: "compass-marker-diamond").style.backgroundColor = urgent ? new Color(1f, 0.31f, 0.27f) : marker.Color;
            // 임박하면 깜빡인다(현실 시간 기준 0.5초 주기).
            element.style.opacity = urgent ? (Mathf.Repeat(Time.unscaledTime, 1f) < 0.5f ? 1f : 0.25f) : 1f;
        }

        // 사라진 마커의 요소는 지운다.
        staleMarkers.Clear();
        foreach (KeyValuePair<CompassMarker, VisualElement> pair in compassMarkers)
            if (pair.Key == null || !pair.Key.isActiveAndEnabled) staleMarkers.Add(pair.Key);
        foreach (CompassMarker stale in staleMarkers)
        {
            if (compassMarkers.TryGetValue(stale, out VisualElement element)) element.RemoveFromHierarchy();
            compassMarkers.Remove(stale);
        }
    }

    private static VisualElement CreateCompassMarkerElement(CompassMarker marker)
    {
        VisualElement element = new VisualElement { pickingMode = PickingMode.Ignore };
        element.AddToClassList("compass-marker");

        VisualElement diamond = new VisualElement { pickingMode = PickingMode.Ignore };
        diamond.AddToClassList("compass-marker-diamond");
        element.Add(diamond);

        Label text = new Label { pickingMode = PickingMode.Ignore };
        text.AddToClassList("compass-marker-text");
        element.Add(text);
        return element;
    }

    private static Texture2D CreateScanlineTexture(int height, int lineSpacing)
    {
        Texture2D texture = new Texture2D(1, height, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Point;
        Color32[] pixels = new Color32[height];
        for (int y = 0; y < height; y++)
        {
            byte alpha = (y % lineSpacing) == 0 ? (byte)255 : (byte)0;
            pixels[y] = new Color32(255, 255, 255, alpha);
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    private void RandomizeNoiseTexture()
    {
        if (noiseTexture == null) return;
        int count = noiseTexture.width * noiseTexture.height;
        Color32[] pixels = new Color32[count];
        for (int i = 0; i < count; i++)
        {
            byte alpha = (byte)Random.Range(0, 256);
            pixels[i] = new Color32(255, 255, 255, alpha);
        }
        noiseTexture.SetPixels32(pixels);
        noiseTexture.Apply();
    }

    private IEnumerator NoiseRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(0.07f);
        while (true)
        {
            RandomizeNoiseTexture();
            yield return wait;
        }
    }

    // 체력이 정상(noiseHealthThreshold 이상)일 때는 화면이 깔끔해야 하므로 깜빡임을 아예 트리거하지 않는다.
    private IEnumerator FlickerRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(2.5f, 6f));
            if (hudRoot == null || currentHealthRatio >= noiseHealthThreshold) continue;

            hudRoot.style.opacity = Random.Range(0.72f, 0.9f);
            yield return new WaitForSeconds(Random.Range(0.03f, 0.08f));
            hudRoot.style.opacity = 1f;

            if (Random.value < 0.35f)
            {
                yield return new WaitForSeconds(Random.Range(0.04f, 0.1f));
                hudRoot.style.opacity = Random.Range(0.8f, 0.95f);
                yield return new WaitForSeconds(0.05f);
                hudRoot.style.opacity = 1f;
            }
        }
    }

    // 화면 전체가 아니라 무작위로 고른 패널 하나만 짧게 신호 끊김처럼 깜빡인다.
    // 마찬가지로 체력이 정상일 때는 트리거하지 않는다.
    private IEnumerator GlitchRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(3f, 7f));
            if (hologramPanels.Length == 0 || currentHealthRatio >= noiseHealthThreshold) continue;

            VisualElement panel = hologramPanels[Random.Range(0, hologramPanels.Length)];
            panel.style.opacity = 0.45f;
            yield return new WaitForSeconds(Random.Range(0.03f, 0.06f));
            panel.style.opacity = 1f;
        }
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // 테스트용: -mp-ui-test면 보관함 패널을 실제 HUD 이벤트로 조작한다(패널 열기 → 인벤토리 칸 클릭으로 넣기 → 보관함 칸 클릭으로 꺼내기 → 두 묶음 넣고 "모두 가져오기" 버튼 → 닫기). 단계마다 화면 상태를 [mp-ui] 로그로 남긴다.
    private static readonly bool uiTest = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-mp-ui-test") >= 0;
    private int uiTestStep;
    private float nextUiTest;

    private void RunUiTest()
    {
        if (!uiTest || Time.time < nextUiTest || playerInventory == null || storagePanel == null || storageGrid == null) return;
        nextUiTest = Time.time + 2.5f;

        switch (uiTestStep)
        {
            case 0:
                StorageContainer chest = FindUiTestChest();
                if (chest == null || playerInventory.Stacks.Count < 2) return;
                OpenStorage(chest);
                LogUi("패널 열기");
                break;
            case 1:
                ClickCell(storageInventoryGrid, 0);
                break;
            case 2:
                LogUi("인벤토리 첫 칸 클릭(넣기) 뒤");
                ClickCell(storageGrid, 0);
                break;
            case 3:
                LogUi("보관함 첫 칸 클릭(꺼내기) 뒤");
                // 같은 칸을 연달아 두 번: 두 번째는 요청 중이라 무시돼야 한다.
                ClickCell(storageInventoryGrid, 0);
                ClickCell(storageInventoryGrid, 0);
                LogUi("같은 칸 연속 클릭 직후(응답 전)");
                break;
            case 4:
                LogUi("같은 칸 연속 클릭 뒤");
                ClickTakeAll();
                ClickTakeAll();
                break;
            case 5:
                LogUi("모두 가져오기 버튼(연속 두 번) 뒤");
                // 서로 다른 칸을 연달아: 둘 다 요청돼야 한다.
                ClickCell(storageInventoryGrid, 0);
                ClickCell(storageInventoryGrid, 1);
                break;
            case 6:
                LogUi("서로 다른 두 칸 연속 클릭 뒤");
                CloseStorage();
                break;
            case 7:
                LogUi("닫은 뒤");
                break;
            default:
                return;
        }
        uiTestStep++;
    }

    private static StorageContainer FindUiTestChest()
    {
        foreach (StorageContainer storage in StorageContainer.Active)
        {
            BuildableInstance instance = storage.GetComponent<BuildableInstance>();
            if (instance != null && instance.BuildableId != BuildableInstance.CorpseBagId && !string.IsNullOrEmpty(instance.WorldId)) return storage;
        }
        return null;
    }

    // 격자의 index번째 칸에 ClickEvent를 보낸다(실제 마우스 클릭이 등록된 콜백을 부르는 것과 같다).
    private static void ClickCell(VisualElement grid, int index)
    {
        if (grid == null || index >= grid.childCount) { Debug.Log($"[mp-ui] 클릭할 칸이 없다: index={index}, 칸 수={(grid != null ? grid.childCount : -1)}"); return; }
        VisualElement cell = grid.ElementAt(index);
        using (ClickEvent click = ClickEvent.GetPooled())
        {
            click.target = cell;
            cell.SendEvent(click);
        }
    }

    private void ClickTakeAll()
    {
        Button takeAll = storagePanel.Q<Button>("storage-take-all");
        if (takeAll == null) { Debug.Log("[mp-ui] 모두 가져오기 버튼이 없다"); return; }
        using (NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled())
        {
            submit.target = takeAll;
            takeAll.SendEvent(submit);
        }
    }

    private void LogUi(string label)
    {
        string chestContents = openStorage != null ? NetworkInventory.EncodeSnapshot(openStorage.Contents) : "";
        Debug.Log($"[mp-ui] {label}: 패널={storagePanel.resolvedStyle.display} 보관함칸={storageGrid.childCount} 인벤토리칸={storageInventoryGrid.childCount} 제목='{storageContentsTitle?.text}' 보관함=[{chestContents}] 인벤토리=[{NetworkInventory.EncodeSnapshot(playerInventory)}]");
    }
#endif

    private void Update()
    {
        UpdateCompass();
        if (structureBars != null && playerTransform != null) structureBars.Tick(Camera.main, playerTransform.position);
        UpdateClock();
        UpdateAmmoAttention();
        UpdateHoldRing();
        UpdateStorageGuard();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        RunUiTest();
#endif
        UpdateDeathHint();

        if (Keyboard.current == null) return;

        // 일시정지 메뉴가 열려 있으면 Esc만 받는다(다른 패널 단축키는 막는다).
        if (pauseMenu != null && pauseMenu.IsOpen)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame) pauseMenu.HandleEscape();
            return;
        }

        if (KeyBindings.Pressed(GameKey.Inventory))
        {
            SetInventoryOpen(!inventoryOpen);
        }

        if (KeyBindings.Pressed(GameKey.Craft))
        {
            SetCraftingOpen(!craftingOpen);
        }

        if (KeyBindings.Pressed(GameKey.Equipment))
        {
            SetEquipmentOpen(!equipmentOpen);
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame) HandleEscape();
    }

    // Esc는 취소/닫기/해제 버튼이다. 가장 위에 있는 것 하나만 처리한다: 열린 패널 닫기 → 건축 메뉴/건축 모드 취소 → 사다리 놓기 → 든 무기 집어넣기.
    private void HandleEscape()
    {
        if (menuItem != null)
        {
            HideItemMenu();
            return;
        }
        if (storageIsOpen)
        {
            CloseStorage();
            return;
        }
        if (inventoryOpen || craftingOpen || equipmentOpen)
        {
            if (inventoryOpen) SetInventoryOpen(false);
            if (craftingOpen) SetCraftingOpen(false);
            if (equipmentOpen) SetEquipmentOpen(false);
            return;
        }
        if (buildingSystem != null && buildingSystem.BuildMenuOpen)
        {
            buildingSystem.SetBuildMenuOpen(false);
            return;
        }
        if (buildingSystem != null && buildingSystem.BuildModeActive)
        {
            buildingSystem.ExitBuildMode();
            return;
        }
        if (playerTransform == null) return;

        LadderClimber climber = playerTransform.GetComponent<LadderClimber>();
        if (climber != null && climber.ReleaseHere()) return;

        WeaponEquipment weapons = playerTransform.GetComponent<WeaponEquipment>();
        if (weapons != null && weapons.Current != null && !weapons.IsReloading)
        {
            weapons.Unequip();
            return;
        }

        // 더 닫거나 취소할 것이 없으면 일시정지 메뉴를 연다.
        if (pauseMenu != null) pauseMenu.Open();
    }

    // 허기/갈증 바를 갱신한다. 낮으면(설정의 경고 비율 아래) 붉은 색으로 바꾼다.
    // 상호작용 안내 문구를 보이거나 숨긴다(빈 문자열이면 숨김).
    private void ShowInteractPrompt(string text)
    {
        if (interactPromptRow == null || interactPrompt == null) return;
        interactPrompt.text = text;
        interactPromptRow.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
    }

    private void RefreshSurvival()
    {
        if (survivalStats == null) return;
        ApplyStatBar(hungerFill, hungerLabel, survivalStats.Hunger);
        ApplyStatBar(thirstFill, thirstLabel, survivalStats.Thirst);
    }

    private void ApplyStatBar(VisualElement fill, Label label, SurvivalMeter meter)
    {
        if (fill == null || label == null) return;
        fill.style.width = Length.Percent(meter.Ratio * 100f);
        label.text = Mathf.CeilToInt(meter.Current).ToString();
        fill.EnableInClassList("stat-fill-low", meter.Ratio <= survivalStats.Config.LowThreshold);
    }

    private void RefreshHealth()
    {
        if (playerHealth == null || healthLabel == null) return;
        healthLabel.text = $"{Mathf.CeilToInt(playerHealth.CurrentHealth)} / {Mathf.CeilToInt(playerHealth.MaxHealth)}";
        if (deathBanner != null) deathBanner.style.display = playerHealth.IsDead ? DisplayStyle.Flex : DisplayStyle.None;

        float healthRatio = playerHealth.MaxHealth > 0f
            ? Mathf.Clamp01(playerHealth.CurrentHealth / playerHealth.MaxHealth)
            : 0f;

        if (healthProgressFill != null)
        {
            healthProgressFill.style.width = Length.Percent(healthRatio * 100f);
        }

        currentHealthRatio = healthRatio;
        UpdateHologramIntensity(healthRatio);
    }

    // 체력이 noiseHealthThreshold(기본 40%) 이상이면 스캔라인/노이즈가 전혀 없는 깔끔한 화면을 유지하고,
    // 그 아래로 떨어졌을 때부터 나타나기 시작해 체력이 낮아질수록(0에 가까워질수록) 점점 심해진다.
    // 플리커/글리치 코루틴(FlickerRoutine, GlitchRoutine)도 currentHealthRatio를 직접 참조해
    // 같은 기준으로 억제된다.
    private void UpdateHologramIntensity(float healthRatio)
    {
        float t = healthRatio >= noiseHealthThreshold
            ? 0f
            : Mathf.InverseLerp(noiseHealthThreshold, 0f, healthRatio);

        foreach (VisualElement noise in noiseOverlays)
        {
            noise.style.opacity = t * maxNoiseOpacity;
        }

        foreach (VisualElement scanline in scanlineOverlays)
        {
            scanline.style.opacity = t * maxScanlineOpacity;
        }
    }

    // ---- 공용 그리드 셀 헬퍼 (인벤토리/제작/건축이 모두 공유) ----

    private VisualElement CreateGridCell()
    {
        VisualElement cell = new VisualElement();
        cell.AddToClassList("grid-cell");

        VisualElement icon = new VisualElement { name = "cell-icon" };
        icon.AddToClassList("grid-cell-icon");
        cell.Add(icon);

        Label qty = new Label { name = "cell-qty" };
        qty.AddToClassList("grid-cell-qty");
        cell.Add(qty);

        return cell;
    }

    // 실제 아이콘 스프라이트가 없으면(현재 대부분의 정의가 그렇다) 이름 이니셜 배지로 대체한다.
    // survivor-emblem 등 기존 HUD 곳곳의 placeholder 배지 관례와 같은 방식이다.
    private static void SetCellIcon(VisualElement iconFrame, Sprite sprite, string displayName)
    {
        iconFrame.Clear();
        if (sprite != null)
        {
            iconFrame.style.backgroundImage = new StyleBackground(sprite);
            return;
        }

        iconFrame.style.backgroundImage = StyleKeyword.Null;
        Label fallback = new Label(InitialsOf(displayName));
        fallback.AddToClassList("grid-cell-fallback");
        iconFrame.Add(fallback);
    }

    private static string InitialsOf(string name)
    {
        if (string.IsNullOrEmpty(name)) return "?";
        return name.Length <= 2 ? name : name.Substring(0, 2);
    }

    private void RegisterTooltip(VisualElement cell, string title, string body)
    {
        cell.RegisterCallback<PointerEnterEvent>(_ => ShowTooltip(title, body));
        cell.RegisterCallback<PointerLeaveEvent>(_ => HideTooltip());
        cell.RegisterCallback<PointerMoveEvent>(evt => PositionTooltip(evt.position));
    }

    private void ShowTooltip(string title, string body)
    {
        if (tooltipPanel == null) return;
        tooltipTitle.text = title;
        tooltipBody.text = body;
        tooltipPanel.style.display = DisplayStyle.Flex;
        tooltipPanel.BringToFront();
    }

    private void HideTooltip()
    {
        if (tooltipPanel == null) return;
        tooltipPanel.style.display = DisplayStyle.None;
    }

    private void PositionTooltip(Vector2 pointerPanelPosition)
    {
        if (tooltipPanel == null) return;
        tooltipPanel.style.left = pointerPanelPosition.x + 18f;
        tooltipPanel.style.top = pointerPanelPosition.y + 18f;
    }

    private static string ItemCategoryLabel(ItemCategory category)
    {
        switch (category)
        {
            case ItemCategory.Resource: return "자원";
            case ItemCategory.Ammo: return "탄약";
            case ItemCategory.Consumable: return "소모품";
            case ItemCategory.Weapon: return "무기";
            case ItemCategory.Equipment: return "장비";
            case ItemCategory.CraftingMaterial: return "제작 재료";
            case ItemCategory.BuildingMaterial: return "건축 자재";
            default: return category.ToString();
        }
    }

    // 패널 3개 중 하나라도 열려있는지 다시 계산하고, 열려있으면 마우스로 조작할 수 있도록
    // 잠겨있던 커서를 풀어준다(StarterAssetsInputs가 기본적으로 커서를 항상 잠가두기 때문에
    // 이 처리가 없으면 어떤 HUD 버튼도 실제로 클릭할 수 없다).
    private void UpdatePointerLock()
    {
        bool anyOpen = inventoryOpen || craftingOpen || equipmentOpen || storageIsOpen || (buildingSystem != null && buildingSystem.BuildMenuOpen) || (pauseMenu != null && pauseMenu.IsOpen);
        AnyPanelOpen = anyOpen;
        UnityEngine.Cursor.lockState = anyOpen ? CursorLockMode.None : CursorLockMode.Locked;
        UnityEngine.Cursor.visible = anyOpen;

        // 항목 위에 포인터가 있는 채로 패널이 닫히거나 바뀌면 PointerLeave가 오지 않아 툴팁이 남는다.
        HideTooltip();

        // 패널이 열린 동안 마우스를 움직여도 카메라가 돌지 않도록 시선 입력을 끊는다.
        if (playerInputs != null)
        {
            playerInputs.cursorInputForLook = !anyOpen;
            if (anyOpen) playerInputs.look = Vector2.zero;
        }
    }

    private void CloseBuildModeIfActive()
    {
        if (buildingSystem != null && buildingSystem.BuildModeActive) buildingSystem.ExitBuildMode();
    }

    // ---- 무기 ----

    // 장착한 총의 탄창/예비 탄약과 재장전 상태를 조준점 옆에 보여준다. 현재 탄창은 크게, 예비 탄약은 작게 나눈다. 맨손이면 숨긴다.
    private void RefreshWeapon()
    {
        if (ammoReadout == null || ammoMag == null || ammoReserve == null || weaponEquipment == null) return;

        WeaponDefinition weapon = weaponEquipment.Current;
        if (weapon == null)
        {
            ammoReadout.style.display = DisplayStyle.None;
            return;
        }

        int magazine = weaponEquipment.MagazineRounds;
        int reserve = weaponEquipment.ReserveRounds;
        bool reloading = weaponEquipment.IsReloading;
        bool noAmmo = magazine <= 0 && reserve <= 0;
        bool showText = reloading || noAmmo;
        // 탄창의 4분의 1 이하(최소 1발)를 부족으로 본다.
        bool low = !showText && magazine > 0 && magazine <= Mathf.Max(1, Mathf.CeilToInt(weapon.MagazineSize * 0.25f));

        ammoReadout.style.display = DisplayStyle.Flex;
        ammoMag.text = reloading ? "재장전 중..." : (noAmmo ? "탄약 없음" : magazine.ToString());
        ammoReserve.text = showText ? string.Empty : "/ " + reserve;
        ammoReserve.style.display = showText ? DisplayStyle.None : DisplayStyle.Flex;

        ammoReadout.EnableInClassList("ammo-text", showText);
        ammoReadout.EnableInClassList("ammo-reloading", reloading);
        ammoReadout.EnableInClassList("ammo-low", low);
        ammoReadout.EnableInClassList("ammo-empty", !reloading && magazine <= 0);
    }

    // 평소에는 옅게 두고, 조준하거나 쏘는 중이거나 재장전/탄약 부족일 때만 또렷하게 보여 준다.
    private void UpdateAmmoAttention()
    {
        if (ammoReadout == null || weaponEquipment == null || weaponEquipment.Current == null) return;

        bool attention = weaponEquipment.IsAiming || weaponEquipment.IsReloading || weaponEquipment.MagazineRounds <= 0
            || ammoReadout.ClassListContains("ammo-low");
        if (attention == ammoAttention) return;

        ammoAttention = attention;
        ammoReadout.EnableInClassList("ammo-active", attention);
    }

    private static void SetLabel(Label label, string text)
    {
        if (label != null) label.text = text;
    }

    // ---- 게임 시각 ----

    // 0.5초마다 시각 문자열을 갱신하고 밤이면 붉게 표시한다. 시계가 없으면 숨긴다.
    private void UpdateClock()
    {
        if (clockLabel == null || Time.time < nextClockRefresh) return;
        nextClockRefresh = Time.time + 0.5f;

        GameClock clock = GameClock.Current;
        clockLabel.style.display = clock != null ? DisplayStyle.Flex : DisplayStyle.None;
        if (clock == null) return;

        clockLabel.text = (clock.IsNight ? "밤 " : "낮 ") + GameClock.Format(clock.Hour);
        clockLabel.EnableInClassList("clock-label-night", clock.IsNight);
    }

    // ---- 명중 표시 ----

    private const long HitMarkerMs = 160;

    // 총알이 살아 있는 대상에 맞으면 조준점이 잠깐 붉게 커진다(머리는 더 강하게, 처치하면 더 오래).
    private void ShowHitMarker(bool headshot, bool killed)
    {
        if (reticle == null) return;

        reticle.RemoveFromClassList("reticle-hit");
        reticle.RemoveFromClassList("reticle-head");
        reticle.RemoveFromClassList("reticle-kill");
        reticle.AddToClassList(headshot ? "reticle-head" : "reticle-hit");
        if (killed) reticle.AddToClassList("reticle-kill");

        reticleReset?.Pause();
        reticleReset = reticle.schedule.Execute(() =>
        {
            reticle.RemoveFromClassList("reticle-hit");
            reticle.RemoveFromClassList("reticle-head");
            reticle.RemoveFromClassList("reticle-kill");
        }).StartingIn(killed ? HitMarkerMs * 2 : HitMarkerMs);
    }

    // ---- 건축 설치 거부 안내 ----

    private const long BuildNoticeMs = 1800;

    // 조준한 건축물의 이름, 체력 막대, 수리 안내를 보여 준다(대상이 없으면 숨긴다).
    private void ShowStructureInfo(BuildableInstance instance, BuildableHealth health)
    {
        if (structInfoRow == null) return;
        if (instance == null || health == null)
        {
            structInfoRow.style.display = DisplayStyle.None;
            return;
        }

        BuildableDefinition definition = BuildableDatabase.Instance != null ? BuildableDatabase.Instance.GetById(instance.BuildableId) : null;
        float fraction = health.Max > 0f ? Mathf.Clamp01(health.Current / health.Max) : 0f;
        SetLabel(structInfoTitle, $"{(definition != null ? definition.DisplayName : instance.BuildableId)}  {Mathf.CeilToInt(health.Current)} / {Mathf.CeilToInt(health.Max)}");
        if (structBarFill != null)
        {
            structBarFill.style.width = Length.Percent(fraction * 100f);
            structBarFill.style.backgroundColor = fraction > 0.6f ? new Color(0.43f, 0.86f, 0.51f) : fraction > 0.3f ? new Color(0.97f, 0.8f, 0.3f) : new Color(0.95f, 0.3f, 0.25f);
        }

        ItemDefinition repairItem = definition != null ? definition.RepairItem : null;
        string hint = health.Current < health.Max ? (repairItem != null ? $"Z 길게: 수리 ({repairItem.DisplayName} 1개씩)" : "Z 길게: 수리") : "";
        SetLabel(structInfoHint, hint);
        if (structInfoHint != null) structInfoHint.style.display = hint.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        structInfoRow.style.display = DisplayStyle.Flex;
    }

    private static string BuildRejectedMessage(BuildPlacementStatus status)
    {
        switch (status)
        {
            case BuildPlacementStatus.NoResources: return "자원이 부족합니다";
            case BuildPlacementStatus.NeedsUpperLevel: return "지붕은 위층에만 지을 수 있습니다 (R로 층 올리기)";
            case BuildPlacementStatus.NoSupport: return "아래에서 받쳐 줄 건축물이 필요합니다";
            case BuildPlacementStatus.Occupied: return "이미 같은 종류의 건축물이 있습니다";
            case BuildPlacementStatus.NothingToDemolish: return "철거할 건축물을 조준하세요";
            case BuildPlacementStatus.HasDependents: return "위에 얹히거나 붙어 있는 건축물을 먼저 철거하세요";
            case BuildPlacementStatus.Overlapping: return "다른 건축물과 겹칩니다";
            default: return null;
        }
    }

    private void ShowBuildNotice(BuildPlacementStatus status)
    {
        ShowNotice(BuildRejectedMessage(status));
    }

    // success가 true면 오류(붉은색) 대신 성공(초록색) 스타일로 보여준다.
    private void ShowNotice(string message, bool success = false)
    {
        if (buildNoticeRow == null || buildNotice == null || message == null) return;

        buildNotice.text = message;
        buildNotice.EnableInClassList("build-notice-success", success);
        buildNoticeRow.style.display = DisplayStyle.Flex;
        buildNoticeHide?.Pause();
        buildNoticeHide = buildNoticeRow.schedule.Execute(() => buildNoticeRow.style.display = DisplayStyle.None).StartingIn(BuildNoticeMs);
    }

    // ---- 줍기 알림 ----

    private void ShowPickup(ItemDefinition item, int quantity)
    {
        if (pickupFeed == null || item == null || quantity <= 0) return;

        CombatAudioLibrary audioLibrary = CombatAudioLibrary.Instance;
        if (combatAudio != null && audioLibrary != null) combatAudio.Play(audioLibrary.Pickup);

        if (!pickupToasts.TryGetValue(item, out PickupToast toast))
        {
            toast = CreatePickupToast(item);
            pickupToasts[item] = toast;
            pickupFeed.Add(toast.Root);
            TrimPickupFeed();
        }

        toast.Amount += quantity;
        toast.Quantity.text = "+" + toast.Amount;
        toast.Total.text = "보유 " + (playerInventory != null ? playerInventory.GetQuantity(item) : toast.Amount);

        // 새로 주울 때마다 사라지는 시간을 다시 잰다.
        toast.Root.style.opacity = 1f;
        toast.Fade?.Pause();
        toast.Remove?.Pause();
        PickupToast captured = toast;
        ItemDefinition capturedItem = item;
        toast.Fade = toast.Root.schedule.Execute(() => captured.Root.style.opacity = 0f).StartingIn(PickupFadeDelayMs);
        toast.Remove = toast.Root.schedule.Execute(() =>
        {
            captured.Root.RemoveFromHierarchy();
            pickupToasts.Remove(capturedItem);
        }).StartingIn(PickupRemoveDelayMs);
    }

    private PickupToast CreatePickupToast(ItemDefinition item)
    {
        PickupToast toast = new PickupToast { Root = new VisualElement { pickingMode = PickingMode.Ignore } };
        toast.Root.AddToClassList("pickup-toast");
        if (item.Category == ItemCategory.Ammo) toast.Root.AddToClassList("pickup-toast-ammo");

        toast.Quantity = new Label { pickingMode = PickingMode.Ignore };
        toast.Quantity.AddToClassList("pickup-qty");
        Label nameLabel = new Label(item.DisplayName) { pickingMode = PickingMode.Ignore };
        nameLabel.AddToClassList("pickup-name");
        toast.Total = new Label { pickingMode = PickingMode.Ignore };
        toast.Total.AddToClassList("pickup-total");

        toast.Root.Add(toast.Quantity);
        toast.Root.Add(nameLabel);
        toast.Root.Add(toast.Total);
        return toast;
    }

    // 알림이 너무 많이 쌓이면 가장 오래된 것부터 지운다.
    private void TrimPickupFeed()
    {
        while (pickupFeed.childCount > MaxPickupToasts)
        {
            VisualElement oldest = pickupFeed[0];
            ItemDefinition key = null;
            foreach (KeyValuePair<ItemDefinition, PickupToast> pair in pickupToasts)
            {
                if (pair.Value.Root != oldest) continue;
                key = pair.Key;
                break;
            }
            oldest.RemoveFromHierarchy();
            if (key != null) pickupToasts.Remove(key);
        }
    }

    // ---- 인벤토리 ----

    private void RefreshInventory()
    {
        if (playerInventory == null || inventoryGrid == null) return;
        // 목록이 다시 만들어지면 열려 있던 메뉴가 가리키던 칸이 사라지므로 닫는다.
        HideItemMenu();
        inventoryGrid.Clear();

        foreach (ItemStack stack in playerInventory.Stacks)
        {
            if (stack.Definition == null) continue;
            ItemDefinition def = stack.Definition;
            int quantity = stack.Quantity;

            VisualElement cell = CreateGridCell();
            SetCellIcon(cell.Q<VisualElement>("cell-icon"), def.Icon, def.DisplayName);
            cell.Q<Label>("cell-qty").text = "x" + quantity;
            RegisterTooltip(cell, def.DisplayName, $"{ItemCategoryLabel(def.Category)}\n보유 {quantity} / 최대 {def.MaxStack}\n클릭하면 사용 / 버리기");
            VisualElement clickedCell = cell;
            cell.RegisterCallback<ClickEvent>(evt => OpenItemMenu(def, quantity, clickedCell));
            inventoryGrid.Add(cell);
        }
    }

    private static string BuildConsumableTooltip(ConsumableDefinition item)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        if (item.HungerRestore > 0f) sb.Append("허기 +").Append(Mathf.RoundToInt(item.HungerRestore)).Append("  ");
        if (item.ThirstRestore > 0f) sb.Append("갈증 +").Append(Mathf.RoundToInt(item.ThirstRestore)).Append("  ");
        if (item.HealthRestore > 0f) sb.Append("체력 +").Append(Mathf.RoundToInt(item.HealthRestore));
        else if (item.HealthRestore < 0f) sb.Append("체력 ").Append(Mathf.RoundToInt(item.HealthRestore));
        return sb.ToString().TrimEnd();
    }

    // ---- 인벤토리 아이템 메뉴 ----

    private void SetupItemMenu(VisualElement root)
    {
        itemMenu = root.Q<VisualElement>("item-menu");
        if (itemMenu == null) return;

        itemMenuTitle = root.Q<Label>("item-menu-title");
        itemMenuUse = root.Q<Button>("item-menu-use");
        itemMenuDropOne = root.Q<Button>("item-menu-drop-one");
        itemMenuDropAll = root.Q<Button>("item-menu-drop-all");
        itemMenuUse.clicked += UseMenuItem;
        itemMenuDropOne.clicked += () => DropMenuItem(1);
        itemMenuDropAll.clicked += () => DropMenuItem(menuQuantity);
        itemMenu.style.display = DisplayStyle.None;

        // 메뉴 밖을 누르면 닫는다(버튼을 누르는 경우는 버튼이 직접 처리한다).
        root.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (menuItem == null) return;
            if (evt.target is VisualElement target && (target == itemMenu || itemMenu.Contains(target))) return;
            HideItemMenu();
        }, TrickleDown.TrickleDown);
    }

    // 클릭한 칸 옆에 사용/버리기 메뉴를 연다. 사용은 소비 아이템만 되고(장비는 장비창에서 장착한다), 버리기는 1개/모두를 고른다.
    private void OpenItemMenu(ItemDefinition item, int quantity, VisualElement cell)
    {
        if (itemMenu == null || item == null) return;

        menuItem = item;
        menuQuantity = quantity;
        HideTooltip();

        itemMenuTitle.text = $"{item.DisplayName}  x{quantity}";
        bool consumable = item is ConsumableDefinition;
        itemMenuUse.text = consumable ? "사용" : "사용 불가";
        itemMenuUse.SetEnabled(consumable);
        itemMenuDropOne.text = quantity > 1 ? "1개 버리기" : "버리기";
        itemMenuDropAll.style.display = quantity > 1 ? DisplayStyle.Flex : DisplayStyle.None;
        itemMenuDropAll.text = $"모두 버리기 (x{quantity})";

        itemMenu.style.display = DisplayStyle.Flex;
        itemMenu.BringToFront();
        // 칸의 오른쪽 아래에 놓되 화면 밖으로 나가지 않게 맞춘다.
        Rect cellRect = hudRoot.WorldToLocal(cell.worldBound);
        float width = 170f, height = quantity > 1 ? 150f : 118f;
        float left = Mathf.Min(cellRect.xMax - 10f, hudRoot.layout.width - width - 8f);
        float top = Mathf.Min(cellRect.yMax - 10f, hudRoot.layout.height - height - 8f);
        itemMenu.style.left = Mathf.Max(8f, left);
        itemMenu.style.top = Mathf.Max(8f, top);
    }

    private void HideItemMenu()
    {
        menuItem = null;
        if (itemMenu != null) itemMenu.style.display = DisplayStyle.None;
    }

    private void UseMenuItem()
    {
        if (menuItem is ConsumableDefinition consumable) UseConsumable(consumable);
        HideItemMenu();
    }

    private void DropMenuItem(int quantity)
    {
        ItemDefinition item = menuItem;
        HideItemMenu();
        if (item == null || itemDropper == null) return;

        if (itemDropper.Drop(item, quantity)) ShowNotice($"{item.DisplayName} {quantity}개를 버렸습니다");
        else ShowNotice("버릴 수 없습니다");
    }

    private void UseConsumable(ConsumableDefinition item)
    {
        if (NetworkInventory.ReplicaActive)
        {
            if (survivalStats != null && item != null) NetworkInventory.Local.RequestConsume(item.StableId);
            return;
        }
        if (survivalStats == null || !survivalStats.TryConsume(item)) return;
        HideTooltip();
        RefreshInventory();
        RefreshHealth();
    }

    private void SetInventoryOpen(bool open)
    {
        inventoryOpen = open;
        if (!open) HideItemMenu();
        if (inventoryPanel != null)
        {
            inventoryPanel.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            if (open)
            {
                inventoryPanel.BringToFront();
                CloseStorageInternal();
                SetCraftingOpenInternal(false);
                SetEquipmentOpenInternal(false);
                CloseBuildModeIfActive();
            }
        }
        UpdatePointerLock();
    }

    // ---- 제작 ----

    // 레시피 목록은 실행 중 바뀌지 않으므로 Awake에서 한 번만 채운다. 재료 보유량 같은 동적
    // 정보는 툴팁이 호버 시점에 직접 계산해서 보여준다.
    private void RefreshCraftingGrid()
    {
        if (craftingGrid == null) return;
        craftingGrid.Clear();

        foreach (RecipeDefinition recipe in recipes)
        {
            if (recipe == null) continue;
            RecipeDefinition captured = recipe;

            VisualElement cell = CreateGridCell();
            Sprite icon = recipe.OutputItem != null ? recipe.OutputItem.Icon : null;
            SetCellIcon(cell.Q<VisualElement>("cell-icon"), icon, recipe.DisplayName);
            cell.Q<Label>("cell-qty").text = "x" + recipe.OutputQuantity;
            cell.AddManipulator(new Clickable(() => Craft(captured)));
            cell.RegisterCallback<PointerEnterEvent>(_ => ShowTooltip(captured.DisplayName, BuildRecipeTooltip(captured)));
            cell.RegisterCallback<PointerLeaveEvent>(_ => HideTooltip());
            cell.RegisterCallback<PointerMoveEvent>(evt => PositionTooltip(evt.position));
            craftingGrid.Add(cell);
        }
    }

    private string BuildRecipeTooltip(RecipeDefinition recipe)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append("필요 재료\n");
        foreach (RecipeDefinition.Ingredient ingredient in recipe.Inputs)
        {
            if (ingredient.item == null) continue;
            int have = playerInventory != null ? playerInventory.GetQuantity(ingredient.item) : 0;
            sb.Append(ingredient.item.DisplayName).Append(' ').Append(have).Append('/').Append(ingredient.quantity).Append('\n');
        }
        sb.Append("\n결과: ");
        sb.Append(recipe.OutputItem != null ? recipe.OutputItem.DisplayName : "?");
        sb.Append(" x").Append(recipe.OutputQuantity);
        if (recipe.RequiredStation != CraftingStationType.None)
        {
            bool near = craftingSystem != null && craftingSystem.IsStationAvailable(recipe);
            sb.Append("\n작업대: ").Append(StationLabel(recipe.RequiredStation)).Append(near ? " (근처)" : " (멀리 있음)");
        }
        return sb.ToString();
    }

    private static string StationLabel(CraftingStationType type)
    {
        switch (type)
        {
            case CraftingStationType.Campfire: return "모닥불";
            default: return type.ToString();
        }
    }

    // 서버가 제작 요청을 처리할 때 이름으로 레시피를 찾는다.
    public RecipeDefinition FindRecipe(string recipeName)
    {
        foreach (RecipeDefinition recipe in recipes)
            if (recipe != null && recipe.name == recipeName) return recipe;
        return null;
    }

    private void Craft(RecipeDefinition recipe)
    {
        if (craftingSystem == null) return;
        // 멀티플레이 참가자의 인벤토리는 복제본이라 서버에 요청한다(결과 알림과 목록 갱신은 서버 응답이 한다).
        if (NetworkInventory.ReplicaActive)
        {
            NetworkInventory.Local.RequestCraft(recipe.name);
            return;
        }
        if (craftingSystem.TryCraft(recipe))
        {
            RefreshInventory();
            ShowNotice(recipe.GetCompletionMessage(), true);
        }
        else if (!craftingSystem.IsStationAvailable(recipe))
        {
            ShowNotice(StationLabel(recipe.RequiredStation) + " 근처에서만 제작할 수 있습니다");
        }
        else
        {
            ShowNotice("재료가 부족합니다");
        }
    }

    private void SetCraftingOpen(bool open)
    {
        craftingOpen = open;
        if (craftingPanel != null)
        {
            craftingPanel.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            if (open)
            {
                craftingPanel.BringToFront();
                CloseStorageInternal();
                SetInventoryOpenInternal(false);
                SetEquipmentOpenInternal(false);
                CloseBuildModeIfActive();
            }
        }
        UpdatePointerLock();
    }

    // SetInventoryOpen/SetCraftingOpen이 서로를 닫을 때 재귀적으로 서로를 또 닫으려 들지 않도록
    // 상태만 바꾸는 내부용 버전을 따로 둔다(둘 다 "열 때"만 상대를 닫으므로 실제로는 무한
    // 재귀가 나지 않지만, 의도를 명확히 하기 위해 분리했다).
    private void SetInventoryOpenInternal(bool open)
    {
        inventoryOpen = open;
        if (inventoryPanel != null) inventoryPanel.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void SetCraftingOpenInternal(bool open)
    {
        craftingOpen = open;
        if (craftingPanel != null) craftingPanel.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // ---- 장비 ----

    // 장비창: 위쪽 슬롯에 장착 중인 무기를, 아래 목록에 인벤토리에 있는 무기를 보여준다.
    // 목록의 무기를 누르면 장착, 장착 중인 무기(목록 칸이나 슬롯)를 누르면 해제한다.
    private void RefreshEquipment()
    {
        if (equipmentGrid == null) return;
        WeaponDefinition equipped = weaponEquipment != null ? weaponEquipment.Current : null;

        // 칸을 누르면 목록이 다시 만들어지는데, 포인터가 있던 칸이 사라지면 PointerLeave가 오지 않아 툴팁이 남는다.
        HideTooltip();

        if (equipmentSlot != null)
        {
            equipmentSlot.Clear();
            equipmentSlot.UnregisterCallback<PointerEnterEvent>(OnEquipmentSlotEnter);
            equipmentSlot.UnregisterCallback<PointerLeaveEvent>(OnEquipmentSlotLeave);
            equipmentSlot.UnregisterCallback<PointerMoveEvent>(OnEquipmentSlotMove);

            VisualElement icon = new VisualElement { name = "cell-icon" };
            icon.AddToClassList("grid-cell-icon");
            equipmentSlot.Add(icon);
            SetCellIcon(icon, equipped != null && equipped.Item != null ? equipped.Item.Icon : null, equipped != null ? equipped.DisplayName : "비움");
            equipmentSlot.EnableInClassList("grid-cell-selected", equipped != null);

            if (equipped != null)
            {
                equipmentSlot.RegisterCallback<PointerEnterEvent>(OnEquipmentSlotEnter);
                equipmentSlot.RegisterCallback<PointerLeaveEvent>(OnEquipmentSlotLeave);
                equipmentSlot.RegisterCallback<PointerMoveEvent>(OnEquipmentSlotMove);
            }
        }

        if (equipmentSlotName != null) equipmentSlotName.text = equipped != null ? equipped.DisplayName : "맨손";
        if (equipmentSlotDetail != null)
            equipmentSlotDetail.text = equipped != null ? WeaponSummary(equipped) + "\n슬롯이나 같은 무기를 눌러 해제" : "보유한 총을 아래에서 선택해 장착한다";

        equipmentGrid.Clear();
        if (playerInventory == null) return;

        RefreshArmorSlots();
        foreach (ItemStack stack in playerInventory.Stacks)
        {
            ArmorDefinition armor = armorEquipment != null ? armorEquipment.FindArmor(stack.Definition) : null;
            if (armor != null)
            {
                ArmorDefinition capturedArmor = armor;
                VisualElement armorCell = CreateGridCell();
                SetCellIcon(armorCell.Q<VisualElement>("cell-icon"), armor.Icon, armor.DisplayName);
                armorCell.Q<Label>("cell-qty").style.display = DisplayStyle.None;
                armorCell.EnableInClassList("grid-cell-selected", armorEquipment.IsEquipped(armor));
                armorCell.AddManipulator(new Clickable(() => ToggleArmor(capturedArmor)));
                RegisterTooltip(armorCell, armor.DisplayName, ArmorSummaryText(armor) + (armorEquipment.IsEquipped(armor) ? "\n장착 중 - 클릭하면 해제" : "\n클릭하면 장착"));
                equipmentGrid.Add(armorCell);
                continue;
            }

            WeaponDefinition weapon = weaponEquipment != null ? weaponEquipment.FindWeapon(stack.Definition) : null;
            if (weapon == null) continue;
            WeaponDefinition captured = weapon;

            VisualElement cell = CreateGridCell();
            SetCellIcon(cell.Q<VisualElement>("cell-icon"), stack.Definition.Icon, weapon.DisplayName);
            cell.Q<Label>("cell-qty").style.display = DisplayStyle.None;
            cell.EnableInClassList("grid-cell-selected", weapon == equipped);
            cell.AddManipulator(new Clickable(() => ToggleEquip(captured)));
            RegisterTooltip(cell, weapon.DisplayName, WeaponSummary(weapon) + (weapon == equipped ? "\n장착 중 - 클릭하면 해제" : "\n클릭하면 장착"));
            equipmentGrid.Add(cell);
        }
    }

    // 방어구 3칸(머리, 몸통, 다리)과 합산 피해 감소율을 그린다. 칸을 누르면 해제한다.
    private void RefreshArmorSlots()
    {
        if (armorSlots == null || armorEquipment == null) return;
        armorSlots.Clear();
        foreach (ArmorSlot slot in System.Enum.GetValues(typeof(ArmorSlot)))
        {
            ArmorDefinition worn = armorEquipment.Get(slot);
            ArmorSlot capturedSlot = slot;
            VisualElement cell = CreateGridCell();
            cell.AddToClassList("armor-slot");
            SetCellIcon(cell.Q<VisualElement>("cell-icon"), worn != null ? worn.Icon : null, ArmorDefinition.SlotLabel(slot));
            cell.Q<Label>("cell-qty").style.display = DisplayStyle.None;
            cell.EnableInClassList("grid-cell-selected", worn != null);
            cell.AddManipulator(new Clickable(() => armorEquipment.Unequip(capturedSlot)));
            RegisterTooltip(cell, worn != null ? worn.DisplayName : ArmorDefinition.SlotLabel(slot), worn != null ? ArmorSummaryText(worn) + "\n클릭하면 해제" : "비어 있음");
            armorSlots.Add(cell);
        }
        if (armorSummary != null) armorSummary.text = $"피해 감소 {armorEquipment.TotalReduction * 100f:0}%";
    }

    private void ToggleArmor(ArmorDefinition armor)
    {
        if (armorEquipment == null) return;
        if (armorEquipment.IsEquipped(armor)) armorEquipment.Unequip(armor.Slot);
        else armorEquipment.TryEquip(armor);
    }

    private static string ArmorSummaryText(ArmorDefinition armor) => $"{ArmorDefinition.SlotLabel(armor.Slot)} / 피해 감소 {armor.DamageReduction * 100f:0}%";

    private void OnEquipmentSlotEnter(PointerEnterEvent evt)
    {
        WeaponDefinition equipped = weaponEquipment != null ? weaponEquipment.Current : null;
        if (equipped != null) ShowTooltip(equipped.DisplayName, WeaponSummary(equipped) + "\n클릭하면 해제");
    }

    private void OnEquipmentSlotLeave(PointerLeaveEvent evt) => HideTooltip();

    private void OnEquipmentSlotMove(PointerMoveEvent evt) => PositionTooltip(evt.position);

    // 슬롯을 누르면 해제한다. 슬롯 칸 클릭은 UXML에 고정된 요소라 Awake에서 한 번만 연결한다.
    private void OnEquipmentSlotClicked()
    {
        if (weaponEquipment != null) weaponEquipment.Unequip();
    }

    private void ToggleEquip(WeaponDefinition weapon)
    {
        if (weaponEquipment == null) return;
        if (weaponEquipment.Current == weapon) weaponEquipment.Unequip();
        else weaponEquipment.TryEquip(weapon);
    }

    private static string WeaponSummary(WeaponDefinition weapon)
    {
        string ammo = weapon.AmmoItem != null ? weapon.AmmoItem.DisplayName : "-";
        string mode = weapon.Automatic ? "연사" : "단발";
        return $"피해 {weapon.Damage:0} / {mode}\n탄창 {weapon.MagazineSize}발 ({ammo})";
    }

    private void SetEquipmentOpen(bool open)
    {
        equipmentOpen = open;
        if (equipmentPanel != null)
        {
            equipmentPanel.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            if (open)
            {
                equipmentPanel.BringToFront();
                CloseStorageInternal();
                SetInventoryOpenInternal(false);
                SetCraftingOpenInternal(false);
                CloseBuildModeIfActive();
                RefreshEquipment();
            }
        }
        UpdatePointerLock();
    }

    private void SetEquipmentOpenInternal(bool open)
    {
        equipmentOpen = open;
        if (equipmentPanel != null) equipmentPanel.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // 죽은 동안 부활 안내를 보인다. 부활 입력을 받기 전(지연 중)에는 비워 둔다.
    private void UpdateDeathHint()
    {
        if (deathHint == null) return;
        bool ready = playerRespawn != null && playerRespawn.CanRespawn;
        string place = playerRespawn != null && playerRespawn.HasBed ? "침대에서 부활" : "시작 위치에서 부활";
        string text = ready ? (GameSaveSystem.HasSave ? $"[Enter] {place}  ·  [F9] 마지막 저장 불러오기" : $"[Enter] {place}") : "";
        if (deathHint.text != text) deathHint.text = text;
        DisplayStyle display = ready ? DisplayStyle.Flex : DisplayStyle.None;
        if (deathHint.style.display != display) deathHint.style.display = display;
    }

    // ---- 상호작용 진행 링 ----

    private void UpdateHoldRing()
    {
        if (holdRing == null || interactionFocus == null) return;
        float progress = interactionFocus.HoldProgress;
        holdRing.Progress = progress;
        holdRing.style.display = progress > 0f ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // ---- 보관함 ----

    private const float StorageCloseDistance = 4.5f;

    // 보관함 패널을 연다. 다른 패널은 닫고, 내용물이 바뀔 때마다 두 목록을 다시 그린다.
    private void OpenStorage(StorageContainer container)
    {
        if (container == null || storagePanel == null || playerInventory == null) return;
        CloseStorageInternal();

        SetInventoryOpenInternal(false);
        SetCraftingOpenInternal(false);
        SetEquipmentOpenInternal(false);
        CloseBuildModeIfActive();

        openStorage = container;
        storageIsOpen = true;
        // 참가자는 서버에 열기를 알리고 내용을 스냅샷으로 받는다.
        if (MultiplayerSession.IsClientOnly && StorageSync.Instance != null) StorageSync.Instance.RequestOpen(container.WorldId);
        container.Contents.Modified += OnStorageDataChanged;
        playerInventory.Modified += OnStorageDataChanged;
        storagePanel.style.display = DisplayStyle.Flex;
        storagePanel.BringToFront();
        RefreshStorage();
        UpdatePointerLock();
    }

    public void CloseStorage()
    {
        if (!storageIsOpen) return;
        CloseStorageInternal();
        UpdatePointerLock();
    }

    // 다른 패널을 여는 쪽이 부르는 내부용이다(커서 잠금은 부른 쪽이 갱신한다).
    private void CloseStorageInternal()
    {
        if (openStorage != null)
        {
            openStorage.Contents.Modified -= OnStorageDataChanged;
            if (MultiplayerSession.IsClientOnly && StorageSync.Instance != null) StorageSync.Instance.RequestClose(openStorage.WorldId);
        }
        if (playerInventory != null) playerInventory.Modified -= OnStorageDataChanged;
        storagePending.Clear();
        openStorage = null;
        storageIsOpen = false;
        if (storagePanel != null) storagePanel.style.display = DisplayStyle.None;
    }

    // Esc/E를 누르거나 보관함이 사라지거나 플레이어가 멀어지면 닫는다.
    private void UpdateStorageGuard()
    {
        if (!storageIsOpen) return;

        bool gone = openStorage == null || !openStorage.isActiveAndEnabled;
        bool far = !gone && playerTransform != null && Vector3.Distance(openStorage.transform.position, playerTransform.position) > StorageCloseDistance;
        bool pressed = Keyboard.current != null && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame);
        if (gone || far || pressed) CloseStorage();
    }

    // 참가자가 보낸 보관함 요청이 서버 응답을 기다리는 동안은 같은 아이템 칸(과 "모두 가져오기")의 클릭을 무시한다. 화면은 서버 스냅샷이 와야 바뀌므로 같은 요청이 두 번 나가는 것을 막는다.
    private readonly PendingRequests storagePending = new PendingRequests();
    private const float StoragePendingSeconds = 1.5f;
    private const string TakeAllKey = "__take_all__";

    // 인벤토리나 보관함 내용이 바뀌었다(서버 스냅샷 도착 포함). 요청 중 표시를 풀고 다시 그린다.
    private void OnStorageDataChanged()
    {
        storagePending.Clear();
        RefreshStorage();
    }

    private void RefreshStorage()
    {
        if (openStorage == null || storageGrid == null || storageInventoryGrid == null || playerInventory == null) return;
        HideTooltip();

        Inventory chest = openStorage.Contents;
        if (storageTitle != null) storageTitle.text = openStorage.DisplayName;
        if (storageContentsTitle != null)
            storageContentsTitle.text = openStorage.SlotCount > 0
                ? $"{openStorage.DisplayName}  {chest.Stacks.Count}/{openStorage.SlotCount}칸"
                : $"{openStorage.DisplayName}  {chest.Stacks.Count}묶음";

        FillStorageGrid(storageGrid, chest, playerInventory, false);
        FillStorageGrid(storageInventoryGrid, playerInventory, chest, true);
    }

    // from의 스택마다 칸을 만든다. 클릭하면 한 묶음, Shift+클릭이면 1개를 to로 옮긴다.
    private void FillStorageGrid(VisualElement grid, Inventory from, Inventory to, bool intoStorage)
    {
        grid.Clear();
        foreach (ItemStack stack in from.Stacks)
        {
            if (stack.Definition == null) continue;
            ItemDefinition def = stack.Definition;
            int quantity = stack.Quantity;

            VisualElement cell = CreateGridCell();
            SetCellIcon(cell.Q<VisualElement>("cell-icon"), def.Icon, def.DisplayName);
            cell.Q<Label>("cell-qty").text = "x" + quantity;
            cell.RegisterCallback<ClickEvent>(evt => MoveStorageItem(from, to, def, evt.shiftKey ? 1 : quantity, intoStorage));
            RegisterTooltip(cell, def.DisplayName, $"{ItemCategoryLabel(def.Category)}\n수량 {quantity} / 최대 {def.MaxStack}\n클릭: 모두 옮기기\nShift+클릭: 1개 옮기기");
            // 서버 응답을 기다리는 칸은 흐리게 보여 눌러도 소용없음을 알린다(참가자만 해당).
            if (MultiplayerSession.IsClientOnly && storagePending.IsPending(def.StableId, Time.unscaledTime)) cell.AddToClassList("grid-cell-pending");
            grid.Add(cell);
        }
    }

    // 열려 있는 보관함의 내용물을 모두 내 인벤토리로 옮긴다(시체 가방을 한 번에 비울 때 쓴다).
    private void TakeAllFromStorage()
    {
        if (openStorage == null || playerInventory == null) return;
        // 참가자는 서버가 옮긴다(결과는 인벤토리와 보관함 스냅샷으로 온다).
        if (MultiplayerSession.IsClientOnly)
        {
            if (!storagePending.TryBegin(TakeAllKey, Time.unscaledTime, StoragePendingSeconds)) return;
            if (StorageSync.Instance != null) StorageSync.Instance.RequestTakeAll(openStorage.WorldId);
            return;
        }
        StorageTransfer.MoveAll(openStorage.Contents, playerInventory);
    }

    // 침대 지정이 바뀌거나 지정한 침대가 사라졌을 때 알린다.
    private void OnBedChanged(RespawnPoint bed, bool removed)
    {
        if (bed != null) ShowNotice("부활 위치를 침대로 지정했습니다", true);
        else if (removed) ShowNotice("침대가 사라져 부활 위치가 시작 위치로 돌아갑니다");
    }

    private void MoveStorageItem(Inventory from, Inventory to, ItemDefinition item, int quantity, bool intoStorage)
    {
        // 참가자는 서버에 요청한다(가득 찬 경우의 알림도 서버가 보낸다).
        if (MultiplayerSession.IsClientOnly)
        {
            if (!storagePending.TryBegin(item.StableId, Time.unscaledTime, StoragePendingSeconds))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[mp-ui] 요청 중이라 클릭을 무시: {item.StableId}");
#endif
                return;
            }
            if (openStorage != null && StorageSync.Instance != null) StorageSync.Instance.RequestMove(openStorage.WorldId, item.StableId, quantity, intoStorage);
            RefreshStorage();
            return;
        }
        int moved = StorageTransfer.Move(from, to, item, quantity);
        if (moved >= quantity || !intoStorage) return;
        ShowNotice(moved == 0 ? "보관함이 가득 찼습니다" : $"보관함이 가득 차서 {moved}개만 넣었습니다");
    }

    // ---- 건축 ----

    // BuildingSystem이 건축 메뉴를 열고 닫을 때마다 호출된다. 항목을 고르면 메뉴만 닫히고
    // 건축 모드는 유지되며, B키로 메뉴를 다시 열 수 있다.
    private void OnBuildMenuChanged(bool open)
    {
        if (buildPanel != null)
        {
            buildPanel.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            if (open)
            {
                buildPanel.BringToFront();
                CloseStorageInternal();
                SetInventoryOpenInternal(false);
                SetCraftingOpenInternal(false);
                SetEquipmentOpenInternal(false);
                RebuildTabs();
                RefreshBuildGrid();
            }
        }
        UpdatePointerLock();
    }

    private void RebuildTabs()
    {
        if (buildTabsContainer == null || buildingSystem == null) return;
        buildTabsContainer.Clear();

        List<BuildableCategory> categories = new List<BuildableCategory>();
        foreach (BuildableDefinition def in buildingSystem.Buildables)
        {
            if (def == null || categories.Contains(def.Category)) continue;
            categories.Add(def.Category);
        }
        if (categories.Count == 0) return;
        if (!categories.Contains(currentBuildCategory)) currentBuildCategory = categories[0];

        foreach (BuildableCategory category in categories)
        {
            Button tab = new Button { text = BuildCategoryLabel(category) };
            tab.AddToClassList("build-tab");
            if (category == currentBuildCategory) tab.AddToClassList("build-tab-active");
            tab.clicked += () =>
            {
                currentBuildCategory = category;
                RebuildTabs();
                RefreshBuildGrid();
            };
            buildTabsContainer.Add(tab);
        }
    }

    private static string BuildCategoryLabel(BuildableCategory category)
    {
        switch (category)
        {
            case BuildableCategory.Wall: return "벽";
            case BuildableCategory.Floor: return "바닥";
            case BuildableCategory.Furniture: return "가구";
            case BuildableCategory.Defense: return "방어";
            case BuildableCategory.Door: return "문";
            case BuildableCategory.Roof: return "지붕";
            case BuildableCategory.Window: return "창문";
            default: return category.ToString();
        }
    }

    private void RefreshBuildGrid()
    {
        if (buildGrid == null || buildingSystem == null) return;
        buildGrid.Clear();

        foreach (BuildableDefinition def in buildingSystem.Buildables)
        {
            if (def == null || def.Category != currentBuildCategory) continue;
            BuildableDefinition captured = def;

            VisualElement cell = CreateGridCell();
            SetCellIcon(cell.Q<VisualElement>("cell-icon"), def.Icon, def.DisplayName);
            // 건축물은 "보유 수량" 개념이 없으므로(자원을 매번 소모해서 짓는 것) 수량 배지는 숨긴다.
            cell.Q<Label>("cell-qty").style.display = DisplayStyle.None;

            bool isSelected = System.Array.IndexOf(buildingSystem.Buildables, def) == buildingSystem.SelectedIndex;
            cell.EnableInClassList("grid-cell-selected", isSelected);

            cell.AddManipulator(new Clickable(() =>
            {
                buildingSystem.SelectBuildable(captured);
                buildingSystem.SetBuildMenuOpen(false);
            }));
            RegisterTooltip(cell, def.DisplayName, BuildCostTooltip(def));
            buildGrid.Add(cell);
        }
    }

    private string BuildCostTooltip(BuildableDefinition def)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append("필요 자원\n");
        foreach (BuildableDefinition.Cost cost in def.Costs)
        {
            if (cost.item == null) continue;
            int have = playerInventory != null ? playerInventory.GetQuantity(cost.item) : 0;
            sb.Append(cost.item.DisplayName).Append(' ').Append(have).Append('/').Append(cost.quantity).Append('\n');
        }
        return sb.ToString();
    }
}
