// 연못 물 표면 URP 셰이더다. 얇은 원통 윗면(오브젝트 공간 반지름 0.5)에 쓰도록 만들었고, 깊이 텍스처 없이 반지름으로 깊이감과 가장자리 거품을 낸다.
// 잔물결 법선, 프레넬 반사(하늘/반사 프로브), 태양 하이라이트, 가장자리 투명도를 합친다.
Shader "ZGV01/PondWater"
{
    Properties
    {
        _ShallowColor("Shallow Color", Color) = (0.16, 0.36, 0.3, 1)
        _DeepColor("Deep Color", Color) = (0.03, 0.1, 0.09, 1)
        _FoamColor("Foam Color", Color) = (0.75, 0.82, 0.78, 1)
        _Opacity("Center Opacity", Range(0, 1)) = 0.93
        _RippleScale("Ripple Scale", Float) = 1.1
        _RippleStrength("Ripple Strength", Range(0, 1)) = 0.35
        _RippleSpeed("Ripple Speed", Float) = 0.35
        _SunSharpness("Sun Sharpness", Range(20, 400)) = 220
        _ReflectionStrength("Reflection Strength", Range(0, 1)) = 0.85
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        Pass
        {
            Name "Water"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                half4 _FoamColor;
                half _Opacity;
                float _RippleScale;
                half _RippleStrength;
                float _RippleSpeed;
                half _SunSharpness;
                half _ReflectionStrength;
            CBUFFER_END

            // 그래픽 품질 단계(GraphicsPresets가 넣는 전역 값): 0 높음, 1 중간, 2 낮음. 낮음은 물결/반사/거품 없이 색만 칠한다.
            float _ZGV_Cut;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 localXZ : TEXCOORD1;
                half fogFactor : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertex = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertex.positionCS;
                output.positionWS = vertex.positionWS;
                output.localXZ = input.positionOS.xz;
                output.fogFactor = ComputeFogFactor(vertex.positionCS.z);
                return output;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash21(i), Hash21(i + float2(1, 0)), f.x), lerp(Hash21(i + float2(0, 1)), Hash21(i + float2(1, 1)), f.x), f.y);
            }

            // 서로 다른 방향으로 흐르는 두 잡음의 기울기로 잔물결 법선을 만든다.
            float3 RippleNormal(float2 xz, float time)
            {
                float2 p1 = xz * _RippleScale + float2(time, time * 0.6);
                float2 p2 = xz * _RippleScale * 2.3 + float2(-time * 0.8, time * 1.1);
                const float e = 0.05;
                float h1 = ValueNoise(p1);
                float2 g1 = float2(ValueNoise(p1 + float2(e, 0)) - h1, ValueNoise(p1 + float2(0, e)) - h1) / e;
                float h2 = ValueNoise(p2);
                float2 g2 = float2(ValueNoise(p2 + float2(e, 0)) - h2, ValueNoise(p2 + float2(0, e)) - h2) / e;
                float2 g = g1 * 0.7 + g2 * 0.4;
                return normalize(float3(-g.x * _RippleStrength, 1.0, -g.y * _RippleStrength));
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 가운데 0 ~ 가장자리 1.
                float radius = saturate(length(input.localXZ) * 2.0);
                float time = _Time.y * _RippleSpeed;

                if (_ZGV_Cut >= 1.5)
                {
                    half3 flatColor = MixFog(lerp(_DeepColor.rgb, _ShallowColor.rgb, smoothstep(0.1, 0.95, radius)), input.fogFactor);
                    return half4(flatColor, _Opacity * (1.0h - smoothstep(0.88, 1.0, radius)));
                }

                // 가장자리로 갈수록 물결이 약하고 얕다.
                float3 normalWS = RippleNormal(input.positionWS.xz, time);
                normalWS = normalize(lerp(normalWS, float3(0, 1, 0), smoothstep(0.7, 1.0, radius) * 0.6));
                half3 viewDir = GetWorldSpaceNormalizeViewDir(input.positionWS);

                half fresnel = pow(1.0h - saturate(dot(normalWS, viewDir)), 4.0h);
                half depthMix = smoothstep(0.1, 0.95, radius);
                half3 body = lerp(_DeepColor.rgb, _ShallowColor.rgb, depthMix);

                // 하늘/반사 프로브 반사를 프레넬로 섞는다.
                half3 reflectDir = reflect(-viewDir, normalWS);
                half3 reflection = GlossyEnvironmentReflection(reflectDir, 0.05h, 1.0h);
                half3 color = lerp(body, reflection, saturate(fresnel * 0.9h + 0.12h) * _ReflectionStrength);

                // 태양 하이라이트.
                Light mainLight = GetMainLight();
                half3 halfDir = normalize(mainLight.direction + viewDir);
                half sun = pow(saturate(dot(normalWS, halfDir)), _SunSharpness);
                color += mainLight.color * sun * 1.4h * saturate(mainLight.direction.y * 4.0h);

                // 가장자리 거품: 잡음으로 들쭉날쭉하게 띠를 만든다.
                float foamNoise = _ZGV_Cut >= 0.5 ? 0.5 : ValueNoise(input.positionWS.xz * 3.2 + time * 0.5);
                half foam = smoothstep(0.87, 0.95, radius + (foamNoise - 0.5) * 0.14) * (1.0h - smoothstep(0.97, 1.0, radius));
                color = lerp(color, _FoamColor.rgb * (0.4h + 0.6h * mainLight.color), foam * 0.55h);

                half alpha = _Opacity * (1.0h - smoothstep(0.88, 1.0, radius));
                alpha = saturate(alpha + foam * 0.35h + sun * 0.5h);

                color = MixFog(color, input.fogFactor);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
