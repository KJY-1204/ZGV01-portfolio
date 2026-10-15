// 거점 유무, 게임 시작부터 흐른 시간, 시각에 따라 좀비가 정찰할지 밤새 플레이어를 쫓을지 정하는 순수 규칙이다(게임 시간 기준, 엔진에 의존하지 않는다).
using System;

public static class ZombieThreatRule
{
    // 게임을 시작하고 이만큼(게임 일)이 지나기 전에는 거점이 없으면 좀비가 생성된 곳 주변만 정찰한다. 거점을 잃어도 이 기간은 다시 시작하지 않는다.
    public const float GraceDays = 10f;
    public const float GraceHours = GraceDays * 24f;

    // 게임 시작부터 흐른 게임 시간(시)을 더한다(거점 유무와 상관없다).
    public static float AdvanceElapsedHours(float current, float deltaGameHours)
    {
        return Math.Max(0f, current) + Math.Max(0f, deltaGameHours);
    }

    // 거점이 없고 유예 기간(게임 시작 후 10일)이 남아 있으면 정찰한다.
    public static bool Patrols(bool hasBase, float elapsedGameHours)
    {
        return !hasBase && elapsedGameHours < GraceHours;
    }

    // 정찰하지 않는 밤에는 좀비가 플레이어 위치를 계속 알고 쫓아가 공격한다(낮에는 좀비가 나오지 않거나 불탄다).
    public static bool Hunts(bool isNight, bool hasBase, float elapsedGameHours)
    {
        return isNight && !Patrols(hasBase, elapsedGameHours);
    }
}

