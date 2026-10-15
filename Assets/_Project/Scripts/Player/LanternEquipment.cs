// 허리춤에 차는 휴대 랜턴이다. 인벤토리에 랜턴 아이템이 있으면 허리에 모델이 달리고, L로 켜고 끈다. 손을 쓰지 않아서 총과 함께 쓴다.
using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Inventory))]
public class LanternEquipment : MonoBehaviour
{
    [SerializeField] private ItemDefinition lanternItem;
    // 모델과 조명이 든 허리 장착용 프리팹이다(루트 아래에 Light가 하나 있다).
    [SerializeField] private GameObject beltPrefab;
    // 골반 본(pelvis) 기준 왼쪽 허리 위치와 똑바로 서게 하는 회전이다(이 리그는 골반의 로컬 축이 눕혀져 있어 값이 낯설다).
    [SerializeField] private Vector3 beltLocalPosition = new Vector3(0.064f, 0.01f, -0.249f);
    [SerializeField] private Vector3 beltLocalEuler = new Vector3(87.7f, 189.1f, 280f);
    [SerializeField, Range(0f, 0.4f)] private float flicker = 0.08f;

    private Inventory inventory;
    private Animator animator;
    private Health health;
    private GameObject belt;
    private Light beltLight;
    private float baseIntensity;
    private float seed;
    private bool on;

    public bool IsOn => on && belt != null;
    public bool Owns => lanternItem != null && inventory != null && inventory.GetQuantity(lanternItem) > 0;

    // 켜거나 껐을 때 알린다(HUD 알림용). 랜턴이 없을 때 켜려고 하면 NoLantern이 온다.
    public event Action<bool> Toggled;
    public event Action NoLantern;

    private void Awake()
    {
        inventory = GetComponent<Inventory>();
        animator = GetComponent<Animator>();
        health = GetComponent<Health>();
        seed = UnityEngine.Random.value * 100f;
    }

    private void Update()
    {
        SyncBelt();

        if (Keyboard.current != null && KeyBindings.Pressed(GameKey.Lantern)
            && !HudController.AnyPanelOpen && !(health != null && health.IsDead))
        {
            if (!Owns) NoLantern?.Invoke();
            else SetOn(!on, true);
        }

        if (beltLight != null && belt != null)
        {
            beltLight.enabled = on;
            beltLight.intensity = baseIntensity * (on ? FlameFlicker.Factor(seed, flicker, 7f) : 0f);
        }
    }

    // 랜턴 아이템을 가졌는지에 맞춰 허리 모델을 달거나 뗀다. 아이템이 사라지면 꺼진 채로 둔다.
    private void SyncBelt()
    {
        bool owns = Owns;
        if (owns && belt == null) Attach();
        else if (!owns && belt != null)
        {
            Destroy(belt);
            belt = null;
            beltLight = null;
            on = false;
        }
    }

    private void Attach()
    {
        if (beltPrefab == null) return;
        // 휴머노이드의 Hips 본이 발치의 루트일 수 있어서 허리 높이의 골반(척추의 부모)에 단다.
        Transform parent = transform;
        if (animator != null && animator.isHuman)
        {
            Transform spine = animator.GetBoneTransform(HumanBodyBones.Spine);
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            parent = spine != null && spine.parent != null ? spine.parent : (hips != null ? hips : transform);
        }
        belt = Instantiate(beltPrefab, parent);
        belt.name = "LanternBelt";
        belt.transform.localPosition = beltLocalPosition;
        belt.transform.localRotation = Quaternion.Euler(beltLocalEuler);
        beltLight = belt.GetComponentInChildren<Light>(true);
        baseIntensity = beltLight != null ? beltLight.intensity : 0f;
    }

    private void SetOn(bool value, bool notify)
    {
        if (on == value) return;
        on = value;
        if (notify) Toggled?.Invoke(on);
    }

    // 저장 불러오기용이다. 알림 없이 켜짐 상태를 맞춘다(랜턴이 없으면 꺼진 채로 둔다).
    public void RestoreOn(bool value)
    {
        SyncBelt();
        on = value && belt != null;
    }
}
