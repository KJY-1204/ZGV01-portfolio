// 대상 몸의 한 부위(머리 등)를 나타내고 그곳에 맞았을 때의 피해 배율을 정하는 표식이다.
using UnityEngine;

public class HitZone : MonoBehaviour
{
    [SerializeField] private float multiplier = 2f;
    [SerializeField] private bool head = true;

    public float Multiplier => multiplier;
    public bool IsHead => head;
}
