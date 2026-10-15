// F5로 저장, F9로 불러오기를 실행하는 테스트 전용 디버그 컨트롤러다. 정식 UI가 생기면 대체한다.
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class SaveLoadDebugController : MonoBehaviour
{
    // 저장/불러오기 결과를 화면에 알릴 때 쓴다(HUD가 구독한다). success가 false면 오류 스타일이다.
    public static event Action<string, bool> Notified;

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.f5Key.wasPressedThisFrame) SaveNow(gameObject);
        else if (Keyboard.current.f9Key.wasPressedThisFrame) LoadNow(gameObject);
    }

    // 다른 시스템(멀티플레이 서버 응답 등)이 HUD 알림을 띄울 때 쓴다.
    public static void Notify(string message, bool success) => Notified?.Invoke(message, success);

    // 저장하고 결과를 알린다(F5와 일시정지 메뉴가 같이 쓴다). auto가 참이면 자동 저장이라 참가자에게는 아무것도 하지 않고 알림 문구만 다르다.
    public static void SaveNow(GameObject player, bool auto = false)
    {
        // 멀티플레이 참가자는 저장하지 않는다(저장은 호스트가 맡는다).
        if (MultiplayerSession.IsClientOnly)
        {
            if (!auto) Notified?.Invoke("호스트만 저장할 수 있습니다", false);
            return;
        }
        GameSaveSystem.Save(player, auto);
        Notified?.Invoke($"{(auto ? "자동 저장했습니다" : "저장했습니다")} (슬롯 {GameSaveSystem.CurrentSlot})", true);
    }

    // 불러오고 결과를 알린다(F9와 일시정지 메뉴가 같이 쓴다). 죽은 상태에서는 체력 복원이 무시되므로 저장 파일이 있을 때만 먼저 제자리에서 되살린다(위치와 체력은 저장 파일이 정한다).
    // 멀티플레이 중에는 호스트도 사용자가 직접 불러올 수 없다(월드와 접속 중인 참가자 상태를 되돌려 맞지 않는다). 호스트가 저장을 이어하기로 시작하는 경로만 atSessionStart로 허용한다.
    public static bool LoadNow(GameObject player, bool atSessionStart = false)
    {
        if (MultiplayerSession.Active && !atSessionStart)
        {
            Notified?.Invoke("멀티플레이 중에는 불러올 수 없습니다", false);
            return false;
        }
        PlayerRespawn respawn = player.GetComponent<PlayerRespawn>();
        if (respawn != null && respawn.IsDead && GameSaveSystem.HasSave) respawn.ReviveInPlace();
        bool fromAuto = GameSaveSystem.LoadFromAuto;
        bool loaded = GameSaveSystem.Load(player);
        Notified?.Invoke(loaded ? $"{(fromAuto ? "자동 저장을 불러왔습니다" : "불러왔습니다")} (슬롯 {GameSaveSystem.CurrentSlot})" : "저장 파일이 없거나 읽을 수 없습니다", loaded);
        return loaded;
    }
}
