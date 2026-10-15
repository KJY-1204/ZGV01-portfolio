// 허약 상태일 때 점프와 달리기 입력을 StarterAssets 컨트롤러가 읽기 전에 지우는 어댑터다. 외부 컨트롤러 코드는 고치지 않는다.
using UnityEngine;

// 컨트롤러(기본 순서 0)의 Update보다 먼저 입력을 정리해야 한다.
[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(SurvivalStats))]
public class SurvivalMovementLimiter : MonoBehaviour
{
    private SurvivalStats stats;
    private StarterAssets.StarterAssetsInputs inputs;

    private void Awake()
    {
        stats = GetComponent<SurvivalStats>();
        inputs = GetComponent<StarterAssets.StarterAssetsInputs>();
    }

    private void Update()
    {
        if (inputs == null || !stats.IsWeakened) return;

        inputs.JumpInput(false);
        inputs.SprintInput(false);
    }
}
