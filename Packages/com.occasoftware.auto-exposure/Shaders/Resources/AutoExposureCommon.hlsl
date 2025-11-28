#ifndef OS_AUTO_EXPOSURE_COMMON_INCLUDE
#define OS_AUTO_EXPOSURE_COMMON_INCLUDE

#define DEBUG_MODE_ENABLED 0
#define _MIDDLE_GRAY_ADJ_18_PCT 2.473
#define _MIDDLE_GRAY_ADJ_50_PCT 1.0

SamplerState point_clamp_sampler;
SamplerState linear_clamp_sampler;



float GetEV100(float luminance)
{
	luminance = max(luminance, 1e-5);
	return log2(8.0 * luminance);
}

float LumFromEV100(float ev)
{
	return exp2(ev) / 8.0;
}

float ConvertEVToExposureMultiplier(float ev)
{
	return 8.0 / pow(2.0, ev);
}

float Map(float value, float start1, float stop1, float start2, float stop2)
{
	value = clamp(value, start1, stop1);
	return start2 + (stop2 - start2) * ((value - start1) / (stop1 - start1));
}

float rand2dTo1d(float2 vec, float2 dotDir = float2(12.9898, 78.233))
{
	float random = dot(sin(vec.xy), dotDir);
	random = frac(sin(random) * 143758.5453);
	return random;
}

float2 rand2dTo2d(float2 vec, float2 seed = 4605)
{
	return float2(
		rand2dTo1d(vec + seed, float2(12.989, 78.233)),
		rand2dTo1d(vec + seed, float2(39.346, 11.135))
	);
}


float GetLuminance(float3 color)
{
	const float3 _RGB_WEIGHTS = float3(0.2126, 0.7152, 0.0722);
	return dot(color, _RGB_WEIGHTS);
}


float CalculateImportance(float2 uv, int mode, float falloff, Texture2D tex)
{
	const half2 _Center = half2(0.5, 0.5);
	
	if (mode == 0)
	{
		float d = saturate(1.0 - (distance(uv, _Center) * 2.0));
		return pow(d, falloff);
	}
	else
	{
		return tex.SampleLevel(linear_clamp_sampler, uv, 0).r;
	}
}


float CalculateTargetEv(float luminanceCurrent, float evPrevious, int isFirstFrame, int adaptationMode, float lightToDarkSpeed, float darkToLightSpeed, float evMin, float evMax, float deltaTime)
{
	float avgEv = GetEV100(luminanceCurrent);
	float targetEv = avgEv;
	float pastEv = evPrevious;
	

	if(isFirstFrame == 1)
		pastEv = avgEv;
	
	if(adaptationMode == 0)
	{
		float t = lightToDarkSpeed;
		float diff = avgEv - pastEv;
		if (diff > 0)
		{
			t = darkToLightSpeed;
		}
		
		t *= min(deltaTime, 0.033);
		
		// If the difference between the average luminance and the past luminance is greater than 1.5 f-stops, use linear movement.
		// Otherwise, use exponential movement.
		float exponentialMovement = pastEv + diff * t;
		float linearMovement = pastEv + sign(diff) * t;
		float movementInterpolator = Map(abs(diff), 0, 1.5, 0, 1);
		targetEv = lerp(exponentialMovement, linearMovement, movementInterpolator);	
	}
	
	return clamp(targetEv, evMin, evMax);
}


#endif