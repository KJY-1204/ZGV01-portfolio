// 자동 저장의 주기와 저장해도 되는 상태를 정하는 규칙이다. 유니티 컴포넌트에 의존하지 않는다.
public static class AutoSaveRule
{
    // 자동 저장 주기(실제 시간 초)다. 약 10분.
    public const float IntervalSeconds = 600f;

    // 마지막 저장 뒤로 주기가 지났는지다.
    public static bool IsDue(float now, float lastSaved, float intervalSeconds = IntervalSeconds)
    {
        return now - lastSaved >= intervalSeconds;
    }

    // 지금 자동 저장을 해도 되는 상태인지다. 메뉴 씬이거나, 멀티플레이 참가자(저장은 호스트가 한다)이거나, 플레이어가 없거나 죽어 있으면 하지 않는다
    // (죽은 상태를 저장하지 않고 되살아난 뒤에 저장한다).
    public static bool CanSave(bool inMenuScene, bool clientOnly, bool hasPlayer, bool playerDead)
    {
        return !inMenuScene && !clientOnly && hasPlayer && !playerDead;
    }
}
