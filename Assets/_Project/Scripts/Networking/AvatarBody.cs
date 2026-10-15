// 멀티플레이 접속자 아바타 몸(충돌체)의 물리 레이어 정보를 알려 주는 정적 도우미다. 아바타는 몸으로 서로 막히지만 사격, 시야, 조준 같은 레이캐스트 판정에서는 지금처럼 보이지 않아야 하므로 그런 판정은 ExcludeMask로 이 레이어를 뺀다.
using UnityEngine;

public static class AvatarBody
{
    public const string LayerName = "Avatar";

    private static int layer = int.MinValue;

    // 아바타 레이어 번호다(프로젝트에 레이어가 없으면 -1).
    public static int Layer
    {
        get
        {
            if (layer == int.MinValue) layer = LayerMask.NameToLayer(LayerName);
            return layer;
        }
    }

    // 레이캐스트에서 아바타를 건너뛰는 레이어 마스크다(레이어가 없으면 전부 포함).
    public static int ExcludeMask => Layer < 0 ? ~0 : ~(1 << Layer);
}
