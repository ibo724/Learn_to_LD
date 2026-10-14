Shader "Villa/Matte"
{
    Properties
    {
        [MainColor] _BaseColor("Sunlit color", Color) = (0.88, 0.81, 0.65, 1)
        _ShadeColor("Shade color", Color) = (0.52, 0.59, 0.61, 1)
        _ShadeThreshold("Shade threshold", Range(0, 1)) = 0.45
        _ShadeSoftness("Shade transition", Range(0.005, 0.5)) = 0.08
        _AmbientStrength("Minimum room brightness", Range(0.1, 1)) = 0.7
        [HDR] _AccentColor("Accent color", Color) = (0.15, 0.72, 0.7, 1)
        _AccentStrength("Accent emission", Range(0, 2)) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" "UniversalMaterialType"="Lit" }
        LOD 150

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _ShadeColor;
            half4 _AccentColor;
            half _ShadeThreshold;
            half _ShadeSoftness;
            half _AmbientStrength;
            half _AccentStrength;
        CBUFFER_END

        struct VillaAttributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct VillaVaryings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            float3 normalWS : TEXCOORD1;
            float4 shadowCoord : TEXCOORD2;
            half fogFactor : TEXCOORD3;
            half3 vertexLighting : TEXCOORD4;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };
        ENDHLSL

        // ForwardOnly is deliberate: the palette lighting also works with URP's deferred renderer.
        Pass
        {
            Name "VillaForward"
            Tags { "LightMode"="UniversalForwardOnly" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex VillaVertex
            #pragma fragment VillaFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            VillaVaryings VillaVertex(VillaAttributes input)
            {
                VillaVaryings output = (VillaVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.shadowCoord = GetShadowCoord(position);
                output.fogFactor = ComputeFogFactor(position.positionCS.z);
                output.vertexLighting = VertexLighting(position.positionWS, output.normalWS);
                return output;
            }

            half3 VillaAdditionalLight(Light light, half3 normalWS)
            {
                half diffuse = saturate(dot(normalWS, light.direction) * 0.8h + 0.2h);
                return _BaseColor.rgb * light.color * diffuse
                    * saturate(light.distanceAttenuation) * light.shadowAttenuation * 0.32h;
            }

            half4 VillaFragment(VillaVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half3 normalWS = NormalizeNormalPerPixel(input.normalWS);
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    float4 shadowCoord = input.shadowCoord;
                #else
                    float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #endif
                Light sun = GetMainLight(shadowCoord);
                half illumination = saturate(dot(normalWS, sun.direction) * 0.75h + 0.25h);
                half band = smoothstep(_ShadeThreshold - _ShadeSoftness,
                    _ShadeThreshold + _ShadeSoftness, illumination);
                band *= sun.shadowAttenuation * saturate(sun.distanceAttenuation);
                half3 color = lerp(_ShadeColor.rgb, _BaseColor.rgb, band);

                // An intentional brightness floor keeps palette colors readable in unbaked rooms.
                half3 ambient = SampleSH(normalWS);
                half3 lightTint = max(_AmbientStrength.xxx, ambient * 0.35h + sun.color * 0.65h);
                color *= min(lightTint, half3(1.2h, 1.2h, 1.2h));

                #if defined(_SCREEN_SPACE_OCCLUSION)
                    AmbientOcclusionFactor ao = GetScreenSpaceAmbientOcclusion(GetNormalizedScreenSpaceUV(input.positionCS));
                    color *= lerp(1.0h, ao.indirectAmbientOcclusion, 0.4h);
                #endif

                #if defined(_ADDITIONAL_LIGHTS)
                    InputData inputData = (InputData)0;
                    inputData.positionWS = input.positionWS;
                    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                    half4 shadowMask = half4(1, 1, 1, 1);
                    uint pixelLightCount = GetAdditionalLightsCount();
                    #if USE_CLUSTER_LIGHT_LOOP
                        UNITY_LOOP for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); ++lightIndex)
                        {
                            CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
                            color += VillaAdditionalLight(GetAdditionalLight(lightIndex, input.positionWS, shadowMask), normalWS);
                        }
                    #endif
                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        color += VillaAdditionalLight(GetAdditionalLight(lightIndex, input.positionWS, shadowMask), normalWS);
                    LIGHT_LOOP_END
                #endif
                #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                    color += input.vertexLighting * _BaseColor.rgb * 0.32h;
                #endif

                color += _AccentColor.rgb * _AccentStrength;
                return half4(MixFog(color, input.fogFactor), 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            Cull Back
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex VillaShadowVertex
            #pragma fragment VillaShadowFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            float3 _LightPosition;

            struct VillaShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            VillaShadowVaryings VillaShadowVertex(VillaAttributes input)
            {
                VillaShadowVaryings output = (VillaShadowVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 directionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 directionWS = _LightDirection;
                #endif
                output.positionCS = ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, directionWS)));
                return output;
            }

            half4 VillaShadowFragment(VillaShadowVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            Cull Back
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex VillaDepthVertex
            #pragma fragment VillaDepthFragment
            #pragma multi_compile_instancing

            struct VillaDepthVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            VillaDepthVaryings VillaDepthVertex(VillaAttributes input)
            {
                VillaDepthVaryings output = (VillaDepthVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half VillaDepthFragment(VillaDepthVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return input.positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormalsOnly"
            Tags { "LightMode"="DepthNormalsOnly" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex VillaNormalVertex
            #pragma fragment VillaNormalFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

            struct VillaNormalVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            VillaNormalVaryings VillaNormalVertex(VillaAttributes input)
            {
                VillaNormalVaryings output = (VillaNormalVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            void VillaNormalFragment(VillaNormalVaryings input, out half4 outNormalWS : SV_Target0
                #ifdef _WRITE_RENDERING_LAYERS
                    , out uint outRenderingLayers : SV_Target1
                #endif
            )
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 normalWS = normalize(input.normalWS);
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 octNormal = PackNormalOctQuadEncode(normalWS);
                    half3 packedNormal = half3(PackFloat2To888(saturate(octNormal * 0.5 + 0.5)));
                    outNormalWS = half4(packedNormal, 0);
                #else
                    outNormalWS = half4(normalWS, 0);
                #endif
                #ifdef _WRITE_RENDERING_LAYERS
                    outRenderingLayers = EncodeMeshRenderingLayer();
                #endif
            }
            ENDHLSL
        }
    }
    FallBack Off
}
