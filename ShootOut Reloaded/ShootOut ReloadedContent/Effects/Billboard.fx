// Billboard.fx

#include "Lighthelper.fx"

#define ALPHA_MASK 0.5f

// VARIABLES
float4x4 g_World;

float4x4 g_View;
float4x4 g_Projection;
float4x4 g_ReflectionView;
float4x4 g_ReflectionProjection;
float3   g_EyePosW;

float3 g_LightDir;
float4 g_LightAmbient;
float4 g_LightDiffuse;
float4 g_LightSpecular;

float  g_FogStart;
float  g_FogRange;
float4 g_SkyColor;
float  g_DarkSkyOffset;

// TEXTURES
Texture2D g_BillboardTexture;
sampler g_BillboardTextureSampler = sampler_state
{
	texture = <g_BillboardTexture>;
	minfilter = LINEAR;
	magfilter = LINEAR;
	mipfilter = LINEAR;
	addressU = WRAP;
	addressV = WRAP;
};

// SHADER I/O STRUCTS
struct BillboardVS_IN
{
	float3 posL : POSITION;
	float2 tex  : TEXCOORD;
};

struct BillboardPS_IN
{
	float4 posH : SV_POSITION;
	float3 posW : TEXCOORD0;
	float2 tex  : TEXCOORD1;
};


// VERTEX SHADERS
BillboardPS_IN BillboardVS(BillboardVS_IN vIn)
{
	BillboardPS_IN vOut;

	float4x4 WVP = mul(g_World, g_View);
	WVP = mul(WVP, g_Projection);

	vOut.posH = mul(float4(vIn.posL, 1.0f), WVP);
	vOut.posW = mul(float4(vIn.posL, 1.0f), g_World);

	vOut.tex = vIn.tex;

	return vOut;
} // end BillboardVS

// PIXEL SHADERS
float4 BillboardPS(BillboardPS_IN pIn) : SV_TARGET
{
	float4 color = tex2D(g_BillboardTextureSampler, pIn.tex);

	clip(color.a - ALPHA_MASK);

	return color;
} // end BillboardPS

float4 ParticlePS(BillboardPS_IN pIn) : SV_TARGET
{
	float4 color = tex2D(g_BillboardTextureSampler, pIn.tex);

	clip(color.a);

	return color;
} // end ParticlePS

float4 BillboardLightingPS(BillboardPS_IN pIn) : SV_TARGET
{
	float4 color = tex2D(g_BillboardTextureSampler, pIn.tex);
	clip(color.a - ALPHA_MASK);

	// Light the pixel
	SurfaceInfo v = {pIn.posW, float3(0.0f, 1.0f, 0.0f), color, float4(0.0f, 0.0f, 0.0f, 0.0f)};
	Light light = {g_LightDir, g_LightAmbient, g_LightDiffuse, g_LightSpecular};

	float3 litColor = DirectionalLight(v, light, g_EyePosW, 1.0f);

	// Calculate fog
	float3 toEye = pIn.posW - g_EyePosW;
	float dist = length(toEye);
	toEye = normalize(toEye);

	float fogLerp = saturate((dist - g_FogStart) / g_FogRange);
	float4 darkSkyColor = float4(g_SkyColor.rgb * g_DarkSkyOffset, 1.0f);
	float4 fogColor = lerp(g_SkyColor, darkSkyColor, 
			saturate(dot(float3(0.0f, 1.0f, 0.0f), toEye) + 0.1f));
	
	litColor = lerp(litColor, fogColor, fogLerp);

	return float4(litColor, 1.0f);
} // end BillboardLightingPS

float4 BillboardOcclusionPS(BillboardPS_IN pIn) : SV_TARGET
{
	clip(tex2D(g_BillboardTextureSampler, pIn.tex).a - ALPHA_MASK);

	return float4(0.0f, 0.0f, 0.0f, 1.0f);
} // end BillboardOcclusionPS

// TECHNIQUES
technique BillboardTech
{
	pass P0
	{
		VertexShader = compile vs_2_0 BillboardVS();
		PixelShader  = compile ps_2_0 BillboardPS();
	}
}

technique ParticleTech
{
	pass P0
	{
		VertexShader = compile vs_2_0 BillboardVS();
		PixelShader  = compile ps_2_0 ParticlePS();
	}
}

technique BillboardLightingTech
{
	pass P0
	{
		VertexShader = compile vs_2_0 BillboardVS();
		PixelShader  = compile ps_2_0 BillboardLightingPS();
	}
}

technique BillboardOcclusionTech
{
	pass P0
	{
		VertexShader = compile vs_2_0 BillboardVS();
		PixelShader  = compile ps_2_0 BillboardOcclusionPS();
	}
}	