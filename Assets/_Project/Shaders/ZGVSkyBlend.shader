// 밤/노을/낮 큐브맵 세 장을 해의 고도에 따른 비중으로 섞고, 그 위에 시각에 따라 호를 그리며 이동하는 해와 달 원반을 그리는 스카이박스 셰이더다. 하늘마다 노출과 Y축 회전을 따로 가져서 노을 방위를 맞추고 별/구름을 흘려 보낸다. 내장 Skybox/Cubemap을 확장했다.
Shader "ZGV/SkyBlend"
{
    Properties
    {
        _Tint ("Tint Color", Color) = (.5, .5, .5, .5)
        [NoScaleOffset] _TexN ("Night Sky (HDR)", Cube) = "grey" {}
        [NoScaleOffset] _TexG ("Glow Sky (HDR)", Cube) = "grey" {}
        [NoScaleOffset] _TexD ("Day Sky (HDR)", Cube) = "grey" {}
        // 세 하늘의 비중(x 밤, y 노을, z 낮), 노출, Y축 회전(도)이다.
        _Weights ("Weights N/G/D", Vector) = (0, 0, 1, 0)
        _Exposures ("Exposures N/G/D", Vector) = (1, 1, 1, 0)
        _Rotations ("Rotations N/G/D", Vector) = (0, 0, 0, 0)
        // 낮 하늘에 곱하는 색이다(해가 낮을 때 주황빛을 입힌다).
        _DayTint ("Day Tint", Color) = (1, 1, 1, 1)
        // 밤 큐브맵에 구워진 달을 가릴 방향이다(xyz 방향, w 1이면 켬). 반경은 _MoonMaskCos(x 바깥, y 안쪽)이다.
        _MoonMask ("Baked Moon Mask", Vector) = (0, 1, 0, 0)
        _MoonMaskCos ("Baked Moon Mask Cos", Vector) = (0.9, 0.97, 0, 0)
        // 해/달 원반이다(xyz 방향, w 세기). 색과 각반경(라디안)은 따로 둔다.
        _SunDirInt ("Sun Dir (xyz) Intensity (w)", Vector) = (0, 1, 0, 0)
        _SunColor ("Sun Color", Color) = (1, 0.95, 0.85, 1)
        _SunRadius ("Sun Angular Radius", Float) = 0.045
        _MoonDirInt ("Moon Dir (xyz) Intensity (w)", Vector) = (0, -1, 0, 0)
        _MoonColor ("Moon Color", Color) = (0.85, 0.9, 1, 1)
        _MoonRadius ("Moon Angular Radius", Float) = 0.04
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            samplerCUBE _TexN;
            samplerCUBE _TexG;
            samplerCUBE _TexD;
            half4 _TexN_HDR;
            half4 _TexG_HDR;
            half4 _TexD_HDR;
            half4 _Tint;
            float4 _Weights;
            float4 _Exposures;
            float4 _Rotations;
            half4 _DayTint;
            float4 _MoonMask;
            float4 _MoonMaskCos;
            float4 _SunDirInt;
            half4 _SunColor;
            float _SunRadius;
            float4 _MoonDirInt;
            half4 _MoonColor;
            float _MoonRadius;

            float3 RotateAroundYInDegrees(float3 vertex, float degrees)
            {
                float alpha = degrees * UNITY_PI / 180.0;
                float sina, cosa;
                sincos(alpha, sina, cosa);
                float2x2 m = float2x2(cosa, -sina, sina, cosa);
                return float3(mul(m, vertex.xz), vertex.y).xzy;
            }

            struct appdata_t
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.vertex.xyz;
                return o;
            }

            half3 SampleSky(samplerCUBE tex, half4 hdr, float3 dir, half exposure, float rotation)
            {
                half3 c = DecodeHDR(texCUBE(tex, RotateAroundYInDegrees(dir, rotation)), hdr);
                return c * _Tint.rgb * unity_ColorSpaceDouble.rgb * exposure;
            }

            // 밤 하늘은 구워진 달 둘레를 같은 높이의 다른 방위 하늘로 덮어 가린다(진짜 달은 원반으로 따로 그린다).
            half3 SampleNight(float3 dir)
            {
                float3 d = RotateAroundYInDegrees(dir, _Rotations.x);
                half3 c = DecodeHDR(texCUBE(_TexN, d), _TexN_HDR);
                if (_MoonMask.w > 0.5)
                {
                    float w = smoothstep(_MoonMaskCos.x, _MoonMaskCos.y, dot(normalize(d), _MoonMask.xyz));
                    if (w > 0.0)
                    {
                        half3 fill = DecodeHDR(texCUBE(_TexN, RotateAroundYInDegrees(d, 40.0)), _TexN_HDR);
                        c = lerp(c, fill, w);
                    }
                }
                return c * _Tint.rgb * unity_ColorSpaceDouble.rgb * _Exposures.x;
            }

            // 원반(가장자리가 부드러운 원)과 둘레의 빛무리를 합친 밝기다.
            half DiscGlow(float3 dir, float3 center, float radius, half discAmount, half haloAmount)
            {
                float ang = acos(clamp(dot(dir, center), -1.0, 1.0));
                half disc = smoothstep(radius * 1.25, radius * 0.75, ang);
                half halo = exp(-(ang * ang) / (radius * radius * 22.0));
                return disc * discAmount + halo * haloAmount;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                half3 sky = 0;
                if (_Weights.x > 0.001) sky += SampleNight(i.texcoord) * _Weights.x;
                if (_Weights.y > 0.001) sky += SampleSky(_TexG, _TexG_HDR, i.texcoord, _Exposures.y, _Rotations.y) * _Weights.y;
                if (_Weights.z > 0.001) sky += SampleSky(_TexD, _TexD_HDR, i.texcoord, _Exposures.z, _Rotations.z) * _DayTint.rgb * _Weights.z;

                // 지평선 아래로는 해/달을 그리지 않는다.
                float3 dir = normalize(i.texcoord);
                half horizon = smoothstep(-0.02, 0.06, dir.y);
                if (_SunDirInt.w > 0.001)
                    sky += _SunColor.rgb * DiscGlow(dir, _SunDirInt.xyz, _SunRadius, 3.0, 0.4) * _SunDirInt.w * horizon;
                if (_MoonDirInt.w > 0.001)
                    sky += _MoonColor.rgb * DiscGlow(dir, _MoonDirInt.xyz, _MoonRadius, 1.15, 0.22) * _MoonDirInt.w * horizon;
                return half4(sky, 1);
            }
            ENDCG
        }
    }

    Fallback Off
}
