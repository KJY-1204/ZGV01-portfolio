// 좀비 한 종류가 내는 소리 묶음과 신음 간격을 데이터로 관리하는 에셋이다.
using UnityEngine;

[CreateAssetMenu(fileName = "ZombieAudioSet", menuName = "ZGV01/Zombies/Zombie Audio Set")]
public class ZombieAudioSet : ScriptableObject
{
    [Header("Sounds")]
    [SerializeField] private SoundGroup idle = new SoundGroup();
    [SerializeField] private SoundGroup aggro = new SoundGroup();
    [SerializeField] private SoundGroup attack = new SoundGroup();
    [SerializeField] private SoundGroup hurt = new SoundGroup();
    [SerializeField] private SoundGroup death = new SoundGroup();

    // 신음은 가만히 있을 때는 드물게, 쫓아올 때는 더 자주 낸다(초).
    [Header("Groan Interval")]
    [SerializeField] private Vector2 idleGroanInterval = new Vector2(6f, 12f);
    [SerializeField] private Vector2 chaseGroanInterval = new Vector2(3f, 6f);

    // 연속으로 맞아도 비명이 끊임없이 겹치지 않게 하는 최소 간격(초).
    [SerializeField] private float hurtCooldown = 0.4f;

    public SoundGroup Idle => idle;
    public SoundGroup Aggro => aggro;
    public SoundGroup Attack => attack;
    public SoundGroup Hurt => hurt;
    public SoundGroup Death => death;
    public Vector2 IdleGroanInterval => idleGroanInterval;
    public Vector2 ChaseGroanInterval => chaseGroanInterval;
    public float HurtCooldown => hurtCooldown;
}
