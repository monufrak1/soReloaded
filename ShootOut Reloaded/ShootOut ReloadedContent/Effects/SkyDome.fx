// SkyDome.fx

#include "Lighthelper.fx"

// VARIABLES
float g_TextureScale;

float    g_Time;
float4x4 g_View;
float4x4 g_Projection;
float4x4 g_ReflView;
float4x4 g_ReflProjection;
float3   g_EyePosW;

float3 g_LightDir;
float4 g_LightAmbient;
float4 g_LightDiffuse;
float4 g_LightSpecular;

float4 g_SkyColor;
float  g_DarkSkyOffset;

// TEXTURES
Texture2D g_SkyTexture;
sampler g_SkyTextureSampler = sampler_state
{
	texture = <g_SkyTexture>;
	minfilter = LINEAR;
	magfilter = LINEAR;
	mipfilter = LINEAR;
	addressU = WRAP;
	addressV = WRAP;
};

// SHADER I/O STRUCTS
struct SkyVS_IN
{
	float3 posL : POSITION;
};

struct SkyTexturedVS_IN
{
	float3 posL : POSITION;
	float2 tex  : TEXCOORD;
};

struct SkyPS_IN
{
	float4 posH : SV_POSITION;
	float3 tex  : TEXCOORD0;
};

struct SkyTexturedPS_IN
{
	float4 posH : SV_POSITION;
	float3 posW : TEXCOORD0;
	float3 dirW : TEXCOORD1;
	float2 tex  : TEXCOORD2;
};

// VERTEX SHADERS
SkyPS_IN SkyVS(SkyVS_IN vIn)
{
	SkyPS_IN vOut;

	float4x4 world;
	world[0] = float4(1.0f, 0.0f, 0.0f, 0.0f);
	world[1] = float4(0.0f, 1.0f, 0.0f, 0.0f);
	world[2] = float4(0.0f, 0.0f, 1.0f, 0.0f);
	world[3] = float4(g_EyePosW,        1.0f);

	float4x4 WVP = mul(world, g_View);
	WVP = mul(WVP, g_Projection);

	vOut.posH = mul(float4(vIn.posL, 1.0f), WVP);
	vOut.tex = normalize(vIn.posL);

	return vOut;
} // end SkyVS

SkyTexturedPS_IN SkyTexturedVS(SkyTexturedVS_IN vIn)
{
	SkyTexturedPS_IN vOut;

	float windSpeed = 0.01f;

	float4x4 world;
	world[0] = float4(cos(g_Time*windSpeed),  0.0f, sin(g_Time*windSpeed), 0.0f);
	world[1] = float4(0.0f, 1.0f,   0.0f, 0.0f);
	world[2] = float4(-sin(g_Time*windSpeed), 0.0f, cos(g_Time*windSpeed), 0.0f);
	world[3] = float4(g_EyePosW,        1.0f);

	float4x4 WVP = mul(world, g_View);
	WVP = mul(WVP, g_Projection);

	vOut.posH = mul(float4(vIn.posL, 1.0f), WVP);
	vOut.dirW = normalize(vIn.posL);
	vOut.posW = mul(float4(vOut.dirW, 0.0f), world);
	vOut.tex = vIn.tex * g_TextureScale;

	return vOut;
} // end SkyTexturedVS

SkyTexturedPS_IN SkyTexturedReflectionVS(SkyTexturedVS_IN vIn)
{
	SkyTexturedPS_IN vOut;

	float windSpeed = 0.01f;

	float4x4 world;
	world[0] = float4(cos(g_Time*windSpeed),  0.0f, sin(g_Time*windSpeed), 0.0f);
	world[1] = float4(0.0f, 1.0f,   0.0f, 0.0f);
	world[2] = float4(-sin(g_Time*windSpeed), 0.0f, cos(g_Time*windSpeed), 0.0f);
	world[3] = float4(g_EyePosW,        1.0f);

	float4x4 WVP = mul(world, g_ReflView);
	WVP = mul(WVP, g_ReflProjection);

	vOut.posH = mul(float4(vIn.posL, 1.0f), WVP);
	vOut.dirW = normalize(vIn.posL);
	vOut.posW = mul(float4(vOut.dirW, 0.0f), world);
	vOut.tex = vIn.tex * g_TextureScale;

	return vOut;
} // end SkyTexturedReflectionVS

// PIXEL SHADERS
float4 SkyPS(SkyPS_IN pIn) : SV_TARGET
{
	float4 darkSkyColor = g_SkyColor * g_DarkSkyOffset;
	darkSkyColor.a = 1.0f;

	return lerp(g_SkyColor, darkSkyColor,
		saturate(dot(float3(0.0f, 1.0f, 0.0f), pIn.tex.xyz) + 0.1f));
} // end SkyPS

float4 SkyReflectionPS(SkyPS_IN pIn) : SV_TARGET
{
	float4 darkSkyColor = g_SkyColor * g_DarkSkyOffset;
	darkSkyColor.a = 1.0f;

	return lerp(g_SkyColor, darkSkyColor,
		saturate(dot(float3(0.0f, -1.0f, 0.0f), pIn.tex.xyz) + 0.1f));
} // end SkyReflectionPS

float4 SkyTexturedPS(SkyTexturedPS_IN pIn) : SV_TARGET
{
	float4 texColor = tex2D(g_SkyTextureSampler, pIn.tex);
	
	float4 darkSkyColor = g_SkyColor * g_DarkSkyOffset;
	darkSkyColor.a = 1.0f;

	float4 skyColor = lerp(g_SkyColor, darkSkyColor,
						saturate(dot(float3(0.0f, 1.0f, 0.0f), pIn.dirW.xyz) + 0.1f));

	if(pIn.dirW.y < 0.995f)
	{
		return lerp(skyColor, texColor,
			saturate(dot(float3(0.0f, 1.0f, 0.0f), pIn.dirW.xyz) - 0.25f));
	}

	return skyColor;
}  // end SkyTexturedPS

float4 SkyTexturedCloudsPS(SkyTexturedPS_IN pIn) : SV_TARGET
{
	float4 texColor = tex2D(g_SkyTextureSampler, pIn.tex);
	
	float4 darkSkyColor = g_SkyColor * g_DarkSkyOffset;
	darkSkyColor.a = 1.0f;

	float4 skyColor = lerp(g_SkyColor, darkSkyColor,
						saturate(dot(float3(0.0f, 1.0f, 0.0f), pIn.dirW.xyz) + 0.1f));

	if(texColor.a - 0.45f > 0.0f && pIn.dirW.y < 0.995f)
	{
		SurfaceInfo v = {pIn.posW, pIn.posW, texColor, float4(0.0f, 0.0f, 0.0f, 0.0f)};
		Light light = {g_LightDir, g_LightAmbient * 1.25f, g_LightDiffuse, g_LightSpecular};

		float3 litColor = DirectionalLight(v, light, g_EyePosW, 1.0f);

		skyColor = lerp(skyColor, float4(litColor, texColor.a),
				saturate(dot(float3(0.0f, 1.0f, 0.0f), pIn.dirW.xyz) - 0.25f));

		// Clouds have occlusion
		skyColor.a = 1.0f;
		return skyColor;
	}

	// No cloud section, no Occlusion
	skyColor.a = 0.0f;

	return skyColor;
} // end SkyTexturedCloudsPS

float4 SkyTexturedOcclusionPS(SkyTexturedPS_IN pIn) : SV_TARGET
{
	float4 texColor = tex2D(g_SkyTextureSampler, pIn.tex);
	
	clip(texColor.a - 0.45f);
	clip((saturate(dot(float3(0.0f, 1.0f, 0.0f), pIn.dirW.xyz) - 0.25f)) - 0.05f);

	return float4(0.0f, 0.0f, 0.0f, 1.0f);
} // end SkyTexturedOcclusionPS

// TECHNIQUE
technique SkyTech
{
	pass P0
	{
		VertexShader = compile vs_2_0 SkyVS();
		PixelShader  = compile ps_2_0 SkyPS();
	}
}

technique SkyReflectionTech
{
	pass P0
	{
		VertexShader = compile vs_2_0 SkyVS();
		PixelShader  = compile ps_2_0 SkyReflectionPS();
	}
}

technique SkyTexturedTech
{
	pass P0
	{
		VertexShader = compile vs_2_0 SkyTexturedVS();
		PixelShader  = compile ps_2_0 SkyTexturedPS();
	}
}

technique SkyTexturedCloudsTech
{
	pass P0
	{
		VertexShader = compile vs_2_0 SkyTexturedVS();
		PixelShader  = compile ps_2_0 SkyTexturedCloudsPS();
	}
}

technique SkyTexturedCloudsReflectionTech
{
	pass P0
	{
		VertexShader = compile vs_2_0 SkyTexturedReflectionVS();
		PixelShader  = compile ps_2_0 SkyTexturedCloudsPS();
	}
}

technique SkyTexturedOcclusionTech
{
	pass P0
	{
		VertexShader = compile vs_2_0 SkyTexturedVS();
		PixelShader  = compile ps_2_0 SkyTexturedOcclusionPS();
	}
}