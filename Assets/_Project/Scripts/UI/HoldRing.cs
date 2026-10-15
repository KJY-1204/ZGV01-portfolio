// 길게 누르는 상호작용의 진행도를 원형 호로 그리는 UI Toolkit 요소다. 값(0~1)만 받아 그리고 게임 상태는 모른다.
using UnityEngine;
using UnityEngine.UIElements;

public class HoldRing : VisualElement
{
    private static readonly Color Track = new Color(1f, 1f, 1f, 0.22f);
    private static readonly Color Fill = new Color(1f, 0.85f, 0.2f, 1f);

    private float progress;

    public HoldRing()
    {
        pickingMode = PickingMode.Ignore;
        style.width = 28;
        style.height = 28;
        style.marginRight = 10;
        style.alignSelf = Align.Center;
        generateVisualContent += Draw;
    }

    public float Progress
    {
        get => progress;
        set
        {
            float clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(clamped, progress)) return;
            progress = clamped;
            MarkDirtyRepaint();
        }
    }

    private void Draw(MeshGenerationContext context)
    {
        Rect rect = contentRect;
        if (rect.width <= 0f || rect.height <= 0f) return;

        Painter2D painter = context.painter2D;
        Vector2 center = rect.center;
        float radius = Mathf.Min(rect.width, rect.height) * 0.5f - 3f;
        painter.lineWidth = 4f;
        painter.lineCap = LineCap.Round;

        painter.strokeColor = Track;
        painter.BeginPath();
        painter.Arc(center, radius, 0f, 360f);
        painter.Stroke();

        if (progress <= 0f) return;
        painter.strokeColor = Fill;
        painter.BeginPath();
        painter.Arc(center, radius, -90f, -90f + 360f * progress);
        painter.Stroke();
    }
}
