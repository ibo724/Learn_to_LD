Shader "Villa/Water"
{
    Properties
    {
        [MainColor] _BaseColor("Shallow turquoise", Color) = (0.31, 0.72, 0.69, 1)
        _DeepColor("Deep turquoise", Color) = (0.08, 0.35, 0.43, 1)
        _EdgeColor("Edge and ripple color", Color) = (0.77, 0.91, 0.83, 1)
        _Opacity("Opacity", Range(0, 1)) = 0.72
        _DepthRange("Depth color distance", Range(0.1, 5)) = 1.6
        _WaveScale("Ripple scale", Range(0.1, 5)) = 0.8
        _WaveSpeed("Ripple speed", Range(0, 2)) = 0.25
        _WaveStrength("Ripple normal strength", Range(0, 0.2)) = 0.015
        [ToggleUI] _UseDepth("Use camera depth texture", Float) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "VillaWater"
            Tags { "LightMode"="UniversalForwardOnly" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex WaterVertex
            #pragma fragment WaterFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #define _SURFACE_TYPE_TRANSPARENT 1
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _DeepColor;
                half4 _EdgeColor;
                half _Opacity;
                half _DepthRange;
                half _WaveScale;
                half _WaveSpeed;
                half _WaveStrength;
                half _UseDepth;
            CBUFFER_END

            struct WaterAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct WaterVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 shadowCoord : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            WaterVaryings WaterVertex(WaterAttributes input)
            {
                WaterVaryings output = (WaterVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.shadowCoord = GetShadowCoord(position);
                output.fogFactor = ComputeFogFactor(position.positionCS.z);
                return output;
            }

            half4 WaterFragment(WaterVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = GetNormalizedScreenSpaceUV(input.positionCS);
                float depth = 0.8;
                if (_UseDepth > 0.5h)
                {
                    float rawDepth = SampleSceneDepth(uv);
                    float sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                    float surfaceDepth = -TransformWorldToView(input.positionWS).z;
                    depth = max(0, sceneDepth - surfaceDepth);
                }

                // Two clean sine strokes, rather than noisy or ocean-sized displacement.
                float2 position = input.positionWS.xz * _WaveScale;
                float time = _Time.y * _WaveSpeed;
                float waveA = dot(position, float2(1.0, 0.45)) * 6.0 + time;
                float waveB = dot(position, float2(-0.35, 1.0)) * 5.0 - time * 0.73;
                float2 slope = float2(cos(waveA) - cos(waveB) * 0.35,
                    cos(waveA) * 0.45 + cos(waveB)) * _WaveStrength;
                half3 normalWS = normalize(input.normalWS + float3(-slope.x, 0, -slope.y));
                half3 viewWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half fresnel = pow(1 - saturate(dot(normalWS, viewWS)), 3);
                half depthMix = saturate(depth / _DepthRange);
                half3 color = lerp(_BaseColor.rgb, _DeepColor.rgb, depthMix);
                half ripple = smoothstep(0.94h, 1.0h, sin(waveA) * 0.5h + 0.5h)
                    * smoothstep(-0.25h, 0.7h, sin(waveB));
                half edge = (1 - smoothstep(0.02h, 0.16h, depth)) * step(0.5h, _UseDepth);
                color = lerp(color, _EdgeColor.rgb, saturate(fresnel * 0.28h + ripple * 0.13h + edge * 0.4h));

                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    float4 shadowCoord = input.shadowCoord;
                #else
                    float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #endif
                Light sun = GetMainLight(shadowCoord);
                color *= lerp(0.79h, 1.0h, sun.shadowAttenuation);
                half alpha = saturate(_Opacity + fresnel * 0.12h + edge * 0.08h);
                return half4(MixFog(color, input.fogFactor), alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
