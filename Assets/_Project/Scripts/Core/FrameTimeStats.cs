// 프레임 시간(밀리초) 표본의 평균과 상위 1% 최악 프레임 평균을 계산하는 성능 측정용 순수 규칙이다. 유니티 컴포넌트에 의존하지 않는다.
using System;
using System.Collections.Generic;

public static class FrameTimeStats
{
    // 표본의 산술 평균이다. 표본이 없으면 0이다.
    public static float Average(IReadOnlyList<float> samples)
    {
        if (samples == null || samples.Count == 0) return 0f;
        double sum = 0.0;
        for (int i = 0; i < samples.Count; i++) sum += samples[i];
        return (float)(sum / samples.Count);
    }

    // 가장 느린 1%(최소 1개) 프레임의 평균이다. 끊김을 평균이 가리는 것을 막는다.
    public static float WorstOnePercentAverage(IReadOnlyList<float> samples)
    {
        if (samples == null || samples.Count == 0) return 0f;
        float[] sorted = new float[samples.Count];
        for (int i = 0; i < sorted.Length; i++) sorted[i] = samples[i];
        Array.Sort(sorted);

        int count = Math.Max(1, sorted.Length / 100);
        double sum = 0.0;
        for (int i = sorted.Length - count; i < sorted.Length; i++) sum += sorted[i];
        return (float)(sum / count);
    }

    // 평균 프레임 시간(밀리초)에 해당하는 초당 프레임 수다.
    public static float ToFps(float frameMilliseconds)
    {
        return frameMilliseconds > 0f ? 1000f / frameMilliseconds : 0f;
    }
}
