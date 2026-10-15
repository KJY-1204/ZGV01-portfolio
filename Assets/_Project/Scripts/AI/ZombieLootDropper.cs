// 좀비 사망 시 드랍 테이블을 굴려 월드에 전리품을 스폰하는 권위 컴포넌트다.
using UnityEngine;

[RequireComponent(typeof(Health))]
public class ZombieLootDropper : MonoBehaviour
{
    [SerializeField] private DropTable dropTable;
    [SerializeField] private GameObject lootPickupPrefab;

    private Health health;
    private bool lootSuppressed;

    private void Awake()
    {
        health = GetComponent<Health>();
        health.Died += HandleDied;
    }

    private void OnDestroy()
    {
        health.Died -= HandleDied;
    }

    // 햇빛에 불타 죽는 것처럼 전리품이 없어야 하는 죽음이면 죽기 전에 부른다.
    public void SuppressLoot() => lootSuppressed = true;

    private void HandleDied()
    {
        if (lootSuppressed) return;
        // 전리품은 서버가 굴려 네트워크로 놓는다. 참가자 쪽에서는 만들지 않는다.
        if (MultiplayerSession.IsClientOnly) return;
        if (dropTable == null || lootPickupPrefab == null) return;
        if (!dropTable.TryRoll(out ItemDefinition item, out int quantity)) return;

        LootPickup.Spawn(lootPickupPrefab, transform.position, item, quantity);
    }
}
