// 건축물의 남은 체력에 따라 모델을 점점 어둡고 칙칙하게 칠하는 표현 컴포넌트다(재질 복제 없이 MaterialPropertyBlock을 쓴다).
using UnityEngine;

[RequireComponent(typeof(BuildableHealth))]
public class BuildableDamageVisual : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    // 체력이 0에 가까울 때 원래 색에 곱하는 색이다(그을리고 깨진 듯한 갈색 기운이 도는 어둠).
    private static readonly Color WornColor = new Color(0.34f, 0.3f, 0.28f, 1f);

    private struct Slot
    {
        public Renderer renderer;
        public int materialIndex;
        public Color baseColor;
    }

    private BuildableHealth health;
    private Slot[] slots;
    private MaterialPropertyBlock block;

    private void Awake()
    {
        health = GetComponent<BuildableHealth>();
        block = new MaterialPropertyBlock();

        System.Collections.Generic.List<Slot> list = new System.Collections.Generic.List<Slot>();
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            if (renderer is ParticleSystemRenderer) continue;
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                Color baseColor = material != null && material.HasProperty(BaseColorId) ? material.GetColor(BaseColorId) : Color.white;
                list.Add(new Slot { renderer = renderer, materialIndex = i, baseColor = baseColor });
            }
        }
        slots = list.ToArray();
    }

    private void OnEnable()
    {
        health.Changed += Refresh;
        Refresh();
    }

    private void OnDisable() => health.Changed -= Refresh;

    private void Refresh()
    {
        if (health.Max <= 0f) return;

        float damage = 1f - Mathf.Clamp01(health.Current / health.Max);
        Color tint = Color.Lerp(Color.white, WornColor, damage);
        foreach (Slot slot in slots)
        {
            if (slot.renderer == null) continue;
            slot.renderer.GetPropertyBlock(block, slot.materialIndex);
            block.SetColor(BaseColorId, slot.baseColor * tint);
            slot.renderer.SetPropertyBlock(block, slot.materialIndex);
        }
    }
}
