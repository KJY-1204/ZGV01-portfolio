// 무기를 들거나 조준할 때, 건물 안에서는 1인칭으로 Cinemachine 3인칭 카메라의 어깨 오프셋/거리/시야각을 부드럽게 바꾸는 컴포넌트다.
using Unity.Cinemachine;
using UnityEngine;

public class CombatCameraController : MonoBehaviour
{
    [System.Serializable]
    private struct CameraPose
    {
        public Vector3 shoulderOffset;
        public float distance;
        public float fieldOfView;
    }

    // 총이 조준선과 거의 겹쳐 몸에 가려지므로 무기를 들면 카메라를 오른쪽으로 더 벌린다.
    [SerializeField] private CameraPose armed = new CameraPose { shoulderOffset = new Vector3(0.85f, 0.3f, 0f), distance = 3.4f, fieldOfView = 40f };
    [SerializeField] private CameraPose aiming = new CameraPose { shoulderOffset = new Vector3(0.9f, 0.25f, 0f), distance = 2.5f, fieldOfView = 34f };
    // 건물 안(IndoorView)에서는 카메라를 머리 위치(거리 0)에 둔다.
    [SerializeField] private CameraPose firstPerson = new CameraPose { shoulderOffset = Vector3.zero, distance = 0f, fieldOfView = 70f };
    [SerializeField] private CameraPose firstPersonAiming = new CameraPose { shoulderOffset = Vector3.zero, distance = 0f, fieldOfView = 52f };
    [SerializeField] private float blendSpeed = 9f;

    private WeaponEquipment equipment;
    private IndoorView indoor;
    private CinemachineCamera cinemachineCamera;
    private CinemachineThirdPersonFollow follow;
    private CameraPose unarmed;

    private void Awake()
    {
        equipment = GetComponent<WeaponEquipment>();
        indoor = GetComponent<IndoorView>();
    }

    private void Start()
    {
        cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();
        if (cinemachineCamera == null) return;
        follow = cinemachineCamera.GetComponent<CinemachineThirdPersonFollow>();
        if (follow == null) return;

        // 맨손일 때는 씬에 설정된 기존 값을 그대로 쓰기 위해 시작 시점의 값을 기억한다.
        unarmed = new CameraPose
        {
            shoulderOffset = follow.ShoulderOffset,
            distance = follow.CameraDistance,
            fieldOfView = cinemachineCamera.Lens.FieldOfView,
        };
    }

    private void LateUpdate()
    {
        if (follow == null || equipment == null) return;

        CameraPose target = equipment.Current == null ? unarmed : (equipment.IsAiming ? aiming : armed);
        if (indoor != null && indoor.IsInside) target = equipment.IsAiming ? firstPersonAiming : firstPerson;
        float t = 1f - Mathf.Exp(-blendSpeed * Time.deltaTime);

        follow.ShoulderOffset = Vector3.Lerp(follow.ShoulderOffset, target.shoulderOffset, t);
        follow.CameraDistance = Mathf.Lerp(follow.CameraDistance, target.distance, t);

        LensSettings lens = cinemachineCamera.Lens;
        // 옵션 창의 시야각 배율을 곱한다.
        lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, target.fieldOfView * GameSettingsStore.FovScale, t);
        cinemachineCamera.Lens = lens;
    }
}
