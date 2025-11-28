Shader "OccaSoftware/AutoExposure/FragmentCalculateExposure"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZWrite Off Cull Off ZTest Always
        Pass
        {
            Name "AutoExposureFragmentCalculateExposurePass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "AutoExposurePass.hlsl"
            
            TEXTURE2D_X(_Source);
            TEXTURE2D(_AutoExposureDataPrevious);
            
            half3 Fragment (Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return CalculateExposure(_Source, _AutoExposureDataPrevious);
            }
            ENDHLSL
        }
    }
}