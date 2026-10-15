// 총기 한 종류의 정적 정의다. 모델 정렬, 발사 수치, 손 위치, 반동 값을 데이터로 관리한다.
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponDefinition", menuName = "ZGV01/Combat/Weapon Definition")]
public class WeaponDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string weaponId;
    [SerializeField] private string displayName;
    // 이 무기를 인벤토리에서 나타내는 아이템이다. 이 아이템을 갖고 있어야 장착할 수 있다.
    [SerializeField] private ItemDefinition item;

    [Header("Model")]
    [SerializeField] private GameObject modelPrefab;
    // 모델을 "총구가 +Z, 위쪽이 +Y"인 공통 좌표로 돌리는 오일러 각이다. 모델마다 총구 방향이 달라서 필요하다.
    [SerializeField] private Vector3 modelEuler;
    [SerializeField] private Vector3 modelOffset;
    // 모델 프리팹의 재질이 텍스처 없는 기본 재질일 때 대신 입힐 재질이다. 비워 두면 프리팹의 재질을 그대로 쓴다.
    [SerializeField] private Material modelMaterial;

    [Header("Effects")]
    [SerializeField] private GameObject muzzleFlashPrefab;
    [SerializeField] private float muzzleFlashScale = 1f;

    [Header("Audio")]
    // 발사음 변형들(사격마다 무작위로 하나). 재장전음은 탄창을 갈아 끼우는 순간에 한 번 난다.
    [SerializeField] private AudioClip[] fireSounds = new AudioClip[0];
    [SerializeField, Range(0f, 1f)] private float fireVolume = 1f;
    [SerializeField] private AudioClip reloadSound;

    [Header("Fire")]
    [SerializeField] private float damage = 20f;
    [SerializeField] private float range = 100f;
    [SerializeField] private float fireInterval = 0.12f;
    // 한 발 쏠 때 좀비가 들을 수 있는 반경(m)이다.
    [SerializeField] private float noiseRadius = 30f;
    [SerializeField] private bool automatic = true;

    [Header("Ammo")]
    [SerializeField] private ItemDefinition ammoItem;
    [SerializeField] private int magazineSize = 30;
    [SerializeField] private float reloadTime = 2f;

    // 아래 좌표는 모두 정렬이 끝난 총 좌표(+Z 총구, +Y 위, +X 오른쪽)에서의 위치다.
    [Header("Hands & Muzzle")]
    [SerializeField] private Vector3 rightGrip;
    [SerializeField] private Vector3 leftGrip;
    // 총 길이가 달라서 개머리판이 어깨에 닿게 하려면 총을 총구 방향으로 얼마나 밀지(+) 당길지(-) 정한다.
    [SerializeField] private float holdForward;
    // 재장전 때 왼손이 탄창을 갈아 끼우러 내려가는 위치다.
    [SerializeField] private Vector3 magazinePoint;
    [SerializeField] private Vector3 muzzle;

    [Header("Recoil")]
    [SerializeField] private float recoilBack = 0.05f;
    [SerializeField] private float recoilPitch = 4f;

    public string WeaponId => weaponId;
    public string DisplayName => displayName;
    public ItemDefinition Item => item;
    public GameObject ModelPrefab => modelPrefab;
    public Vector3 ModelEuler => modelEuler;
    public Vector3 ModelOffset => modelOffset;
    public Material ModelMaterial => modelMaterial;
    public GameObject MuzzleFlashPrefab => muzzleFlashPrefab;
    public float MuzzleFlashScale => muzzleFlashScale;
    public AudioClip[] FireSounds => fireSounds;
    public float FireVolume => fireVolume;
    public AudioClip ReloadSound => reloadSound;
    public float Damage => damage;
    public float Range => range;
    public float FireInterval => fireInterval;
    public float NoiseRadius => noiseRadius;
    public bool Automatic => automatic;
    public ItemDefinition AmmoItem => ammoItem;
    public int MagazineSize => magazineSize;
    public float ReloadTime => reloadTime;
    public Vector3 RightGrip => rightGrip;
    public Vector3 LeftGrip => leftGrip;
    public float HoldForward => holdForward;
    public Vector3 MagazinePoint => magazinePoint;
    public Vector3 Muzzle => muzzle;
    public float RecoilBack => recoilBack;
    public float RecoilPitch => recoilPitch;
}
