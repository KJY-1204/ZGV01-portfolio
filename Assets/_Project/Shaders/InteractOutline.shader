// 상호작용 대상의 테두리를 그리는 URP 셰이더다. 메시를 경계 상자 중심 기준으로 부풀려 앞면을 컬링해 뒤쪽 면만 그린다(역면 외곽선).
Shader "ZGV01/InteractOutline"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.85, 0.25, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            // 속이 빈 메시(잎 카드 등)도 원래 모양이 비치도록 반투명으로 그린다.
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END

            // 렌더러마다 스크립트가 넣어 주는 월드 경계 상자 중심/절반 크기와 부풀릴 두께(월드 단위)다.
            float4 _OutlineCenter;
            float4 _OutlineExtents;
            float _OutlineWidth;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 extents = max(_OutlineExtents.xyz, 0.05);
                // 축마다 같은 절대 두께만큼 늘어나도록 (중심에서의 거리) x (1 + 두께/절반 크기)로 부풀린다.
                positionWS = _OutlineCenter.xyz + (positionWS - _OutlineCenter.xyz) * (1.0 + _OutlineWidth / extents);
                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return _Color;
            }
            ENDHLSL
        }
    }
}
