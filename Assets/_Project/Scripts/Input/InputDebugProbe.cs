// PlayerInput의 Send Messages 콜백을 받아 각 액션 값을 콘솔에 출력하는 Test_Input 씬 전용 디버그 프로브다.
using UnityEngine;
using UnityEngine.InputSystem;

public class InputDebugProbe : MonoBehaviour
{
    private void OnMove(InputValue value) => Debug.Log($"Move: {value.Get<Vector2>()}");
    private void OnLook(InputValue value) => Debug.Log($"Look: {value.Get<Vector2>()}");
    private void OnFire(InputValue value) => Debug.Log($"Fire: {value.isPressed}");
    private void OnAim(InputValue value) => Debug.Log($"Aim: {value.isPressed}");
    private void OnReload(InputValue value) => Debug.Log($"Reload: {value.isPressed}");
    private void OnSprint(InputValue value) => Debug.Log($"Sprint: {value.isPressed}");
    private void OnInteract(InputValue value) => Debug.Log($"Interact: {value.isPressed}");
    private void OnInventory(InputValue value) => Debug.Log($"Inventory: {value.isPressed}");
    private void OnBuildMode(InputValue value) => Debug.Log($"BuildMode: {value.isPressed}");
}
