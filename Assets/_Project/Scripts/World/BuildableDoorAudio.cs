// 문이 열리고 닫힐 때 그 자리에서 소리를 내는 표현 컴포넌트다. 문의 상태 로직은 BuildableDoor가 맡는다.
using UnityEngine;

[RequireComponent(typeof(BuildableDoor))]
public class BuildableDoorAudio : MonoBehaviour
{
    [SerializeField] private SoundGroup openSound = new SoundGroup();
    [SerializeField] private SoundGroup closeSound = new SoundGroup();

    private BuildableDoor door;

    private void Awake() => door = GetComponent<BuildableDoor>();

    private void OnEnable()
    {
        door.Opened += OnOpened;
        door.Closed += OnClosed;
    }

    private void OnDisable()
    {
        door.Opened -= OnOpened;
        door.Closed -= OnClosed;
    }

    private void OnOpened() => Play(openSound);
    private void OnClosed() => Play(closeSound);

    private void Play(SoundGroup group)
    {
        AudioClip clip = group.PickClip();
        if (clip == null) return;
        EffectPool.PlaySoundAt(clip, transform.position + Vector3.up, group.volume, group.PickPitch());
    }
}
