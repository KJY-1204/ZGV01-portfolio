// 피해 입은 건축물 위에 체력 막대를 띄우는 오버레이다. 건축물의 월드 위치를 매 프레임 UI 패널 좌표로 옮겨 풀링한 UI Toolkit 요소를 그 자리에 놓는다.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class StructureBarOverlay
{
    private const float ShowRange = 28f;
    private const int MaxBars = 10;
    private const float BarWidth = 52f;
    private const float BarHeight = 7f;
    // 건축물 윗면에서 막대를 띄우는 높이(m)다.
    private const float AnchorLift = 0.35f;

    private readonly VisualElement container;
    private readonly List<VisualElement> bars = new List<VisualElement>();
    private readonly List<VisualElement> fills = new List<VisualElement>();
    private readonly List<(BuildableHealth health, float sqrDistance)> candidates = new List<(BuildableHealth, float)>();

    public StructureBarOverlay(VisualElement hudRoot)
    {
        container = new VisualElement { name = "world-bars", pickingMode = PickingMode.Ignore };
        container.AddToClassList("world-bars");
        hudRoot.Add(container);
        // 다른 HUD 요소(패널, 알림)보다 아래에 깔리도록 맨 앞에 둔다.
        container.SendToBack();
    }

    // 매 프레임 호출한다. 카메라 앞에 있고 가까운 피해 입은 건축물 중 가까운 것부터 MaxBars개에 막대를 붙인다.
    public void Tick(Camera cam, Vector3 playerPosition)
    {
        if (cam == null || container.panel == null)
        {
            HideFrom(0);
            return;
        }

        candidates.Clear();
        float maxSqr = ShowRange * ShowRange;
        foreach (BuildableHealth health in BuildableHealth.Active)
        {
            if (health == null || health.IsBroken || !health.HasBounds || health.Current >= health.Max) continue;

            float sqr = (health.WorldBounds.center - playerPosition).sqrMagnitude;
            if (sqr <= maxSqr) candidates.Add((health, sqr));
        }
        candidates.Sort((a, b) => a.sqrDistance.CompareTo(b.sqrDistance));

        int used = 0;
        for (int i = 0; i < candidates.Count && used < MaxBars; i++)
        {
            BuildableHealth health = candidates[i].health;
            Bounds bounds = health.WorldBounds;
            Vector3 anchor = new Vector3(bounds.center.x, bounds.max.y + AnchorLift, bounds.center.z);

            // 카메라 뒤쪽이거나 화면 밖이면 건너뛴다.
            Vector3 viewport = cam.WorldToViewportPoint(anchor);
            if (viewport.z <= 0f || viewport.x < -0.05f || viewport.x > 1.05f || viewport.y < -0.05f || viewport.y > 1.05f) continue;

            Vector2 panelPosition = RuntimePanelUtils.CameraTransformWorldToPanel(container.panel, anchor, cam);
            VisualElement bar = GetBar(used);
            bar.style.display = DisplayStyle.Flex;
            bar.style.left = panelPosition.x - BarWidth * 0.5f;
            bar.style.top = panelPosition.y - BarHeight * 0.5f;

            float fraction = Mathf.Clamp01(health.Current / health.Max);
            VisualElement fill = fills[used];
            fill.style.width = Length.Percent(fraction * 100f);
            fill.style.backgroundColor = fraction > 0.6f ? new Color(0.43f, 0.86f, 0.51f) : fraction > 0.3f ? new Color(0.97f, 0.8f, 0.3f) : new Color(0.95f, 0.3f, 0.25f);

            // 멀수록 흐려져 화면이 지저분해지지 않는다.
            float distance = Mathf.Sqrt(candidates[i].sqrDistance);
            bar.style.opacity = Mathf.Clamp01(1.4f - distance / ShowRange);
            used++;
        }
        HideFrom(used);
    }

    private VisualElement GetBar(int index)
    {
        while (bars.Count <= index)
        {
            VisualElement bar = new VisualElement { pickingMode = PickingMode.Ignore };
            bar.AddToClassList("world-bar");
            VisualElement fill = new VisualElement { pickingMode = PickingMode.Ignore };
            fill.AddToClassList("world-bar-fill");
            bar.Add(fill);
            container.Add(bar);
            bars.Add(bar);
            fills.Add(fill);
        }
        return bars[index];
    }

    private void HideFrom(int index)
    {
        for (int i = index; i < bars.Count; i++) bars[i].style.display = DisplayStyle.None;
    }
}
