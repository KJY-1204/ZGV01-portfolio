// 총소리, 발소리, 건축 소리처럼 좀비가 들을 수 있는 소음을 알리는 정적 중개자다. 발생 쪽은 누가 듣는지 모르고, 좀비가 구독해서 반응한다.
using System;
using UnityEngine;

public static class NoiseSystem
{
    // (소리가 난 자리, 들리는 반경(m), 낸 쪽). 낸 쪽은 자기 소리를 자기가 듣지 않게 거를 때 쓴다.
    public static event Action<Vector3, float, object> Emitted;

    public static void Emit(Vector3 position, float radius, object source = null)
    {
        if (radius <= 0f) return;
        Emitted?.Invoke(position, radius, source);
    }
}
