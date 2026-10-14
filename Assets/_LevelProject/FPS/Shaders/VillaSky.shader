Shader "Villa/Sky"
{
    Properties
    {
        _TopColor("Upper sky", Color) = (0.31, 0.55, 0.61, 1)
        _HorizonColor("Horizon", Color) = (0.91, 0.85, 0.68, 1)
        _BottomColor("Below horizon", Color) = (0.56, 0.63, 0.54, 1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Pass
        {
            Name "VillaSky"
            Cull Off
            ZWrite Off
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex SkyVertex
            #pragma fragment SkyFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor;
                half4 _HorizonColor;
                half4 _BottomColor;
            CBUFFER_END

            struct SkyAttributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct SkyVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            SkyVaryings SkyVertex(SkyAttributes input)
            {
                SkyVaryings output = (SkyVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }
            half4 SkyFragment(SkyVaryings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float height = normalize(input.direction).y;
                half3 sky = lerp(_HorizonColor.rgb, _TopColor.rgb, smoothstep(0, 0.78, height));
                sky = lerp(sky, _BottomColor.rgb, smoothstep(0, 0.55, -height));
                return half4(sky, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
