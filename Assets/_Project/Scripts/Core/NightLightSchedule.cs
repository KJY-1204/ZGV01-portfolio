// 밤에 켜지는 조명(가로등, 설치형 조명)의 켜짐 정도를 시각(0~24시)으로 정하는 순수 규칙이다. 유니티 컴포넌트에 의존하지 않는다.
using UnityEngine;

public static class NightLightSchedule
{
    // 저녁에 켜지기 시작해 최대가 되는 시각, 새벽에 꺼지기 시작해 완전히 꺼지는 시각이다.
    public const float TurnOnStart = 18.5f;
    public const float TurnOnFull = 19.5f;
    public const float TurnOffStart = 5.5f;
    public const float TurnOffEnd = 6.5f;

    // 0이면 꺼짐, 1이면 최대 밝기다. 켜질 때와 꺼질 때는 한 시간에 걸쳐 부드럽게 변한다.
    public static float Intensity(float hour)
    {
        hour = Mathf.Repeat(hour, 24f);
        if (hour >= TurnOnFull || hour <= TurnOffStart) return 1f;
        if (hour >= TurnOnStart) return Mathf.SmoothStep(0f, 1f, (hour - TurnOnStart) / (TurnOnFull - TurnOnStart));
        if (hour < TurnOffEnd) return 1f - Mathf.SmoothStep(0f, 1f, (hour - TurnOffStart) / (TurnOffEnd - TurnOffStart));
        return 0f;
    }
}
