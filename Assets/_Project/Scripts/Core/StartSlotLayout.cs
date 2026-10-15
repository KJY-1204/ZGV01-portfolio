// 멀티플레이 접속자마다 시작 위치를 나누는 규칙이다. 번호 0은 호스트(시작 위치 그대로)이고, 1번부터 시작 위치 둘레의 고리에 한 칸씩 놓는다. 한 고리에 8명이 서고 넘치면 바깥 고리를 쓴다. 유니티 컴포넌트에 의존하지 않는다.
using System;

public static class StartSlotLayout
{
    // 첫 고리의 반지름(m)과 고리 사이 간격, 한 고리에 서는 인원이다. 이웃한 칸의 거리가 몸 충돌체 지름(0.56m)보다 충분히 크다.
    public const float FirstRadius = 2f;
    public const float RingSpacing = 1.8f;
    public const int SlotsPerRing = 8;

    // 번호(0 이상)의 시작 위치 오프셋(m)이다. 가로(x)와 세로(z)만 있고 높이는 호출하는 쪽이 지면에 맞춘다.
    public static void Offset(int slot, out float x, out float z)
    {
        if (slot <= 0)
        {
            x = 0f;
            z = 0f;
            return;
        }

        int index = slot - 1;
        int ring = index / SlotsPerRing;
        int position = index % SlotsPerRing;
        float radius = FirstRadius + ring * RingSpacing;
        // 바깥 고리는 반 칸 비켜서 안쪽 고리의 칸과 일렬로 서지 않게 한다.
        float angle = (position + (ring % 2) * 0.5f) * (2f * (float)Math.PI / SlotsPerRing);
        x = (float)Math.Cos(angle) * radius;
        z = (float)Math.Sin(angle) * radius;
    }

    // used에 없는 가장 작은 양수 번호다(번호 0은 호스트라 쓰지 않는다).
    public static int NextFree(System.Collections.Generic.ICollection<int> used)
    {
        int slot = 1;
        while (used != null && used.Contains(slot)) slot++;
        return slot;
    }
}
