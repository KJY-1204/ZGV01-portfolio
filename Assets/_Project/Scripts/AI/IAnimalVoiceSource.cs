// 동물의 소리 컴포넌트가 초식(AnimalAI)과 맹수(PredatorAI)를 구분하지 않고 쓰는 최소한의 행동 정보다.
using System;

public enum AnimalMotion
{
    None,
    Walk,
    Run,
}

public interface IAnimalVoiceSource
{
    // 놀라 도망치기 시작하거나(초식) 추격을 시작할 때(맹수) 알린다.
    event Action Alarmed;
    // 공격이 시작될 때 알린다(공격하지 않는 동물은 알리지 않는다).
    event Action Attacked;
    // 지금 걷는지 달리는지다. 발소리의 보폭을 고른다.
    AnimalMotion Motion { get; }
    // 평소 울음을 내도 되는 상태인지다(도망, 추격, 공격 중에는 아니다).
    bool CanCallIdly { get; }
}
