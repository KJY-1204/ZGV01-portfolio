// 사냥감 동물 한 종류가 내는 소리 묶음과 울음 간격, 발소리 보폭을 데이터로 관리하는 에셋이다.
using UnityEngine;

[CreateAssetMenu(fileName = "AnimalAudioSet", menuName = "ZGV01/Animals/Animal Audio Set")]
public class AnimalAudioSet : ScriptableObject
{
    [Header("Voice")]
    // 평소에 가끔 내는 울음이다.
    [SerializeField] private SoundGroup idle = new SoundGroup();
    // 놀라 도망치기 시작할 때 낸다.
    [SerializeField] private SoundGroup alarm = new SoundGroup();
    [SerializeField] private SoundGroup hurt = new SoundGroup();
    [SerializeField] private SoundGroup death = new SoundGroup();
    // 공격을 시작할 때 내는 소리다(맹수만 쓴다). 맹수는 경계(alarm) 묶음을 추격을 시작하는 포효로 쓴다.
    [SerializeField] private SoundGroup attack = new SoundGroup();

    [Header("Footsteps")]
    [SerializeField] private SoundGroup footstep = new SoundGroup();
    // 이 거리(m)를 움직일 때마다 발소리를 한 번 낸다. 걷기와 달리기의 보폭이 다르다.
    [SerializeField] private float walkStride = 0.9f;
    [SerializeField] private float runStride = 1.5f;

    [Header("Timing")]
    // 대기/배회 중에 평소 울음을 내는 간격(초)이다.
    [SerializeField] private Vector2 idleCallInterval = new Vector2(8f, 18f);
    // 연속으로 맞아도 비명이 겹치지 않게 하는 최소 간격(초)이다.
    [SerializeField] private float hurtCooldown = 0.5f;

    public SoundGroup Idle => idle;
    public SoundGroup Alarm => alarm;
    public SoundGroup Hurt => hurt;
    public SoundGroup Death => death;
    public SoundGroup Attack => attack;
    public SoundGroup Footstep => footstep;
    public float WalkStride => walkStride;
    public float RunStride => runStride;
    public Vector2 IdleCallInterval => idleCallInterval;
    public float HurtCooldown => hurtCooldown;
}
