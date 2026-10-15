// 이 오브젝트(와 자식 콜라이더)에 맞았을 때 어떤 표면 이펙트를 쓸지 표시하는 컴포넌트다.
using UnityEngine;

public class ImpactSurfaceMarker : MonoBehaviour
{
    [SerializeField] private ImpactSurface surface = ImpactSurface.Concrete;

    public ImpactSurface Surface => surface;

    // 코드로 표식을 붙일 때(자원 노드처럼 정의 데이터에서 표면을 정할 때) 쓴다.
    public void Initialize(ImpactSurface newSurface)
    {
        surface = newSurface;
    }
}
