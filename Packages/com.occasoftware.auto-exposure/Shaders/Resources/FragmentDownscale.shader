Shader "OccaSoftware/AutoExposure/FragmentDownscale"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        ZWrite Off Cull Off ZTest Always
        Pass
        {
            Name "AutoExposureFragmentDownscalePass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            
            
            TEXTURE2D_X(_Source);
            SAMPLER(sampler_Source);
            
            float3 Fragment (Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return SAMPLE_TEXTURE2D_X_LOD(_Source, sampler_Source, input.texcoord, 0).rgb;
            }
            ENDHLSL
        }
    }
}