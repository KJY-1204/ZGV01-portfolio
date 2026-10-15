// 무기와 상관없이 공통으로 쓰는 효과음(장착, 빈 탄창 클릭, 펀치, 줍기)을 데이터로 관리하는 에셋이다. Resources에서 한 번 불러 쓴다.
using UnityEngine;

[CreateAssetMenu(fileName = "CombatAudioLibrary", menuName = "ZGV01/Combat/Combat Audio Library")]
public class CombatAudioLibrary : ScriptableObject
{
    private const string ResourcePath = "CombatAudioLibrary";

    [SerializeField] private AudioClip equip;
    [SerializeField] private AudioClip dryFire;
    [SerializeField] private AudioClip punchSwing;
    [SerializeField] private AudioClip[] punchHits = new AudioClip[0];
    [SerializeField] private AudioClip pickup;

    private static CombatAudioLibrary instance;

    public static CombatAudioLibrary Instance
    {
        get
        {
            if (instance == null) instance = Resources.Load<CombatAudioLibrary>(ResourcePath);
            return instance;
        }
    }

    public AudioClip Equip => equip;
    public AudioClip DryFire => dryFire;
    public AudioClip PunchSwing => punchSwing;
    public AudioClip[] PunchHits => punchHits;
    public AudioClip Pickup => pickup;
}
