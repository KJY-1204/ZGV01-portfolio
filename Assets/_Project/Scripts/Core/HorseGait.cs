// 말을 탄 플레이어의 이동 속도와 공중 시간으로 말의 걸음걸이(정지/걷기/질주/점프)를 고르는 순수 규칙이다. 유니티 컴포넌트에 의존하지 않는다.
public enum HorseGait
{
    Idle,
    Walk,
    Gallop,
    Jump,
}

public static class HorseGaitRule
{
    // 이 속도(m/s)보다 느리면 서 있는 것으로 본다.
    public const float StandSpeed = 0.4f;
    // 공중에 이 시간(초) 이상 떠 있어야 점프 동작으로 본다(비탈에서 잠깐 뜨는 것은 무시한다).
    public const float AirSeconds = 0.2f;

    // 걷는 속도와 질주 속도의 중간(보다 약간 위)을 넘으면 질주로 본다.
    public static HorseGait Choose(float horizontalSpeed, float airSeconds, float walkSpeed, float gallopSpeed)
    {
        if (airSeconds >= AirSeconds) return HorseGait.Jump;
        if (horizontalSpeed < StandSpeed) return HorseGait.Idle;

        // 질주 속도가 걷기 속도보다 빠르지 않게 설정돼 있으면 질주 동작은 없다.
        if (gallopSpeed <= walkSpeed) return HorseGait.Walk;
        float gallopThreshold = walkSpeed + (gallopSpeed - walkSpeed) * 0.4f;
        return horizontalSpeed >= gallopThreshold ? HorseGait.Gallop : HorseGait.Walk;
    }
}
