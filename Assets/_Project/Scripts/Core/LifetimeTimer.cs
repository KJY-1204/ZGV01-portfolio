// 정해진 시간이 지나면 만료되는 수명 하나의 순수 계산이다. 유니티 컴포넌트에 의존하지 않고, 시간은 호출하는 쪽이 넣는다.
using UnityEngine;

public class LifetimeTimer
{
    public float Total { get; }
    public float Remaining { get; private set; }
    public bool IsExpired => Remaining <= 0f;

    public LifetimeTimer(float totalSeconds)
    {
        Total = Mathf.Max(0f, totalSeconds);
        Remaining = Total;
    }

    // dt초만큼 흐르게 한다. 이번 호출로 처음 만료됐으면 true를 돌려준다(이미 만료된 뒤에는 false).
    public bool Tick(float dt)
    {
        if (IsExpired || dt <= 0f) return false;
        Remaining = Mathf.Max(0f, Remaining - dt);
        return IsExpired;
    }

    // 저장된 남은 시간으로 되돌린다(0~전체 시간으로 자른다).
    public void SetRemaining(float seconds)
    {
        Remaining = Mathf.Clamp(seconds, 0f, Total);
    }
}
