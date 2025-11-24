Shader "Custom/URP_YLockedBillboard"
{
    Properties
    {
        [NoScaleOffset][MainTexture]_BaseMap("Base Map", 2D) = "white" {}
        [NoScaleOffset][Normal]_NormalMap("Normal Map", 2D) = "bump" {}
        [NoScaleOffset]_OcclusionMap("Occlusion Map", 2D) = "white" {}

        [Header(Transform)]
        _Tiling("Tiling", Vector) = (1, 1, 0, 0)
        _Offset("Offset", Vector) = (0, 0, 0, 0)

        [Header(Visual Settings)]
        _Color("Tint Color", Color) = (1,1,1,1)
        _Smoothness("Smoothness (Roughness)", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct appdata
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap);      SAMPLER(sampler_NormalMap);
            TEXTURE2D(_OcclusionMap);   SAMPLER(sampler_OcclusionMap);

            float2 _Tiling;
            float2 _Offset;
            float4 _Color;
            float _Smoothness;
            float _Metallic;

            v2f vert(appdata v)
            {
                v2f o;
                float3 centerWS = TransformObjectToWorld(float3(0, 0, 0));

                float3 camForward = normalize(centerWS - _WorldSpaceCameraPos);
                camForward.y = 0;
                camForward = normalize(camForward);

                float3 camRight = normalize(cross(float3(0,1,0), camForward));
                float3 camUp = float3(0,1,0);

                float3 scale = float3(
                    length(UNITY_MATRIX_M._m00_m10_m20),
                    length(UNITY_MATRIX_M._m01_m11_m21),
                    length(UNITY_MATRIX_M._m02_m12_m22)
                );

                float3 offset = v.positionOS.x * camRight * scale.x +
                                v.positionOS.z * camUp    * scale.y;

                float3 worldPos = centerWS + offset;

                o.positionHCS = TransformWorldToHClip(worldPos);
                o.uv = v.uv * _Tiling + _Offset;
                o.worldNormal = camUp;
                o.worldPos = worldPos;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                float4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _Color;
                float3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv));
                float ao = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).r;

                float3 normalWS = normalize(i.worldNormal);
                float3 tangentWS = normalize(cross(float3(0,1,0), normalWS));
                float3 bitangentWS = cross(normalWS, tangentWS);
                float3x3 TBN = float3x3(tangentWS, bitangentWS, normalWS);
                float3 normal = normalize(mul(normalTS, TBN));

                InputData inputData;
                inputData.positionWS = i.worldPos;
                inputData.normalWS = normal;
                inputData.viewDirectionWS = normalize(_WorldSpaceCameraPos - i.worldPos);
                inputData.shadowCoord = 0;
                inputData.fogCoord = 0;
                inputData.vertexLighting = float3(0, 0, 0);
                inputData.bakedGI = float3(0.5, 0.5, 0.5);
                inputData.normalizedScreenSpaceUV = i.uv;
                inputData.shadowMask = 1;

                SurfaceData surfaceData;
                surfaceData.albedo = albedo.rgb;
                surfaceData.alpha = albedo.a;
                surfaceData.normalTS = normalTS;
                surfaceData.emission = float3(0, 0, 0);
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = saturate(_Smoothness);
                surfaceData.occlusion = ao;
                surfaceData.clearCoatMask = 0.0;
                surfaceData.clearCoatSmoothness = 0.0;
                surfaceData.specular = float3(0.0, 0.0, 0.0);

                return UniversalFragmentPBR(inputData, surfaceData);
            }

            ENDHLSL
        }
    }
}
