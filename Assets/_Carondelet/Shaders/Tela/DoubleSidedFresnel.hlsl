#ifndef DOUBLE_SIDED_FRESNEL_INCLUDED
#define DOUBLE_SIDED_FRESNEL_INCLUDED

void DoubleSidedFresnel_half(half3 Normal, half3 ViewDir, half Power, half Bias, half Scale, out half Out)
{
    #if defined(SHADERGRAPH_PREVIEW)
        Out = 0.5h; // Valor por defecto para preview en half
    #else
        half dotNV = abs(dot(normalize(Normal), normalize(ViewDir)));
        Out = Bias + Scale * pow(saturate(1.0h - dotNV), Power);
    #endif
}

void DoubleSidedFresnel_float(float3 Normal, float3 ViewDir, float Power, float Bias, float Scale, out float Out)
{
    #if defined(SHADERGRAPH_PREVIEW)
        Out = 0.5f; // Valor por defecto para preview en float
    #else
        float dotNV = abs(dot(normalize(Normal), normalize(ViewDir)));
        Out = Bias + Scale * pow(saturate(1.0f - dotNV), Power);
    #endif
}

#endif