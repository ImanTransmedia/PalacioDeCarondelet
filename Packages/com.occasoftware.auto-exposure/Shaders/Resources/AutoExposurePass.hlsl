#ifndef OS_AUTO_EXPOSURE_PASS_INCLUDED
#define OS_AUTO_EXPOSURE_PASS_INCLUDED

#include "AutoExposureCommon.hlsl"

int _SampleCount;
float _Response;

int _ClampingEnabled;
float _ClampingBracket;

int _MeteringMaskMode;
Texture2D _MeteringMaskTexture;
SamplerState sampler_MeteringMaskTexture;
float _MeteringProceduralFalloff;
int _AnimateSamplePositions;


int _AdaptationMode;
int _IsFirstFrame;
float _FixedCompensation;
float _DarkToLightSpeed;
float _LightToDarkSpeed;
float _EvMin;
float _EvMax;
Texture2D _ExposureCompensationCurve;

float3 CalculateExposure(TEXTURE2D_X(Screen), Texture2D PermanentData)
{
	float3 Result = float3(0,0,0);
	
	// Sample Previous Data
	float3 resultPrevious = PermanentData.Load(int3(0, 0, 0)).rgb;
	
	
	///////////////////////////////
	// Calculate Luminance       //
	///////////////////////////////
	
	float luminance = 0;
	float importanceSum = 0.0;
	
	float importance;
	float2 samplePoint;
	float3 color;
	float luminanceSample;
	
	for (int i = 0; i < _SampleCount; i++)
	{
		// Calculate Sample Point
		samplePoint = rand2dTo2d(i + _Time.yy * _AnimateSamplePositions);
		samplePoint = UnityStereoTransformScreenSpaceTex(samplePoint);
		
		// Calculate Importance
		importance = CalculateImportance(samplePoint, _MeteringMaskMode, _MeteringProceduralFalloff, _MeteringMaskTexture);
		importanceSum += importance;
		
		
		// Evaluate Luminance
		color = SAMPLE_TEXTURE2D_X_LOD(Screen, linear_clamp_sampler, samplePoint, 0).rgb;
		luminanceSample = GetLuminance(color);
		
		if(_ClampingEnabled == 1)
		{
			luminanceSample = clamp(luminanceSample, resultPrevious.r - _ClampingBracket, resultPrevious.r + _ClampingBracket);
		}
		
		// Integrate Luminance
		luminance += luminanceSample * importance;
	}
	
	
	// Estimate mean luminance
	luminance /= importanceSum;
	if(_IsFirstFrame)
		_Response = 1.0f;
		
	luminance = lerp(resultPrevious.r, luminance, _Response);
	luminance = max(luminance, 0);
	Result.r = luminance;
	
	
	///////////////////////////////
	// Calculate Target Ev       //
	///////////////////////////////
	float dT = unity_DeltaTime.x;
	float targetEv = CalculateTargetEv(Result.r, resultPrevious.g, _IsFirstFrame, _AdaptationMode, _LightToDarkSpeed, _DarkToLightSpeed, _EvMin, _EvMax, dT);
	Result.g = targetEv;
	
	
	///////////////////////////////
	// Calculate Exposure        //
	///////////////////////////////
	
	float targetEv01 = Map(targetEv, _EvMin, _EvMax, 0.0, 1.0);
    float compensationCurve = _ExposureCompensationCurve.SampleLevel(linear_clamp_sampler, float2(targetEv01, 0), 0).r;
	
	targetEv -= compensationCurve;
	targetEv -= _FixedCompensation;
	targetEv+= _MIDDLE_GRAY_ADJ_50_PCT;
	// If the average scene luminance is ~0.5, this algorithm suggests doubling the brightness. 
	// However, we generally prefer that the scene rests around 0.5 luminance after compensation (i.e., 50/50 split between "bright" and "dark").
	// Therefore, we use this term to adjust the default recommendation to leaving the brightness as-is when the scene luminance is 0.5.
	
	Result.b = ConvertEVToExposureMultiplier(targetEv);	
	
	return Result;
}


float3 ApplyExposure(float2 UV, TEXTURE2D_X(Screen), Texture2D Data)
{
	float3 Color = float3(0,0,0);
	float ExposureMultiplier = Data.Load(int3(0, 0, 0)).b;
	
	Color = SAMPLE_TEXTURE2D_X_LOD(Screen, linear_clamp_sampler, UnityStereoTransformScreenSpaceTex(UV), 0).rgb * ExposureMultiplier;
	
	#if DEBUG_MODE_ENABLED
	float3 DataOutput = Data.Load(int3(0, 0, 0)).rgb;
	if(UV.y > 0.75)
	{
		if(UV.x < 0.9)
		{
			Color = Map(DataOutput.b, _EvMin,_EvMax, 0.0, 1.0);
			Color = DataOutput.b;
		}   
		if(UV.x < 0.8)
		{
			Color = Map(DataOutput.g, _EvMin,_EvMax, 0.0, 1.0);	
			Color = DataOutput.g;
		}   
		if(UV.x < 0.7)
		{
			Color = DataOutput.r;
		}
	}
	#endif
	
	return Color;
}

#endif