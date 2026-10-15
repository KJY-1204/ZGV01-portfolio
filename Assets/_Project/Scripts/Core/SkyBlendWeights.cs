// 해의 고도에 따라 밤/노을/낮 하늘의 섞는 비중과 낮 하늘의 따뜻한 정도, 노출, 해/달 원반의 세기를 구하는 순수 규칙이다. 유니티 컴포넌트에 의존하지 않는다.
using System;

public static class SkyBlendWeights
{
    public struct Weights
    {
        public float night, glow, day;
    }

    // 게임 시각(0~24)의 태양 고도(도)다. 해는 6시에 뜨고 12시에 머리 위를 지나 18시에 진다.
    public static float ElevationDegrees(float hour)
    {
        double pitch = (hour / 24.0 * 360.0 - 90.0) * Math.PI / 180.0;
        return (float)(Math.Asin(Math.Sin(pitch)) * 180.0 / Math.PI);
    }

    // 세 하늘의 비중이다(합이 1). 해가 높으면 낮, 지평선 근처면 노을, 한참 아래면 밤이다. 경계를 넓게 겹쳐 서서히 이어지게 한다.
    public static Weights At(float elevation)
    {
        float night = Smooth(-6f, -26f, elevation);
        float day = Smooth(0f, 60f, elevation);
        float glow = elevation >= 0f ? 1f - Smooth(4f, 52f, elevation) : 1f - Smooth(-6f, -22f, elevation);

        float sum = night + glow + day;
        if (sum <= 0.0001f) return new Weights { night = 0f, glow = 1f, day = 0f };
        return new Weights { night = night / sum, glow = glow / sum, day = day / sum };
    }

    // 해가 낮을수록 1에 가까워지는 낮 하늘의 따뜻한 정도다(낮 하늘에 주황빛을 입힌다).
    public static float DayWarmth(float elevation) => 1f - Smooth(0f, 50f, elevation);

    // 낮 하늘 노출이다. 해가 낮으면 어둡다.
    public static float DayExposure(float elevation) => Lerp(0.55f, 1f, Smooth(0f, 55f, elevation));

    // 노을 하늘 노출이다. 해가 높아지거나 지평선 아래로 내려가면 줄어든다.
    public static float GlowExposure(float elevation)
    {
        return elevation >= 0f
            ? Lerp(1f, 0.65f, Smooth(8f, 50f, elevation))
            : Lerp(1f, 0.75f, Smooth(0f, -14f, elevation));
    }

    // 해 원반의 세기다(지평선 근처에서 서서히 나타난다).
    public static float SunDisc(float elevation) => Smooth(1f, 14f, elevation);

    // 달 원반의 세기다. 달은 해의 반대편에서 뜨므로 해가 내려갈수록 보인다.
    public static float MoonDisc(float elevation) => Smooth(-2f, -16f, elevation);

    // edge0에서 edge1로 부드럽게 0→1이 되는 곡선이다(edge0 > edge1이면 반대 방향).
    public static float Smooth(float edge0, float edge1, float x)
    {
        if (Math.Abs(edge1 - edge0) < 0.0001f) return x >= edge1 ? 1f : 0f;
        float t = Math.Max(0f, Math.Min(1f, (x - edge0) / (edge1 - edge0)));
        return t * t * (3f - 2f * t);
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
