// 설치된 문짝(경첩 피벗)을 열고 닫는 컴포넌트다. 연 사람의 반대편으로 문이 열리고, 열림 상태와 방향은 저장된다.
using System;
using System.Collections.Generic;
using UnityEngine;

public class BuildableDoor : MonoBehaviour
{
    // 상호작용 안내가 가까운 대상을 빠르게 찾을 수 있게 활성 인스턴스를 모아 둔다.
    private static readonly List<BuildableDoor> active = new List<BuildableDoor>();
    public static IReadOnlyList<BuildableDoor> Active => active;

    // 문이 열리거나 닫히기 시작할 때 알린다. 서버가 접속자에게 문 상태를 전하는 데 쓴다.
    public static event Action<BuildableDoor> StateChanged;

    private void OnEnable() => active.Add(this);
    private void OnDisable() => active.Remove(this);

    [Serializable]
    public class Leaf
    {
        public Transform hinge;
        // 문 앞쪽(로컬 +Z)으로 열 때의 각도(도)다. 반대편으로 열면 부호가 뒤집힌다.
        public float openAngle = 90f;
    }

    [SerializeField] private Leaf[] leaves = Array.Empty<Leaf>();
    [SerializeField] private float swingSpeed = 180f;

    private float side = 1f;
    private float target;
    private float progress;

    public bool IsOpen => target > 0.5f;
    // 문짝이 완전히 닫혀 길을 막고 있는 상태다(열리는 중이거나 닫히는 중이면 false).
    public bool IsFullyClosed => target == 0f && progress <= 0.001f;
    // 열리기 시작할 때와 완전히 닫혔을 때 알린다(소리 같은 표현 컴포넌트가 구독한다).
    public event Action Opened;
    public event Action Closed;

    // 문이 열린 쪽(1 또는 -1)이다. 저장에 쓴다.
    public float Side => side;

    // 불러오기용으로 소리 없이 상태를 바로 맞춘다.
    public void RestoreState(bool open, float openSide)
    {
        side = openSide < 0f ? -1f : 1f;
        target = open ? 1f : 0f;
        progress = target;
        ApplyPose();
    }

    // actor가 서 있는 쪽의 반대편으로 열린다.
    public void Toggle(Vector3 actor)
    {
        if (IsOpen)
        {
            target = 0f;
            StateChanged?.Invoke(this);
            return;
        }

        float facing = Vector3.Dot(transform.forward, actor - transform.position);
        side = facing > 0f ? -1f : 1f;
        target = 1f;
        Opened?.Invoke();
        StateChanged?.Invoke(this);
    }

    // 서버가 알린 열림 상태를 참가자 쪽 복제본에 반영한다(움직임과 소리는 평소와 같다).
    public void ApplyReplicated(bool open, float openSide)
    {
        side = openSide < 0f ? -1f : 1f;
        bool wasOpen = IsOpen;
        target = open ? 1f : 0f;
        if (open && !wasOpen) Opened?.Invoke();
    }

    private void Update()
    {
        if (Mathf.Approximately(progress, target)) return;

        progress = Mathf.MoveTowards(progress, target, swingSpeed / 90f * Time.deltaTime);
        ApplyPose();
        if (Mathf.Approximately(progress, 0f) && target == 0f) Closed?.Invoke();
    }

    private void ApplyPose()
    {
        foreach (Leaf leaf in leaves)
        {
            if (leaf.hinge != null) leaf.hinge.localRotation = Quaternion.AngleAxis(leaf.openAngle * side * progress, Vector3.up);
        }
    }
}
