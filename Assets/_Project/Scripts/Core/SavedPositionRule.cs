// 저장된 플레이어 위치가 지금 맵에서 쓸 수 있는지 판단하는 규칙이다. 지형 아래인데 받쳐 주는 바닥도 없으면 맵이 바뀌기 전의 옛 좌표로 보고 쓰지 않는다. 유니티 컴포넌트에 의존하지 않는다.
using System;

public static class SavedPositionRule
{
    // 지면보다 이만큼 이상 아래이고 바닥이 없으면 땅속에 묻힌 위치다(광산처럼 지형 아래에 바닥이 있는 실내는 해당하지 않는다).
    public const float BuriedMargin = 1f;

    public static bool IsBuried(float savedY, float terrainHeight, bool hasSupportBelow)
    {
        if (float.IsNaN(savedY) || float.IsInfinity(savedY)) return true;
        if (float.IsNaN(terrainHeight)) return false;
        return savedY < terrainHeight - BuriedMargin && !hasSupportBelow;
    }
}
