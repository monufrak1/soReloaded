// Lighthelper.fx
// Light helper functions

#define TEXTURE_SCALE_FAR 750.0f
#define TEXTURE_SCALE_CLOSE 300.0f

#define SHADOW_FILTER_DISTANCE 150.0f

float SMAP_SIZE = 1024.0f;
float SHADOW_EPSILON = 0.0025f;
float SMAP_DX  = 1 / 1024.0f;

struct Light
{
	float3 dir;
	float4 ambient;
	float4 diffuse;
	float4 specular;
};

struct SurfaceInfo
{
	float3 pos;
    float3 normal;
    float4 diffuse;
    float4 spec;
};

float3 DirectionalLight(SurfaceInfo v, Light L, float3 eyePos, float shadowFactor)
{
	float3 litColor = float3(0.0f, 0.0f, 0.0f);
 
	// The light vector aims opposite the direction the light rays travel.
	float3 lightVec = -L.dir;
	
	// Add the ambient term.
	litColor += v.diffuse * L.ambient;	
	
	// Add diffuse and specular term, provided the surface is in 
	// the line of site of the light.
	
	float diffuseFactor = dot(lightVec, v.normal);
	if( diffuseFactor > 0.0f )
	{
		float specPower  = max(v.spec.a, 1.0f);
		float3 toEye     = normalize(eyePos - v.pos);
		float3 R         = reflect(-lightVec, v.normal);
		float specFactor = pow(max(dot(R, toEye), 0.0f), specPower);
					
		// diffuse and specular terms
		litColor += shadowFactor * diffuseFactor * v.diffuse * L.diffuse;
		litColor += shadowFactor * specFactor * v.spec * L.specular;
	}

	return litColor;
}

float3 CalcSpecTerm(SurfaceInfo v, Light L, float3 eyePos)
{
	float3 litColor = {0.0f, 0.0f, 0.0f};
 
	// The light vector aims opposite the direction the light rays travel.
	float3 lightVec = normalize(-L.dir);	
	
	// Add diffuse and specular term, provided the surface is in 
	// the line of site of the light.
	
	float diffuseFactor = dot(lightVec, v.normal);
	if( diffuseFactor > 0.0f )
	{
		float specPower  = max(v.spec.a, 1.0f);
		float3 toEye     = normalize(eyePos - v.pos);
		float3 R         = reflect(-lightVec, v.normal);
		float specFactor = 1.5f * pow(max(dot(R, toEye), 0.0f), specPower);
					
		litColor += specFactor * v.spec * L.specular;
	}
	
	return litColor;
}

float CalcShadowFactorNoPCF(float4 projTexC, sampler shadowMap)
{
	// Complete projection
	projTexC.xyz /= projTexC.w;
	
	// No shadow is given to points outside of the shadow volume
	if( projTexC.x < -1.0f || projTexC.x > 1.0f || 
	    projTexC.y < -1.0f || projTexC.y > 1.0f ||
	    projTexC.z < 0.0f  || projTexC.z > 1.0f)
	{
	    return 1.0f;
	}
	    
	// Transform from NDC space to texture space
	projTexC.x = +0.5f * projTexC.x + 0.5f;
	projTexC.y = -0.5f * projTexC.y + 0.5f;
	
	// Depth in NDC space
	float depth = 1.0f - projTexC.z;

	// Sample shadow map 
	return depth > tex2D(shadowMap, projTexC.xy).r - SHADOW_EPSILON;
}

float CalcShadowFactorPCF(float4 projTexC, sampler shadowMap)
{
	// Complete projection
	projTexC.xyz /= projTexC.w;
	
	// No shadow is given to points outside of the shadow volume
	if( projTexC.x < -1.0f || projTexC.x > 1.0f || 
	    projTexC.y < -1.0f || projTexC.y > 1.0f ||
	    projTexC.z < 0.0f  || projTexC.z > 1.0f)
	{
	    return 1.0f;
	}
	    
	// Transform from NDC space to texture space
	projTexC.x = +0.5f * projTexC.x + 0.5f;
	projTexC.y = -0.5f * projTexC.y + 0.5f;
	
	// Depth in NDC space
	float depth = 1 - projTexC.z;

	// Sample shadow map 
	float samples[9];
	samples[0] = tex2D(shadowMap, projTexC.xy).r;
	samples[1] = tex2D(shadowMap, projTexC.xy + float2(0.0f, -SMAP_DX)).r;
	samples[2] = tex2D(shadowMap, projTexC.xy + float2(0.0f,  SMAP_DX)).r;
	samples[3] = tex2D(shadowMap, projTexC.xy + float2(-SMAP_DX, 0.0f)).r;
	samples[4] = tex2D(shadowMap, projTexC.xy + float2( SMAP_DX, 0.0f)).r;
	samples[5] = tex2D(shadowMap, projTexC.xy + float2(-SMAP_DX, -SMAP_DX)).r;
	samples[6] = tex2D(shadowMap, projTexC.xy + float2(-SMAP_DX,  SMAP_DX)).r;
	samples[7] = tex2D(shadowMap, projTexC.xy + float2( SMAP_DX,  SMAP_DX)).r;
	samples[8] = tex2D(shadowMap, projTexC.xy + float2( SMAP_DX, -SMAP_DX)).r;
	
	float avg = 0.0f;
	for(int i = 0; i < 9; i++)
	{
		avg += depth > samples[i] - SHADOW_EPSILON;
	}
	
	return avg / 9;
}

float CalcShadowFactor(float4 projTexC, sampler shadowMap, float distToEye)
{
	if(distToEye <= SHADOW_FILTER_DISTANCE)
	{
		return CalcShadowFactorPCF(projTexC, shadowMap);
	}
	else
	{
		return CalcShadowFactorNoPCF(projTexC, shadowMap);
	}
}

// Needed to compile. This .fx file contains helper functions only
technique NoTech
{
	pass P0
	{
		VertexShader = NULL;
		PixelShader  = NULL;
	}
}