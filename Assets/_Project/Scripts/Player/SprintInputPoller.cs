// 달리기(Shift) 입력을 매 프레임 액션 상태에서 직접 읽어 StarterAssets 입력에 넣어 주는 어댑터다. SendMessages 모드는 버튼을 뗄 때 메시지를 주지 않아 한 번 누르면 계속 달리는 문제가 생길 수 있어서 쓴다.
using UnityEngine;
using UnityEngine.InputSystem;

// 허약 상태 제한(SurvivalMovementLimiter, -50)이 이 값을 지울 수 있도록 그보다 먼저 실행한다.
[DefaultExecutionOrder(-60)]
public class SprintInputPoller : MonoBehaviour
{
    private StarterAssets.StarterAssetsInputs inputs;
    private InputAction sprintAction;
    private SurvivalStats survival;

    private void Awake()
    {
        inputs = GetComponent<StarterAssets.StarterAssetsInputs>();
        survival = GetComponent<SurvivalStats>();
        PlayerInput playerInput = GetComponent<PlayerInput>();
        if (playerInput != null && playerInput.actions != null) sprintAction = playerInput.actions.FindAction("Sprint");
    }

    private void Update()
    {
        if (inputs == null || sprintAction == null) return;
        // 패널이 열린 동안에는 달리기를 끈다(마우스로 메뉴를 쓰는 중).
        inputs.SprintInput(sprintAction.IsPressed() && !HudController.AnyPanelOpen);
        // 실제로 달리는 중(입력을 누르고 이동 입력이 있음)에는 허기/갈증이 더 빨리 준다. 허약 제한이 달리기를 지웠다면 적용되지 않는다.
        if (survival != null)
            survival.DrainMultiplier = inputs.sprint && inputs.move.sqrMagnitude > 0.01f && survival.Config != null ? survival.Config.SprintDrainMultiplier : 1f;
    }
}
