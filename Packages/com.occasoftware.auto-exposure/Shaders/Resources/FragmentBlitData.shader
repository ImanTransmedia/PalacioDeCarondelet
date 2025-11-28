Shader "OccaSoftware/AutoExposure/FragmentBlitData"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}

        ZWrite Off Cull Off ZTest Always
        Pass
        {
            Name "AutoExposureFragmentBlitDataPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "AutoExposurePass.hlsl"
            
            TEXTURE2D(_AutoExposureData);
            TEXTURE2D(_AutoExposureDataPrevious);
            
            float3 Fragment (Varyings input) : SV_Target
            {
                return LOAD_TEXTURE2D(_AutoExposureData, int2(0,0)).rgb;
            }
            ENDHLSL
        }
    }
}