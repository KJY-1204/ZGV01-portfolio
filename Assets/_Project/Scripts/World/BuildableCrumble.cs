// 건축물을 일정 시간 뒤 무너뜨리는 컴포넌트다. 무너질 때 파편과 먼지를 내고 오브젝트를 없앤다.
using System.Collections;
using UnityEngine;

public class BuildableCrumble : MonoBehaviour
{
    public void Begin(float delay)
    {
        if (delay <= 0f)
        {
            Execute();
            return;
        }
        StartCoroutine(Wait(delay));
    }

    private IEnumerator Wait(float delay)
    {
        yield return new WaitForSeconds(delay);
        Execute();
    }

    private void Execute()
    {
        BuildableHealth health = GetComponent<BuildableHealth>();
        if (health != null) health.PlayBreakSound();

        if (BuildableInstance.TryGetColliderBounds(gameObject, out Bounds bounds))
        {
            BuildableDebris.SpawnPieces(bounds, BuildableDebris.PickMaterial(gameObject));
            BuildableDebris.SpawnDust(bounds, Mathf.RoundToInt(bounds.size.magnitude * 5f));
        }
        // Destroy는 프레임 끝에 처리되므로 먼저 꺼서 같은 프레임에 다시 대상이 되지 않게 한다.
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
