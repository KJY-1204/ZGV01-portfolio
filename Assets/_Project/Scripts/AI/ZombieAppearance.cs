// 좀비가 생길 때마다 크기와 피부/옷 색조를 무작위로 골라 같은 종류도 서로 달라 보이게 하는 표현 컴포넌트다. 게임 규칙은 바꾸지 않는다.
using System;
using UnityEngine;

public class ZombieAppearance : MonoBehaviour
{
    [Serializable]
    public class Look
    {
        // 원래 머티리얼 색에 곱해지는 색조다(흰색이면 원래 색 그대로).
        public Color body = Color.white;
        public Color clothes = Color.white;
        public float weight = 1f;
    }

    // 모델 크기에 곱하는 배율의 범위다.
    [SerializeField] private Vector2 scaleRange = new Vector2(0.92f, 1.1f);
    [SerializeField] private Look[] looks = Array.Empty<Look>();
    // 몸집이 클수록 목소리가 낮아지는 정도(1이면 크기에 정확히 반비례, 0이면 영향 없음)다.
    [SerializeField, Range(0f, 1f)] private float voicePitchInfluence = 0.6f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    public float Scale { get; private set; } = 1f;
    public int LookIndex { get; private set; } = -1;

    private void Awake()
    {
        Scale = UnityEngine.Random.Range(scaleRange.x, scaleRange.y);
        LookIndex = PickLook();

        ApplyScale();
        ApplyTint();

        ZombieAudio audio = GetComponent<ZombieAudio>();
        if (audio != null) audio.PitchScale = Mathf.Pow(1f / Scale, voicePitchInfluence);
    }

    private int PickLook()
    {
        float total = 0f;
        foreach (Look look in looks) total += Mathf.Max(0f, look.weight);
        if (total <= 0f) return -1;

        float roll = UnityEngine.Random.value * total;
        float cumulative = 0f;
        for (int i = 0; i < looks.Length; i++)
        {
            cumulative += Mathf.Max(0f, looks[i].weight);
            if (roll <= cumulative) return i;
        }
        return looks.Length - 1;
    }

    // 모델만 키우고, 맞는 크기로 충돌 판정(캡슐)도 같은 비율로 키운다. 에이전트/AI는 그대로다.
    private void ApplyScale()
    {
        Transform model = transform.Find("Model");
        if (model != null) model.localScale *= Scale;

        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        if (capsule == null) return;
        capsule.height *= Scale;
        capsule.radius *= Scale;
        capsule.center *= Scale;
    }

    // 머티리얼을 복제하지 않고 렌더러별 속성 블록으로 색조를 입힌다(이름에 Clothes가 들어간 렌더러는 옷, 나머지 스킨드 메시는 몸).
    private void ApplyTint()
    {
        if (LookIndex < 0) return;

        Look look = looks[LookIndex];
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        foreach (SkinnedMeshRenderer renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            Color tint = renderer.name.Contains("Clothes") ? look.clothes : look.body;
            renderer.GetPropertyBlock(block);
            Color baseColor = renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty(BaseColorId)
                ? renderer.sharedMaterial.GetColor(BaseColorId)
                : Color.white;
            block.SetColor(BaseColorId, baseColor * tint);
            renderer.SetPropertyBlock(block);
        }
    }
}
