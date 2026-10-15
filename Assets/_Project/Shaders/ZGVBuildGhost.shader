// 건축 미리보기(고스트)용 홀로그램 URP 셰이더다. 앞면만 보이도록 깊이를 먼저 채우고, 윤곽(프레넬)이 빛나며 주사선이 위로 흐른다.
// 색은 설치 가능이면 파랑, 불가능이면 빨강으로 BuildingSystem이 _GhostColor를 바꾼다. 원래 재질의 알베도는 옅게 섞어 무엇인지 알아보게 한다.
Shader "ZGV01/BuildGhost"
{
    Properties
    {
        [HDR] _GhostColor("Ghost Color", Color) = (0.25, 0.85, 1, 0.55)
        _BaseMap("Albedo (원본 재질에서 복사)", 2D) = "white" {}
        _TextureMix("Texture Mix", Range(0, 1)) = 0.3
        _RimPower("Rim Power", Range(0.5, 6)) = 2.2
        _ScanDensity("Scan Density", Float) = 9
        _ScanSpeed("Scan Speed", Float) = 1.6
        _ScanStrength("Scan Strength", Range(0, 1)) = 0.5
        _GridSize("Grid Size (m)", Float) = 0.5
        _GridStrength("Grid Strength", Range(0, 1)) = 0.55
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        // 깊이 선채움: 뒤쪽 면이 비쳐 지저분해지지 않게 가장 앞 면만 남긴다(색은 쓰지 않는다).
        Pass
        {
            Name "DepthPrime"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "Hologram"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _GhostColor;
                float4 _BaseMap_ST;
                half _TextureMix;
                half _RimPower;
                float _ScanDensity;
                float _ScanSpeed;
                half _ScanStrength;
                float _GridSize;
                half _GridStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertex = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertex.positionCS;
                output.positionWS = vertex.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 normal = normalize(input.normalWS);
                half3 viewDir = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half rim = pow(1.0h - saturate(dot(normal, viewDir)), _RimPower);

                // 아래에서 위로 흐르는 주사선과 가는 줄무늬.
                float phase = input.positionWS.y * _ScanDensity - _Time.y * _ScanSpeed;
                half lines = smoothstep(0.55h, 1.0h, 0.5h + 0.5h * sin(phase * 6.2831853h * 0.25h));
                half sweep = pow(saturate(1.0h - abs(frac(input.positionWS.y * 0.35 - _Time.y * 0.4) - 0.5h) * 6.0h), 3.0h);
                half scan = saturate(lines * 0.6h + sweep) * _ScanStrength;

                // 월드 격자선: 평평한 면에서도 홀로그램 느낌이 나도록 격자 모양으로 밝힌다(세 축 중 가까운 선).
                float3 cell = input.positionWS / _GridSize;
                float3 edgeDist = min(frac(cell), 1.0 - frac(cell));
                half grid = (1.0h - smoothstep(0.0h, 0.06h, min(edgeDist.x, min(edgeDist.y, edgeDist.z)))) * _GridStrength;

                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;
                half flicker = 1.0h - 0.06h * sin(_Time.y * 37.0h);

                half3 color = _GhostColor.rgb * (0.35h + rim * 1.1h + scan + grid) + albedo * _TextureMix * _GhostColor.rgb;
                half alpha = saturate(_GhostColor.a * (0.5h + rim * 0.7h + scan * 0.5h + grid * 0.6h) * flicker);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
