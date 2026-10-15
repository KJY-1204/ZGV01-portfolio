// 자원 노드의 채집/소멸/재생을 소리와 이펙트로 알리는 표현 컴포넌트다. 채집 소리, 소멸 소리(부서지는 소리), 연기, 재생될 때 커지며 나타나는 연출을 낸다. 노드 상태는 바꾸지 않는다.
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(ResourceNode))]
public class ResourceNodeFeedback : MonoBehaviour
{
    // 재생될 때 이 비율의 크기에서 시작해 원래 크기로 커진다.
    private const float PopStartScale = 0.6f;
    private const float PopSeconds = 0.35f;

    private ResourceNode node;
    private Vector3 baseScale;
    private Coroutine pop;

    private void Awake()
    {
        node = GetComponent<ResourceNode>();
        node.Gathered += OnGathered;
        node.Depleted += OnDepleted;
        node.Respawned += OnRespawned;
    }

    private void OnDestroy()
    {
        if (node == null) return;
        node.Gathered -= OnGathered;
        node.Depleted -= OnDepleted;
        node.Respawned -= OnRespawned;
    }

    // 갑자기 나타나지 않고 작은 크기에서 살짝 넘쳤다가 자리를 잡는다.
    private void OnRespawned()
    {
        // 생성 직후 스포너가 크기를 바꿀 수 있어서 Awake가 아니라 재생 시점의 크기를 기준으로 삼는다(연출 도중 다시 재생되면 처음 기준을 유지한다).
        if (pop != null) StopCoroutine(pop);
        else baseScale = transform.localScale;
        pop = StartCoroutine(PopIn());
    }

    private IEnumerator PopIn()
    {
        for (float t = 0f; t < PopSeconds; t += Time.deltaTime)
        {
            float u = t / PopSeconds;
            // easeOutBack: 1을 조금 넘었다가 돌아온다.
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float eased = 1f + c3 * Mathf.Pow(u - 1f, 3f) + c1 * Mathf.Pow(u - 1f, 2f);
            transform.localScale = baseScale * Mathf.LerpUnclamped(PopStartScale, 1f, eased);
            yield return null;
        }
        transform.localScale = baseScale;
        pop = null;
    }

    private void OnGathered(int amount)
    {
        ResourceNodeDefinition definition = node.Definition;
        if (definition == null) return;

        SoundGroup sounds = definition.GatherSounds;
        AudioClip clip = sounds.PickClip();
        if (clip == null) return;

        EffectPool.PlaySoundAt(clip, transform.position, sounds.volume, sounds.PickPitch());
    }

    // 사라지는 순간 부서지는 소리(클립을 모두 겹쳐서)와 연기를 낸다.
    private void OnDepleted()
    {
        ResourceNodeDefinition definition = node.Definition;
        if (definition == null) return;

        Vector3 center = VisualCenter();

        SoundGroup sounds = definition.DepleteSounds;
        if (sounds.HasClips)
        {
            foreach (AudioClip clip in sounds.clips)
                if (clip != null) EffectPool.PlaySoundAt(clip, center, sounds.volume, sounds.PickPitch());
        }

        if (definition.DepleteEffect != null) EffectPool.Spawn(definition.DepleteEffect, center, Quaternion.identity);
    }

    // 눈에 보이는 부분의 중심(연기가 피어오를 위치)이다. 렌더러가 없으면 노드 위치를 쓴다.
    private Vector3 VisualCenter()
    {
        bool any = false;
        Bounds bounds = new Bounds();
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            if (renderer is ParticleSystemRenderer) continue;
            if (!any) { bounds = renderer.bounds; any = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        return any ? new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * 0.4f, bounds.center.z) : transform.position;
    }
}
