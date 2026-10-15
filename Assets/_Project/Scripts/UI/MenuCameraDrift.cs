// 메인 메뉴 3D 배경의 카메라를 천천히 앞뒤로 밀고 좌우로 흔들어 정지 화면처럼 보이지 않게 하는 컴포넌트다.
using UnityEngine;

public class MenuCameraDrift : MonoBehaviour
{
    [SerializeField] private Vector3 sway = new Vector3(0.7f, 0.12f, 0.9f);
    [SerializeField] private float speed = 0.07f;
    [SerializeField] private float rotationSwayDegrees = 1.6f;

    private Vector3 startPosition;
    private Quaternion startRotation;

    private void Awake()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    private void Update()
    {
        float t = Time.unscaledTime * speed;
        Vector3 offset = new Vector3(
            (Mathf.PerlinNoise(t, 0.3f) - 0.5f) * 2f * sway.x,
            (Mathf.PerlinNoise(t, 7.1f) - 0.5f) * 2f * sway.y,
            (Mathf.PerlinNoise(t, 13.7f) - 0.5f) * 2f * sway.z);
        float yaw = (Mathf.PerlinNoise(t, 21.3f) - 0.5f) * 2f * rotationSwayDegrees;
        float pitch = (Mathf.PerlinNoise(t, 33.9f) - 0.5f) * rotationSwayDegrees;

        transform.position = startPosition + startRotation * offset;
        transform.rotation = startRotation * Quaternion.Euler(pitch, yaw, 0f);
    }
}
