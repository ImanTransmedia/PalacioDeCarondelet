Shader "OccaSoftware/AutoExposure/FragmentApply"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}

        ZWrite Off Cull Off ZTest Always
        Pass
        {
            Name "AutoExposureFragmentApplyPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "AutoExposurePass.hlsl"

            TEXTURE2D_X(_Source);
            TEXTURE2D(_AutoExposureData);
            TEXTURE2D(_AutoExposureDataPrevious);
            
            float3 Fragment (Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return ApplyExposure(input.texcoord, _Source, _AutoExposureData);
            }
            ENDHLSL
        }
    }
}