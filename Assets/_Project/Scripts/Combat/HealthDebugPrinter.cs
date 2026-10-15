// Health 이벤트를 콘솔에 출력하는 테스트 씬 전용 디버그 표현 컴포넌트다.
using UnityEngine;

[RequireComponent(typeof(Health))]
public class HealthDebugPrinter : MonoBehaviour
{
    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
        health.Damaged += OnDamaged;
        health.Died += OnDied;
    }

    private void OnDestroy()
    {
        health.Damaged -= OnDamaged;
        health.Died -= OnDied;
    }

    private void OnDamaged(float amount, float current)
    {
        Debug.Log($"{name} took {amount} damage, {current}/{health.MaxHealth} HP left");
    }

    private void OnDied()
    {
        Debug.Log($"{name} died");
    }
}
