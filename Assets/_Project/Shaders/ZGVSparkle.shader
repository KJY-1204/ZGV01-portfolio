// 자원 노드 반짝임용 URP 셰이더다. 월드 좌표에 놓인 사각형(코너는 UV -1~1)을 카메라를 향한 빌보드로 펴서 십자 별 모양을 더하기 혼합으로 그린다.
// 꼭짓점 색의 RGB는 색조, 알파는 반짝이는 시작 위상(0~1)이다. 멀어질수록 조금 커지고, 지정 거리에서 사라진다.
Shader "ZGV01/Sparkle"
{
    Properties
    {
        _Size("Size (m)", Float) = 0.18
        _DistanceGrowth("Distance Growth", Float) = 0.012
        _Intensity("Intensity", Float) = 1.4
        _TwinkleSpeed("Twinkle Speed", Float) = 2.6
        _FadeStart("Fade Start", Float) = 6
        _FadeEnd("Fade End", Float) = 12
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent+10" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Sparkle"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Size;
                float _DistanceGrowth;
                half _Intensity;
                half _TwinkleSpeed;
                float _FadeStart;
                float _FadeEnd;
            CBUFFER_END

            struct Attributes
            {
                float3 center : POSITION;   // 월드 좌표(메시를 단위 행렬로 그린다)
                float2 corner : TEXCOORD0;  // -1 ~ 1
                half4 color : COLOR;        // rgb 색조, a 위상
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 corner : TEXCOORD0;
                half4 color : TEXCOORD1;
                half fade : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float distanceToCamera = distance(input.center, _WorldSpaceCameraPos);
                float size = _Size * (1.0 + distanceToCamera * _DistanceGrowth);
                float3 right = UNITY_MATRIX_V[0].xyz;
                float3 up = UNITY_MATRIX_V[1].xyz;
                float3 positionWS = input.center + (right * input.corner.x + up * input.corner.y) * size;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.corner = input.corner;
                output.color = input.color;
                output.fade = saturate((_FadeEnd - distanceToCamera) / max(0.01, _FadeEnd - _FadeStart));
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 p = input.corner;
                half core = smoothstep(0.45h, 0.0h, length(p));
                // 가로/세로로 길게 뻗는 빛살.
                half rays = max(exp(-abs(p.y) * 22.0h) * saturate(1.0h - abs(p.x)), exp(-abs(p.x) * 22.0h) * saturate(1.0h - abs(p.y)));
                half twinkle = pow(0.5h + 0.5h * sin(_Time.y * _TwinkleSpeed + input.color.a * 6.2831853h), 5.0h);
                half intensity = (core * 0.9h + rays) * (0.05h + twinkle) * input.fade * _Intensity;
                return half4(input.color.rgb * intensity, 1.0h);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
