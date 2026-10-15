// 불꽃 조명이 일렁이는 정도(배율)를 구하는 도우미다. 같은 시드에는 시간에 따라 부드럽게 변하는 값을 돌려준다.
using UnityEngine;

public static class FlameFlicker
{
    // 1을 중심으로 +-amount만큼 흔들리는 배율이다.
    public static float Factor(float seed, float amount, float speed)
    {
        float noise = Mathf.PerlinNoise(Time.time * speed, seed * 17.31f) * 2f - 1f;
        return 1f + noise * amount;
    }
}
