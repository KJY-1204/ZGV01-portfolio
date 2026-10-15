// 메인 메뉴 3D 배경의 불빛(모닥불, 가로등)을 불규칙하게 깜빡이게 하는 컴포넌트다.
using UnityEngine;

[RequireComponent(typeof(Light))]
public class MenuLightFlicker : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float amount = 0.25f;
    [SerializeField] private float speed = 7f;

    private Light lamp;
    private float baseIntensity;
    private float seed;

    private void Awake()
    {
        lamp = GetComponent<Light>();
        baseIntensity = lamp.intensity;
        seed = Random.value * 100f;
    }

    private void Update()
    {
        float noise = Mathf.PerlinNoise(seed, Time.unscaledTime * speed);
        lamp.intensity = baseIntensity * (1f - amount + noise * amount * 1.6f);
    }
}
