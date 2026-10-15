// 시각에 따라 서서히 켜지고 꺼지는 조명 컴포넌트다(가로등, 설치형 횃불/램프). 조명 밝기를 바꾸고, 발광 유리가 있으면 켜진/꺼진 재질을 바꾼다.
using UnityEngine;

public class NightLight : MonoBehaviour
{
    [SerializeField] private Light[] lights = System.Array.Empty<Light>();
    // 불꽃처럼 일렁이게 할 때의 흔들림 정도(0이면 고정)다.
    [SerializeField, Range(0f, 0.4f)] private float flicker = 0f;
    [SerializeField] private float flickerSpeed = 6f;
    // 켜지면 발광 재질로 바꿀 유리 렌더러와 그 재질 칸 번호다(없으면 비워 둔다).
    [SerializeField] private Renderer glass;
    [SerializeField] private int glassMaterialIndex;
    [SerializeField] private Material onMaterial;
    [SerializeField] private Material offMaterial;
    // 폐허 연출용 고장 상태다(꺼짐/깜빡임). 맵 배치 단계가 정한다.
    [SerializeField] private LampFault fault = LampFault.None;
    [SerializeField] private int faultSeed;
    // 참이면 시각과 상관없이 항상 켜진다(실내 조명). 고장 상태는 그대로 적용된다.
    [SerializeField] private bool alwaysOn;

    // 고장 상태(디버그 도구가 읽는다).
    public LampFault Fault => fault;
    public bool AlwaysOn => alwaysOn;

    private float[] baseIntensity;
    private bool glassLit;
    private float seed;
    private bool faultLit = true;
    private float lastSparkTime = -99f;

    private void Awake()
    {
        baseIntensity = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++) baseIntensity[i] = lights[i] != null ? lights[i].intensity : 0f;
        seed = Random.value * 100f;
        Apply(CurrentLevel());
    }

    private float CurrentLevel()
    {
        if (alwaysOn) return 1f;
        // 시계가 없는 씬(테스트 바닥)에서는 꺼 둔다.
        return GameClock.Current != null ? NightLightSchedule.Intensity(GameClock.Current.Hour) : 0f;
    }

    private void Update() => Apply(CurrentLevel());

    private void Apply(float level)
    {
        if (fault != LampFault.None)
        {
            bool nightOn = level > 0.01f;
            level *= LampFaultPattern.Multiplier(fault, Time.time, faultSeed);
            if (fault == LampFault.Flicker && nightOn)
            {
                bool nowLit = level > 0.01f;
                if (LampFaultPattern.SparkStarts(faultLit, nowLit, Time.time, lastSparkTime))
                {
                    lastSparkTime = Time.time;
                    LampSparkFx.Play(lights.Length > 0 && lights[0] != null ? lights[0].transform.position : transform.position);
                }
                faultLit = nowLit;
            }
        }
        bool lit = level > 0.01f;
        float factor = flicker > 0f && lit ? FlameFlicker.Factor(seed, flicker, flickerSpeed) : 1f;
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] == null) continue;
            lights[i].enabled = lit;
            lights[i].intensity = baseIntensity[i] * level * factor;
        }

        if (glass != null && lit != glassLit)
        {
            glassLit = lit;
            Material[] materials = glass.sharedMaterials;
            if (glassMaterialIndex >= 0 && glassMaterialIndex < materials.Length)
            {
                Material target = lit ? onMaterial : offMaterial;
                if (target != null) materials[glassMaterialIndex] = target;
                glass.sharedMaterials = materials;
            }
        }
    }
}
