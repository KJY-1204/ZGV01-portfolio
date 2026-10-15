// 좀비 셰이더(ZGV01/ZombieLit)의 피격 플래시, 사망 디졸브, 감염 맥동 값을 렌더러 속성 블록으로 구동하는 표현 컴포넌트다. 게임 규칙은 바꾸지 않는다.
using UnityEngine;

[RequireComponent(typeof(Health))]
public class ZombieShaderFX : MonoBehaviour
{
    // 피격 플래시가 가라앉기까지의 시간(초)과 세기(0~1)다. 머리 피격은 더 강하다.
    [SerializeField] private float flashSeconds = 0.22f;
    [SerializeField, Range(0f, 1f)] private float flashStrength = 0.7f;
    [SerializeField, Range(0f, 1f)] private float headshotFlashStrength = 1f;
    [SerializeField] private Color flashColor = new Color(1f, 0.12f, 0.08f, 1f);
    // 불타는 동안 깜빡이는 색이다.
    [SerializeField] private Color burnColor = new Color(1f, 0.45f, 0.08f, 1f);

    // 죽은 뒤 이 시간(초)이 지나면 재처럼 사라지기 시작해 dissolveSeconds 동안 사라진다. 불로 죽으면 바로 시작해 burnDissolveSeconds 동안 사라진다.
    [SerializeField] private float dissolveDelay = 8f;
    [SerializeField] private float dissolveSeconds = 4f;
    [SerializeField] private float burnDissolveDelay = 0.4f;
    [SerializeField] private float burnDissolveSeconds = 2.5f;

    // 발광 맵(눈, 핏줄)이 있는 렌더러에만 건다. 박동 속도는 좀비마다 조금씩 다르다.
    [SerializeField, Range(0f, 1f)] private float pulseAmount = 0.9f;
    [SerializeField] private Vector2 pulseSpeedRange = new Vector2(1.8f, 2.8f);

    private static readonly int HitFlashId = Shader.PropertyToID("_HitFlash");
    private static readonly int HitColorId = Shader.PropertyToID("_HitColor");
    private static readonly int DissolveId = Shader.PropertyToID("_DissolveAmount");
    private static readonly int PulseAmountId = Shader.PropertyToID("_PulseAmount");
    private static readonly int PulseSpeedId = Shader.PropertyToID("_PulseSpeed");
    private static readonly int PulsePhaseId = Shader.PropertyToID("_PulsePhase");
    private static readonly int EmissionMapId = Shader.PropertyToID("_EmissionMap");

    private Health health;
    private ZombieSunBurn sunBurn;
    private Renderer[] renderers;
    private bool[] hasEmissionMap;
    private MaterialPropertyBlock block;
    private float flash;
    private bool burnedToDeath;
    private float deathTime = -1f;
    private float lastDissolve = -1f;
    private float lastFlashValue = -1f;
    private Color lastFlashColor;

    private void Awake()
    {
        health = GetComponent<Health>();
        sunBurn = GetComponent<ZombieSunBurn>();
        block = new MaterialPropertyBlock();

        var list = new System.Collections.Generic.List<Renderer>();
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            if (renderer is ParticleSystemRenderer || renderer is TrailRenderer) continue;
            if (renderer.sharedMaterial == null || !renderer.sharedMaterial.HasProperty(HitFlashId)) continue;
            list.Add(renderer);
        }
        renderers = list.ToArray();

        hasEmissionMap = new bool[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            hasEmissionMap[i] = renderers[i].sharedMaterial.HasProperty(EmissionMapId) && renderers[i].sharedMaterial.GetTexture(EmissionMapId) != null;

        health.Damaged += OnDamaged;
        health.Died += OnDied;
    }

    private void Start()
    {
        float speed = Random.Range(pulseSpeedRange.x, pulseSpeedRange.y);
        float phase = Random.Range(0f, Mathf.PI * 2f);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].GetPropertyBlock(block);
            block.SetFloat(PulseAmountId, hasEmissionMap[i] ? pulseAmount : 0f);
            block.SetFloat(PulseSpeedId, speed);
            block.SetFloat(PulsePhaseId, phase);
            renderers[i].SetPropertyBlock(block);
        }
    }

    private void OnDestroy()
    {
        if (health == null) return;
        health.Damaged -= OnDamaged;
        health.Died -= OnDied;
    }

    private void OnDamaged(float amount, float remaining)
    {
        // 불타는 동안의 틱 피해는 아래 깜빡임이 맡으므로 플래시를 겹치지 않는다.
        if (sunBurn != null && sunBurn.IsBurning) return;
        flash = health.LastHitWasHeadshot ? headshotFlashStrength : flashStrength;
    }

    private void OnDied()
    {
        deathTime = Time.time;
        burnedToDeath = sunBurn != null && sunBurn.IsBurning;
        flash = 0f;
        SetFlash(0f, flashColor);
        // 죽으면 맥동을 멈춘다.
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].GetPropertyBlock(block);
            block.SetFloat(PulseAmountId, 0f);
            renderers[i].SetPropertyBlock(block);
        }
    }

    private void Update()
    {
        if (health.IsDead) UpdateDissolve();
        else UpdateFlash();
    }

    private void UpdateFlash()
    {
        float value;
        Color color = flashColor;
        if (sunBurn != null && sunBurn.IsBurning)
        {
            // 불타는 동안은 주황색으로 일렁인다.
            value = 0.25f + 0.2f * Mathf.Sin(Time.time * 18f);
            color = burnColor;
            flash = 0f;
        }
        else
        {
            flash = Mathf.Max(0f, flash - Time.deltaTime * Mathf.Max(flashStrength, headshotFlashStrength) / Mathf.Max(0.01f, flashSeconds));
            value = flash;
        }

        if (Mathf.Approximately(value, lastFlashValue) && color == lastFlashColor) return;
        SetFlash(value, color);
    }

    private void SetFlash(float value, Color color)
    {
        lastFlashValue = value;
        lastFlashColor = color;
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].GetPropertyBlock(block);
            block.SetFloat(HitFlashId, value);
            block.SetColor(HitColorId, color);
            renderers[i].SetPropertyBlock(block);
        }
    }

    private void UpdateDissolve()
    {
        float delay = burnedToDeath ? burnDissolveDelay : dissolveDelay;
        float seconds = burnedToDeath ? burnDissolveSeconds : dissolveSeconds;
        float t = Mathf.Clamp01((Time.time - deathTime - delay) / Mathf.Max(0.1f, seconds));
        if (Mathf.Approximately(t, lastDissolve)) return;
        lastDissolve = t;
        // 렌더러의 기존 속성 블록(ZombieAppearance의 색조 등)을 읽어 값만 바꿔 다시 넣는다.
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].GetPropertyBlock(block);
            block.SetFloat(DissolveId, t);
            renderers[i].SetPropertyBlock(block);
        }
    }
}
