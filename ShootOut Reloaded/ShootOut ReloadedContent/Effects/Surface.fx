// Surface.fx

#include "Lighthelper.fx"

// VARIABLES
float g_TexScale;

// SHARED VARIABLES
float4x4 g_View;
float4x4 g_Projection;
float4x4 g_ReflectionView;
float4x4 g_ReflectionProjection;
float4x4 g_LightView;
float4x4 g_LightProjection;
float3   g_EyePosW;

float3 g_LightDir;
float4 g_LightAmbient;
float4 g_LightDiffuse;
float4 g_LightSpecular;

float g_WaterHeight;
float4 g_WaterColor;

float  g_FogStart;
float  g_FogRange;
float  g_DeepWaterFogDistance;
float g_DarkSkyOffset;
float4 g_SkyColor;

// TEXTURES
Texture2D g_DiffMap; 
Texture2D g_SpecMap;
Texture2D g_NormMap;
Texture2D g_ShadowMap;

sampler g_DiffMapSampler = sampler_state
{
	texture   = <g_DiffMap>;
	minfilter = LINEAR;
	magfilter = LINEAR;
	mipfilter = LINEAR;
	addressU = WRAP;
	addressV = WRAP;
};

sampler g_SpecMapSampler = sampler_state
{
	texture   = <g_SpecMap>;
	minfilter = LINEAR;
	magfilter = LINEAR;
	mipfilter = LINEAR;
	addressU = WRAP;
	addressV = WRAP;
};

sampler g_NormMapSampler = sampler_state
{
	texture   = <g_NormMap>;
	minfilter = LINEAR;
	magfilter = LINEAR;
	mipfilter = LINEAR;
	addressU = WRAP;
	addressV = WRAP;
};

sampler g_ShadowMapSampler = sampler_state
{
	texture = <g_ShadowMap>;
	minfilter = POINT;
	magfilter = POINT;
	mipfilter = POINT;
	addressU = CLAMP;
	addressV = CLAMP;
};

// SHADER I/O STRUCTS
struct SurfaceVS_IN
{
	float3 posL : POSITION;
	float2 tex  : TEXCOORD;
};

struct SurfacePS_IN
{
	float4 posH : SV_POSITION;
	float3 posW : TEXCOORD0;
	float2 tex      : TEXCOORD1;
	float4 lightPos : TEXCOORD2;
};

struct SurfaceNoShadowPS_IN
{
	float4 posH : SV_POSITION;
	float3 posW : TEXCOORD0;
	float2 tex  : TEXCOORD1;
};

struct SurfaceOcclusionPS_IN
{
	float4 posH : SV_POSITION;
};

// VERTEX SHADERS
SurfacePS_IN SurfaceVS(SurfaceVS_IN vIn)
{
	SurfacePS_IN vOut;

	// Input vertex is in world-space
	float4x4 WVP = mul(g_View, g_Projection);

	vOut.posH = mul(float4(vIn.posL, 1.0f), WVP);
	vOut.posW = vIn.posL;
	vOut.tex = vIn.tex * g_TexScale;

	// Output light coordinates
	float4x4 lightWVP = mul(g_LightView, g_LightProjection);
	vOut.lightPos = mul(float4(vIn.posL, 1.0f), lightWVP);

	return vOut;
} // end SurfaceVS

SurfaceNoShadowPS_IN SurfaceNoShadowVS(SurfaceVS_IN vIn)
{
	SurfaceNoShadowPS_IN vOut;

	// Input vertex is in world-space
	float4x4 WVP = mul(g_View, g_Projection);

	vOut.posH = mul(float4(vIn.posL, 1.0f), WVP);
	vOut.posW = vIn.posL;
	vOut.tex = vIn.tex * g_TexScale;

	return vOut;
} // end SurfaceNoShadowVS

SurfaceOcclusionPS_IN SurfaceOcclusionVS(SurfaceVS_IN vIn)
{
	SurfaceOcclusionPS_IN vOut;

	// Input vertex is in world-space
	float4x4 WVP = mul(g_View, g_Projection);

	vOut.posH = mul(float4(vIn.posL, 1.0f), WVP);

	return vOut;
} // end SurfaceOcclusionVS

// PIXEL SHADERS
float4 SurfacePS(SurfacePS_IN pIn) : SV_TARGET
{
	float4 diffuse = tex2D(g_DiffMapSampler, pIn.tex);
	float4 spec    = tex2D(g_SpecMapSampler, pIn.tex);
	float3 normT   = tex2D(g_NormMapSampler, pIn.tex).xyz;

	spec.a *= 256.0f;

	// Transform normal
	normT = (2.0f * normT) - 1.0f;
	normT = normalize(normT);

	float3x3 TBN = float3x3(float3(1.0f, 0.0f, 0.0f),
							float3(0.0f, 0.0f, 1.0f),
							float3(0.0f, 1.0f, 0.0f));

	float3 finalNorm = mul(normT, TBN);

	// Light the pixel
	SurfaceInfo v = {pIn.posW, finalNorm, diffuse, spec};
	Light light = {g_LightDir, g_LightAmbient, g_LightDiffuse, g_LightSpecular};

	
	float shadowFactor = CalcShadowFactor(pIn.lightPos, g_ShadowMapSampler, 
							distance(pIn.posW, g_EyePosW));
	float3 litColor = DirectionalLight(v, light, g_EyePosW, shadowFactor);

	if(pIn.posW.y < g_WaterHeight)
	{
		// Add underwater color
		float3 toWaterSurface = pIn.posW - float3(pIn.posW.x, g_WaterHeight, pIn.posW.z);
		float waterDist = length(toWaterSurface);
		
		float waterFog = min(saturate((waterDist/g_DeepWaterFogDistance)), 0.95f);
		litColor = lerp(float4(litColor, 1.0f), g_WaterColor, waterFog).rgb;
	}
	else
	{
		// Calculate fog
		float3 toEye = pIn.posW - g_EyePosW;
		float dist = length(toEye);
		toEye = normalize(toEye);

		float fogLerp = saturate((dist - g_FogStart) / g_FogRange);
		float4 darkSkyColor = float4(g_SkyColor.rgb * g_DarkSkyOffset, 1.0f);
		float4 fogColor = lerp(g_SkyColor, darkSkyColor, 
				saturate(dot(float3(0.0f, 1.0f, 0.0f), toEye) + 0.1f));
	
		litColor = lerp(litColor, fogColor, fogLerp);
	}

	return float4(litColor, diffuse.a);
} // end SurfacePS

float4 SurfaceNoShadowPS(SurfaceNoShadowPS_IN pIn) : SV_TARGET
{
	float4 diffuse = tex2D(g_DiffMapSampler, pIn.tex);
	float4 spec    = tex2D(g_SpecMapSampler, pIn.tex);
	float3 normT   = tex2D(g_NormMapSampler, pIn.tex).xyz;

	spec.a *= 256.0f;

	// Transform normal
	normT = (2.0f * normT) - 1.0f;
	normT = normalize(normT);

	float3x3 TBN = float3x3(float3(1.0f, 0.0f, 0.0f),
							float3(0.0f, 0.0f, 1.0f),
							float3(0.0f, 1.0f, 0.0f));

	float3 finalNorm = mul(normT, TBN);

	// Light the pixel
	SurfaceInfo v = {pIn.posW, finalNorm, diffuse, spec};
	Light light = {g_LightDir, g_LightAmbient, g_LightDiffuse, g_LightSpecular};

	float3 litColor = DirectionalLight(v, light, g_EyePosW, 1.0f);

	if(pIn.posW.y < g_WaterHeight)
	{
		// Add underwater color
		float3 toWaterSurface = pIn.posW - float3(pIn.posW.x, g_WaterHeight, pIn.posW.z);
		float waterDist = length(toWaterSurface);
		
		float waterFog = min(saturate((waterDist/g_DeepWaterFogDistance)), 0.95f);
		litColor = lerp(float4(litColor, 1.0f), g_WaterColor, waterFog).rgb;
	}
	else
	{
		// Calculate fog
		float3 toEye = pIn.posW - g_EyePosW;
		float dist = length(toEye);
		toEye = normalize(toEye);

		float fogLerp = saturate((dist - g_FogStart) / g_FogRange);
		float4 darkSkyColor = float4(g_SkyColor.rgb * g_DarkSkyOffset, 1.0f);
		float4 fogColor = lerp(g_SkyColor, darkSkyColor, 
				saturate(dot(float3(0.0f, 1.0f, 0.0f), toEye) + 0.1f));
	
		litColor = lerp(litColor, fogColor, fogLerp);
	}

	return float4(litColor, diffuse.a);
} // end SurfaceNoShadowPS

float4 SurfaceOcclusionPS(SurfaceOcclusionPS_IN pIn) : SV_TARGET
{
	// Output BLACK as occluder
	return float4(0.0f, 0.0f, 0.0f, 1.0f);
}

// TECHNIQUES
technique SurfaceTech
{
	pass P0
	{
		VertexShader = compile vs_3_0 SurfaceVS();
		PixelShader  = compile ps_3_0 SurfacePS();
	}
}

technique SurfaceNoShadowTech
{
	pass P0
	{
		VertexShader = compile vs_2_0 SurfaceNoShadowVS();
		PixelShader  = compile ps_2_0 SurfaceNoShadowPS();
	}
}

technique SurfaceOcclusionTech
{
	pass P0
	{
		VertexShader = compile vs_2_0 SurfaceOcclusionVS();
		PixelShader  = compile ps_2_0 SurfaceOcclusionPS();
	}
}