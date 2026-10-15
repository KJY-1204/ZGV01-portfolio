// 폐허 가로등의 고장 상태(정상/꺼짐/깜빡임)를 정하고, 시각에 따른 켜짐 배율을 구하는 순수 규칙이다. 유니티 컴포넌트에 의존하지 않는다.
using System;

public enum LampFault
{
    None,
    // 밤에도 켜지지 않는다.
    Dead,
    // 깜빡인 뒤 한동안 꺼지고, 다시 깜빡인 뒤 한동안 켜지기를 번갈아 반복한다.
    Flicker,
}

public static class LampFaultPattern
{
    // 깜빡임 한 주기(초)와 그 주기 앞쪽의 깜빡이는 구간 길이(초)다. 나머지 시간은 꺼짐/켜짐을 번갈아 유지한다.
    public const float FlickerCycleSeconds = 7f;
    public const float FlickerBurstSeconds = 1.6f;
    // 깜빡이는 동안 켜짐/꺼짐이 바뀌는 간격(초)이다.
    public const float FlickerStepSeconds = 0.12f;

    // 한 번 합선이 일어난 뒤 다음 합선까지 최소 간격(초)이다. 한 번의 깜빡임 구간(FlickerBurstSeconds)에 불꽃이 한 번만 튀게 한다.
    public const float SparkCooldownSeconds = 2f;

    // 0~1 난수로 고장 상태를 정한다. 앞 구간이 꺼짐, 다음 구간이 깜빡임, 나머지는 정상이다.
    public static LampFault Assign(float roll, float deadChance, float flickerChance)
    {
        if (roll < deadChance) return LampFault.Dead;
        if (roll < deadChance + flickerChance) return LampFault.Flicker;
        return LampFault.None;
    }

    // 시간표 밝기에 곱할 배율(0 또는 1)이다. 같은 시드와 시각에는 항상 같은 값을 돌려준다.
    public static float Multiplier(LampFault fault, float time, int seed)
    {
        switch (fault)
        {
            case LampFault.Dead: return 0f;
            case LampFault.Flicker: return FlickerOn(time, seed) ? 1f : 0f;
            default: return 1f;
        }
    }

    // 켜져 있던 깜빡이는 가로등이 꺼지는 순간이고 쿨다운이 지났으면 합선 불꽃을 튀긴다.
    public static bool SparkStarts(bool wasLit, bool isLit, float now, float lastSparkTime)
    {
        return wasLit && !isLit && now - lastSparkTime >= SparkCooldownSeconds;
    }

    private static bool FlickerOn(float time, int seed)
    {
        // 램프마다 주기가 어긋나게 시드로 시작점을 민다.
        float shifted = time + Hash(seed, -1) % 1000 / 1000f * FlickerCycleSeconds;
        int cycle = (int)Math.Floor(shifted / FlickerCycleSeconds);
        float inCycle = shifted - cycle * FlickerCycleSeconds;
        // 짝수 주기는 깜빡인 뒤 켜진 채로, 홀수 주기는 꺼진 채로 쉰다.
        bool restLit = (cycle & 1) == 0;
        if (inCycle >= FlickerBurstSeconds) return restLit;

        // 깜빡이는 구간: 짧은 단계마다 켜짐/꺼짐을 섞고, 마지막 두 단계는 이어질 상태로 끝낸다.
        int steps = (int)(FlickerBurstSeconds / FlickerStepSeconds);
        int step = (int)(inCycle / FlickerStepSeconds);
        if (step >= steps - 2) return restLit;
        return Hash(seed, cycle * 64 + step) % 100 < 40;
    }

    // 간단한 정수 해시다(음수가 나오지 않게 비트를 자른다).
    private static int Hash(int a, int b)
    {
        unchecked
        {
            uint h = (uint)a * 374761393u + (uint)b * 668265263u;
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (int)(h & 0x7fffffff);
        }
    }

    // 보너스 드랍을 받을지 정한다. roll은 0~1 난수이고, 이미 가졌으면 제외하는 옵션이면 굴리지 않는다.
    public static bool BonusDrops(float chance, float roll, bool alreadyOwned, bool skipIfOwned)
    {
        if (chance <= 0f) return false;
        if (skipIfOwned && alreadyOwned) return false;
        return roll < chance;
    }
}
