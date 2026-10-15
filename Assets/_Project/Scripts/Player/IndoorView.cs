// 플레이어가 건물 안(IndoorZone)에 들어가면 1인칭으로, 나오면 원래 3인칭으로 돌려 주는 컴포넌트다. 카메라 거리/시야각은 CombatCameraController가 부드럽게 바꾸고, 여기서는 안/밖 판정과 몸 숨기기만 한다.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class IndoorView : MonoBehaviour
{
    // 머리 높이 근처를 판정 점으로 쓴다(바닥이 구역 경계에 걸쳐 깜빡이지 않게).
    [SerializeField] private float probeHeight = 1.2f;

    private readonly List<Renderer> bodyRenderers = new List<Renderer>();
    private readonly List<ShadowCastingMode> originalModes = new List<ShadowCastingMode>();
    private readonly List<bool> hideInFirstPerson = new List<bool>();
    private readonly List<bool> originalUpdateOffscreen = new List<bool>();

    public bool IsInside { get; private set; }

    private void Awake()
    {
        // 몸 렌더러를 그리지 않으면(카메라에 안 보이면) 기본 설정의 애니메이터가 뼈 갱신을 멈춘다. 그러면 총 자세 코드가 가슴을 매 프레임 비틀어 회전이 쌓이므로 항상 갱신하게 한다.
        Animator animator = GetComponent<Animator>();
        if (animator != null) animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        Transform geometry = transform.Find("Geometry");
        if (geometry == null) return;
        foreach (Renderer renderer in geometry.GetComponentsInChildren<Renderer>(true))
        {
            bodyRenderers.Add(renderer);
            originalModes.Add(renderer.shadowCastingMode);
            originalUpdateOffscreen.Add(renderer is SkinnedMeshRenderer skinned && skinned.updateWhenOffscreen);
            // 1인칭에서는 카메라가 그 안에 있는 머리/모자/상체 옷(셔츠, 조끼)과 등의 배낭, 따로 움직이는 FPS 전용 팔만 숨기고 팔(장갑 포함)/다리는 그대로 보인다.
            string name = renderer.name;
            hideInFirstPerson.Add(name.Contains("Head") || name.Contains("Cap") || name.Contains("Backpack") || name.Contains("Shirt") || name.Contains("Vest") || name.Contains("FPS_Arms"));
        }
    }

    private void Update()
    {
        bool inside = IndoorZone.IsInside(transform.position + Vector3.up * probeHeight);
        if (inside == IsInside) return;

        IsInside = inside;
        SetBodyVisible(!inside);
    }

    // 1인칭에서는 카메라가 몸 안에 있으므로 머리/모자/상체 옷/배낭은 그리지 않고 그림자만 남긴다(팔과 다리는 보인다).
    private void SetBodyVisible(bool visible)
    {
        for (int i = 0; i < bodyRenderers.Count; i++)
        {
            if (bodyRenderers[i] == null) continue;
            bool show = visible || !hideInFirstPerson[i];
            bodyRenderers[i].shadowCastingMode = show ? originalModes[i] : ShadowCastingMode.ShadowsOnly;
            // 스킨 메시의 경계는 몸 아래쪽 기준이라 카메라 바로 앞의 팔이 시야 밖으로 판정돼 그려지지 않는다. 1인칭에서는 매 프레임 경계를 다시 계산한다.
            if (bodyRenderers[i] is SkinnedMeshRenderer skinned) skinned.updateWhenOffscreen = visible ? originalUpdateOffscreen[i] : true;
        }
    }
}
