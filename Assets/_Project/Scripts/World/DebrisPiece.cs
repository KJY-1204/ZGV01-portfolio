// 파편 조각 하나의 수명을 관리한다. 일정 시간 뒤 작아지며 사라진다.
using UnityEngine;

public class DebrisPiece : MonoBehaviour
{
    private const float Lifetime = 3f;
    private const float ShrinkTime = 0.6f;

    private float age;
    private Vector3 startScale;

    private void Start() => startScale = transform.localScale;

    private void Update()
    {
        age += Time.deltaTime;
        if (age >= Lifetime + ShrinkTime)
        {
            Destroy(gameObject);
            return;
        }
        if (age > Lifetime) transform.localScale = startScale * (1f - (age - Lifetime) / ShrinkTime);
    }
}
