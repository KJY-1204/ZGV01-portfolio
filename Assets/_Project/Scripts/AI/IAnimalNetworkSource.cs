// 초식(AnimalAI)과 맹수(PredatorAI)를 NetworkAnimal이 구분하지 않고 동기화하기 위한 최소한의 계약이다. 서버는 상태와 사건을 내보내고, 참가자 쪽 꼭두각시는 그 값을 받아 모습과 소리만 낸다.
using System;

public interface IAnimalNetworkSource
{
    // 서버: 놀람(경계/추격 시작)과 공격 시작을 알린다.
    event Action Alarmed;
    event Action Attacked;
    // 서버: 애니메이션 상태가 재생될 때(이름, 같은 상태라도 다시 재생할지) 알린다.
    event Action<string, bool> AnimationPlayed;

    // 서버가 동기화하는 현재 상태 값이다(PreyState 또는 PredatorState 정수).
    int StateValue { get; }

    // 참가자 쪽: AI와 길찾기를 끈 꼭두각시가 된다.
    void BecomePuppet();
    bool IsPuppet { get; }
    void RemoteState(int state);
    void RemoteAlarmed();
    void RemoteAttacked();
    void RemoteAnimation(string stateName, bool force);
}
