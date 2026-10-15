// 허기/갈증처럼 0과 최대값 사이에서 줄고 채워지는 수치 하나의 순수 계산이다. 유니티 컴포넌트에 의존하지 않는다.
using UnityEngine;

public class SurvivalMeter
{
    public float Max { get; }
    public float Current { get; private set; }

    public float Ratio => Max > 0f ? Current / Max : 0f;
    public bool IsEmpty => Current <= 0f;

    public SurvivalMeter(float max)
    {
        Max = Mathf.Max(1f, max);
        Current = Max;
    }

    public void Drain(float amount)
    {
        if (amount > 0f) Current = Mathf.Max(0f, Current - amount);
    }

    public void Restore(float amount)
    {
        if (amount > 0f) Current = Mathf.Min(Max, Current + amount);
    }

    public void Set(float value)
    {
        Current = Mathf.Clamp(value, 0f, Max);
    }
}
