// 조준한 건축물의 체력을 HUD에 알리고, Z를 길게 누르면 재료 1개로 체력을 조금씩 수리하는 컴포넌트다.
using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Inventory))]
public class StructureInspector : MonoBehaviour
{
    [SerializeField] private float inspectRange = 8f;
    [SerializeField] private float repairRange = 4.5f;
    // 한 번 수리할 때 채우는 체력 비율과 수리 사이 간격(초)이다. 한 번에 재료 1개를 쓴다.
    [SerializeField, Range(0.05f, 1f)] private float repairFraction = 0.1f;
    [SerializeField] private float repairInterval = 0.8f;

    private Inventory inventory;
    private Health ownHealth;
    private BuildingSystem buildingSystem;
    private BuildableInstance shown;
    private BuildableHealth shownHealth;
    private float shownCurrent = -1f;
    private float nextRepairTime;

    // 조준 대상이 바뀌거나 그 체력이 바뀔 때 알린다(보여 줄 대상이 없으면 둘 다 null).
    public event Action<BuildableInstance, BuildableHealth> AimChanged;
    // 수리할 재료가 없어서 수리하지 못했을 때 알린다.
    public event Action RepairFailed;

    private void Awake()
    {
        inventory = GetComponent<Inventory>();
        ownHealth = GetComponent<Health>();
        buildingSystem = GetComponent<BuildingSystem>();
    }

    private void Update()
    {
        bool blocked = HudController.AnyPanelOpen || (ownHealth != null && ownHealth.IsDead);
        BuildableInstance target = blocked ? null : FindAimed(transform, inspectRange);
        BuildableHealth health = target != null ? target.GetComponent<BuildableHealth>() : null;

        // 체력이 있는 건축물만 보여 주고, 평소에는 피해를 입은 것만(건축 모드에서는 모두) 보여 준다.
        bool buildMode = buildingSystem != null && buildingSystem.BuildModeActive;
        bool show = health != null && !health.IsBroken && (health.Current < health.Max || buildMode);
        if (!show)
        {
            target = null;
            health = null;
        }

        float current = health != null ? health.Current : -1f;
        if (target != shown || !Mathf.Approximately(current, shownCurrent))
        {
            shown = target;
            shownHealth = health;
            shownCurrent = current;
            AimChanged?.Invoke(shown, shownHealth);
        }

        TickRepair();
    }

    // Z를 누르고 있는 동안 가까이 조준한 피해 입은 건축물을 일정 간격으로 수리한다.
    private void TickRepair()
    {
        if (shownHealth == null || Keyboard.current == null || !KeyBindings.Held(GameKey.Repair)) return;
        if (shownHealth.Current >= shownHealth.Max || Time.time < nextRepairTime) return;
        if (Vector3.Distance(transform.position, shownHealth.WorldBounds.ClosestPoint(transform.position)) > repairRange) return;

        nextRepairTime = Time.time + repairInterval;

        ItemDefinition item = RepairItemOf(shown);
        // 참가자는 서버에 요청만 한다. 재료 차감과 수리는 서버가 확정한다(자기 인벤토리는 서버 복제본이라 직접 못 바꾼다).
        if (MultiplayerSession.IsClientOnly)
        {
            if (item != null && inventory.GetQuantity(item) < 1) RepairFailed?.Invoke();
            else if (BuildSync.Instance != null) BuildSync.Instance.RequestRepair(shown.WorldId);
            return;
        }

        if (item != null && !inventory.Remove(item, 1))
        {
            RepairFailed?.Invoke();
            return;
        }

        RepairNow(shownHealth);
    }

    private static ItemDefinition RepairItemOf(BuildableInstance piece)
    {
        BuildableDefinition definition = BuildableDatabase.Instance != null && piece != null
            ? BuildableDatabase.Instance.GetById(piece.BuildableId)
            : null;
        return definition != null ? definition.RepairItem : null;
    }

    private void RepairNow(BuildableHealth target)
    {
        target.Repair(target.Max * repairFraction);
        NoiseSystem.Emit(target.WorldBounds.center, 10f, this);
    }

    // 서버가 참가자의 수리 요청을 검증하고 실행한다. 재료는 참가자의 서버 인벤토리에서 쓴다.
    public bool ServerRepair(Vector3 avatarPosition, BuildableInstance piece, Inventory payer)
    {
        BuildableHealth target = piece != null ? piece.GetComponent<BuildableHealth>() : null;
        if (target == null || payer == null || target.IsBroken || target.Current >= target.Max) return false;
        if (Vector3.Distance(avatarPosition, target.WorldBounds.ClosestPoint(avatarPosition)) > repairRange + 3f) return false;

        ItemDefinition item = RepairItemOf(piece);
        if (item != null && !payer.Remove(item, 1)) return false;

        RepairNow(target);
        return true;
    }

    // 화면 중앙에서 쏜 선이 처음 맞는 건축물이다. 건축물이 아닌 것에 먼저 막히면 없다. 플레이어에게서 range 안일 때만 돌려준다.
    public static BuildableInstance FindAimed(Transform player, float range)
    {
        Camera cam = Camera.main;
        if (cam == null) return null;

        RaycastHit[] hits = Physics.RaycastAll(cam.transform.position, cam.transform.forward, range + 6f, AvatarBody.ExcludeMask, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(player)) continue;
            BuildableInstance instance = hit.collider.GetComponentInParent<BuildableInstance>();
            if (instance == null || string.IsNullOrEmpty(instance.WorldId)) return null;
            return Vector3.Distance(player.position, hit.point) <= range ? instance : null;
        }
        return null;
    }
}
