// 좀비용 URP Lit 확장 셰이더다. URP Lit 포워드 패스를 그대로 쓰고(그림자/깊이는 Lit 패스 재사용) 피격 플래시, 재처럼 사라지는 디졸브, 감염 맥동(발광 맵을 뛰게 함)을 덧붙인다.
// 효과 값은 ZombieShaderFX가 렌더러별 속성 블록으로 넣는다. 값이 0이면 Lit과 같다.
Shader "ZGV01/ZombieLit"
{
    Properties
    {
        // ---- URP Lit과 같은 속성(재질을 Lit에서 그대로 복사해 쓰기 위해 이름을 맞춘다) ----
        _WorkflowMode("WorkflowMode", Float) = 1.0
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _SmoothnessTextureChannel("Smoothness texture channel", Float) = 0
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _MetallicGlossMap("Metallic", 2D) = "white" {}
        _SpecColor("Specular", Color) = (0.2, 0.2, 0.2)
        _SpecGlossMap("Specular", 2D) = "white" {}
        [ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [ToggleOff] _EnvironmentReflections("Environment Reflections", Float) = 1.0
        _BumpScale("Scale", Float) = 1.0
        _BumpMap("Normal Map", 2D) = "bump" {}
        _Parallax("Scale", Range(0.005, 0.08)) = 0.005
        _ParallaxMap("Height Map", 2D) = "black" {}
        _OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
        _OcclusionMap("Occlusion", 2D) = "white" {}
        [HDR] _EmissionColor("Color", Color) = (0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}
        _DetailMask("Detail Mask", 2D) = "white" {}
        _DetailAlbedoMapScale("Scale", Range(0.0, 2.0)) = 1.0
        _DetailAlbedoMap("Detail Albedo x2", 2D) = "linearGrey" {}
        _DetailNormalMapScale("Scale", Range(0.0, 2.0)) = 1.0
        [Normal] _DetailNormalMap("Normal Map", 2D) = "bump" {}
        [HideInInspector] _ClearCoatMask("_ClearCoatMask", Float) = 0.0
        [HideInInspector] _ClearCoatSmoothness("_ClearCoatSmoothness", Float) = 0.0
        _Surface("__surface", Float) = 0.0
        _Blend("__blend", Float) = 0.0
        _Cull("__cull", Float) = 2.0
        [ToggleUI] _AlphaClip("__clip", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _SrcBlendAlpha("__srcA", Float) = 1.0
        [HideInInspector] _DstBlendAlpha("__dstA", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _BlendModePreserveSpecular("_BlendModePreserveSpecular", Float) = 1.0
        [HideInInspector] _AlphaToMask("__alphaToMask", Float) = 0.0
        [HideInInspector] _AddPrecomputedVelocity("_AddPrecomputedVelocity", Float) = 0.0
        [HideInInspector] _XRMotionVectorsPass("_XRMotionVectorsPass", Float) = 1.0
        [ToggleUI] _ReceiveShadows("Receive Shadows", Float) = 1.0
        _QueueOffset("Queue offset", Float) = 0.0
        [HideInInspector] _MainTex("BaseMap", 2D) = "white" {}
        [HideInInspector] _Color("Base Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _GlossMapScale("Smoothness", Float) = 0.0
        [HideInInspector] _Glossiness("Smoothness", Float) = 0.0
        [HideInInspector] _GlossyReflections("EnvironmentReflections", Float) = 0.0
        [HideInInspector][NoScaleOffset] unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}

        // ---- 좀비 효과(스크립트가 속성 블록으로 바꾼다) ----
        [Header(Hit Flash)]
        _HitColor("Hit Color", Color) = (1, 0.12, 0.08, 1)
        _HitFlash("Hit Flash", Range(0, 1)) = 0
        [Header(Dissolve)]
        _DissolveAmount("Dissolve Amount", Range(0, 1)) = 0
        [HDR] _DissolveEdgeColor("Dissolve Edge Color", Color) = (3, 1.1, 0.25, 1)
        _DissolveEdgeWidth("Dissolve Edge Width", Range(0.01, 0.4)) = 0.1
        _DissolveNoiseScale("Dissolve Noise Scale", Float) = 3
        [Header(Infection Pulse)]
        [HDR] _PulseColor("Pulse Color", Color) = (1.2, 0.15, 0.1, 1)
        _PulseAmount("Pulse Amount", Range(0, 1)) = 0
        _PulseSpeed("Pulse Speed", Float) = 2.2
        _PulsePhase("Pulse Phase", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend[_SrcBlend][_DstBlend], [_SrcBlendAlpha][_DstBlendAlpha]
            ZWrite[_ZWrite]
            Cull[_Cull]
            AlphaToMask[_AlphaToMask]

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex LitPassVertex
            #pragma fragment ZombieFragment

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local_fragment _OCCLUSIONMAP
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            #pragma shader_feature_local_fragment _SPECULAR_SETUP

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _SCREEN_SPACE_IRRADIANCE
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fragment _ LIGHTMAP_BICUBIC_SAMPLING
            #pragma multi_compile_fragment _ REFLECTION_PROBE_ROTATION
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"

            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"

            // Lit의 프래그먼트 함수를 이름만 바꿔 가져온 뒤 아래 ZombieFragment가 효과를 입혀 부른다.
            #define LitPassFragment LitPassFragmentBase
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"
            #undef LitPassFragment

            // 효과 속성은 UnityPerMaterial 상수 버퍼 밖에 둔다(속성 블록으로 렌더러마다 바꾼다).
            half4 _HitColor;
            half _HitFlash;
            half _DissolveAmount;
            half4 _DissolveEdgeColor;
            half _DissolveEdgeWidth;
            half _DissolveNoiseScale;
            half4 _PulseColor;
            half _PulseAmount;
            half _PulseSpeed;
            half _PulsePhase;
            // 그래픽 품질 단계(GraphicsPresets가 넣는 전역 값): 0 높음, 1 중간, 2 낮음. 낮음은 맥동과 디졸브 가장자리 빛을 끈다.
            float _ZGV_Cut;

            float Hash13(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.zyx + 31.32);
                return frac((p.x + p.y) * p.z);
            }

            // 3차원 값 잡음(두 겹)이다. 월드 좌표로 계산해 UV 이음매와 상관없이 몸 전체에 이어진다.
            float Noise3(float3 p)
            {
                float result = 0.0;
                float amplitude = 0.65;
                for (int octave = 0; octave < 2; octave++)
                {
                    float3 i = floor(p);
                    float3 f = frac(p);
                    f = f * f * (3.0 - 2.0 * f);
                    float n000 = Hash13(i), n100 = Hash13(i + float3(1, 0, 0));
                    float n010 = Hash13(i + float3(0, 1, 0)), n110 = Hash13(i + float3(1, 1, 0));
                    float n001 = Hash13(i + float3(0, 0, 1)), n101 = Hash13(i + float3(1, 0, 1));
                    float n011 = Hash13(i + float3(0, 1, 1)), n111 = Hash13(i + float3(1, 1, 1));
                    float nx00 = lerp(n000, n100, f.x), nx10 = lerp(n010, n110, f.x);
                    float nx01 = lerp(n001, n101, f.x), nx11 = lerp(n011, n111, f.x);
                    result += amplitude * lerp(lerp(nx00, nx10, f.y), lerp(nx01, nx11, f.y), f.z);
                    p *= 2.1;
                    amplitude *= 0.35;
                }
                return saturate(result / 0.8775);
            }

            void ZombieFragment(
                Varyings input
                , out half4 outColor : SV_Target0
            #ifdef _WRITE_RENDERING_LAYERS
                , out uint outRenderingLayers : SV_Target1
            #endif
            )
            {
                // 디졸브: 잡음이 문턱보다 낮은 곳을 버리고, 문턱 바로 위 띠는 불타는 가장자리로 빛낸다.
                half edge = 0;
                if (_DissolveAmount > 0.001)
                {
                    float noise = Noise3(input.positionWS * _DissolveNoiseScale);
                    float threshold = lerp(-0.05, 1.0 + _DissolveEdgeWidth, _DissolveAmount);
                    clip(noise - threshold);
                    edge = 1.0 - saturate((noise - threshold) / _DissolveEdgeWidth);
                    edge = edge * edge * (_ZGV_Cut >= 1.5 ? 0.0 : 1.0);
                }

            #ifdef _WRITE_RENDERING_LAYERS
                LitPassFragmentBase(input, outColor, outRenderingLayers);
            #else
                LitPassFragmentBase(input, outColor);
            #endif

                // 감염 맥동: 몸의 발광 맵(눈, 핏줄)을 박동에 맞춰 밝힌다.
                if (_PulseAmount > 0.001 && _ZGV_Cut < 1.5)
                {
                    half beat = 0.5h + 0.5h * sin(_Time.y * _PulseSpeed + _PulsePhase);
                    beat = beat * beat;
                    half3 mask = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb;
                    outColor.rgb += mask * _PulseColor.rgb * (_PulseAmount * beat);
                }

                outColor.rgb += _DissolveEdgeColor.rgb * edge;
                // 피격 플래시: 원래 색을 맞은 색으로 덮고 조금 더 밝힌다.
                outColor.rgb = lerp(outColor.rgb, _HitColor.rgb, _HitFlash * 0.55h) + _HitColor.rgb * (_HitFlash * 0.35h);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/SHADOWCASTER"
        UsePass "Universal Render Pipeline/Lit/DEPTHONLY"
        UsePass "Universal Render Pipeline/Lit/DEPTHNORMALS"
        UsePass "Universal Render Pipeline/Lit/META"
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.LitShader"
}
