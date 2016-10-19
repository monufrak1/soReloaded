// Terrain.fx

#include "Lighthelper.fx"

// VARIABLES
float g_TextureScale;

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
float g_DeepWaterFogDistance;
float4 g_WaterColor;

float  g_FogStart;
float  g_FogRange;
float4 g_SkyColor;
float  g_DarkSkyOffset;

// TEXTURES
Texture2D g_LowLevelTexture;
Texture2D g_HighLevelTexture;
Texture2D g_ShadowMap;

sampler g_LowLevelTextureSampler = sampler_state
{
	texture = <g_LowLevelTexture>;
	minfilter = LINEAR;
	magfilter = LINEAR;
	mipfilter = LINEAR;
	AddressU = WRAP;
	AddressV = WRAP;
};

sampler g_HighLevelTextureSampler = sampler_state
{
	texture = <g_HighLevelTexture>;
	minfilter = LINEAR;
	magfilter = LINEAR;
	mipfilter = LINEAR;
	AddressU = WRAP;
	AddressV = WRAP;
};

sampler g_ShadowMapSampler = sampler_state
{
	texture = <g_ShadowMap>;
	minfilter = POINT;
	magfilter = POINT;
	mipfilter = POINT;
	AddressU = WRAP;
	AddressV = WRAP;
};

// SHADER I/O STRUCTS
struct TerrainVS_IN
{
	float3 posL  : POSITION;
	float3 normL : NORMAL;
	float2 tex   : TEXCOORD;
};

struct TerrainPS_IN
{
	float4 posH  : SV_POSITION;
	float3 posW  : TEXCOORD0;
	float3 normW : TEXCOORD1;
	float2 tex   : TEXCOORD2;
	float  grade : TEXCOORD3;
	float4 lightPos : TEXCOORD4;
};

struct TerrainNoShadowPS_IN
{
	float4 posH  : SV_POSITION;
	float3 posW  : TEXCOORD0;
	float3 normW : TEXCOORD1;
	float2 tex   : TEXCOORD2;
	float  grade : TEXCOORD3;
};

struct TerrainReflectionPS_IN
{
	float4 posH  : SV_POSITION;
	float3 posW  : TEXCOORD0;
	float3 normW : TEXCOORD1;
	float2 tex   : TEXCOORD2;
	float  grade : TEXCOORD3;
	float  clip  : TEXCOORD4;
};

struct TerrainOcclusionPS_IN
{
	float4 posH : SV_POSITION;
};

// VERTEX SHADERS
TerrainPS_IN TerrainVS(TerrainVS_IN vIn)
{
	TerrainPS_IN vOut;

	float4x4 VP = mul(g_View, g_Projection);

	// Input coordinates are in world space
	vOut.posH  = mul(float4(vIn.posL, 1.0f), VP);
	vOut.posW  = vIn.posL;
	vOut.normW = vIn.normL;
	vOut.tex = vIn.tex;

	// Calculate texture blending based on grade of terrain
	vOut.grade = saturate(dot(vOut.normW, float3(vOut.normW.x, 0.0f, vOut.normW.z)));

	// Output shadow coordinates
	float4x4 lightVP = mul(g_LightView, g_LightProjection);
	vOut.lightPos = mul(float4(vIn.posL, 1.0f), lightVP);

	return vOut;
} // end TerrainVS

TerrainNoShadowPS_IN TerrainNoShadowVS(TerrainVS_IN vIn)
{
	TerrainNoShadowPS_IN vOut;

	float4x4 VP = mul(g_View, g_Projection);

	// Input coordinates are in world space
	vOut.posH  = mul(float4(vIn.posL, 1.0f), VP);
	vOut.posW  = vIn.posL;
	vOut.normW = vIn.normL;
	vOut.tex   = vIn.tex;

	// Calculate texture blending based on grade of terrain
	vOut.grade = saturate(dot(vOut.normW, float3(vOut.normW.x, 0.0f, vOut.normW.z)));

	return vOut;
} // end TerrainNoShadowVS

TerrainReflectionPS_IN TerrainReflectionVS(TerrainVS_IN vIn)
{
	TerrainReflectionPS_IN vOut;

	float4x4 ReflVP = mul(g_ReflectionView, g_ReflectionProjection);

	// Input coordinates are in world space
	vOut.posH  = mul(float4(vIn.posL, 1.0f), ReflVP);
	vOut.posW  = vIn.posL;
	vOut.normW = vIn.normL;
	vOut.tex   = vIn.tex;

	// Calculate texture blending based on grade of terrain
	vOut.grade = saturate(dot(vOut.normW, float3(vOut.normW.x, 0.0f, vOut.normW.z)));

	// Create clip plane at water height
	float4 clipPlane = float4(0.0f, 1.0f, 0.0f, -g_WaterHeight);
	vOut.clip = dot(float4(vOut.posW, 1.0f), clipPlane);

	return vOut;
} // end TerrainReflectionVS

TerrainOcclusionPS_IN TerrainOcclusionVS(TerrainVS_IN vIn)
{
	TerrainOcclusionPS_IN vOut;

	float4x4 VP = mul(g_View, g_Projection);

	vOut.posH = mul(float4(vIn.posL, 1.0f), VP);

	return vOut;
}

// PIXEL SHADERS
float4 TerrainPS(TerrainPS_IN pIn) : SV_TARGET
{
	// Adjust texture scale based on distance
	float dist = distance(g_EyePosW, pIn.posW);
	if(dist >= TEXTURE_SCALE_FAR)
	{
		pIn.tex = pIn.tex * g_TextureScale * 0.25f;
	}
	else if(dist >= TEXTURE_SCALE_CLOSE)
	{
		pIn.tex = pIn.tex * g_TextureScale * 0.75f;
	}
	else
	{
		pIn.tex = pIn.tex * g_TextureScale;
	}

	float4 color      = tex2D(g_LowLevelTextureSampler, pIn.tex);
	float4 steepColor = tex2D(g_HighLevelTextureSampler, pIn.tex);
	
	// Combine with steep terrain color
	color = lerp(color, steepColor, pIn.grade);
	
	// Light the pixel
	SurfaceInfo v = {pIn.posW, pIn.normW, color, float4(0.0f, 0.0f, 0.0f, 0.0f)};
	Light light = {g_LightDir, g_LightAmbient, g_LightDiffuse, g_LightSpecular};

	float shadowFactor = CalcShadowFactor(pIn.lightPos, g_ShadowMapSampler, 
							distance(pIn.posW, g_EyePosW));
	float3 litColor = DirectionalLight(v, light, g_EyePosW, shadowFactor);
	
	if(pIn.posW.y < g_WaterHeight)
	{
		// Add underwater color
		float waterDist = g_WaterHeight - pIn.posW.y;
		
		float waterFog = min(saturate((waterDist/g_DeepWaterFogDistance)), 0.95f);
		litColor = lerp(float4(litColor, 1.0f), g_WaterColor, waterFog).rgb;
	}
	else
	{
		// Calculate fog
		float3 toEye = pIn.posW - g_EyePosW;
		toEye = normalize(toEye);

		float fogLerp = saturate((dist - g_FogStart) / g_FogRange);
		float4 darkSkyColor = float4(g_SkyColor.rgb * g_DarkSkyOffset, 1.0f);
		float4 fogColor = lerp(g_SkyColor, darkSkyColor, 
				saturate(dot(float3(0.0f, 1.0f, 0.0f), toEye) + 0.1f));
	
		litColor = lerp(litColor, fogColor, fogLerp);
	}
	
	return float4(litColor, 1.0f);
} // end TerrainPS

float4 TerrainNoShadowPS(TerrainNoShadowPS_IN pIn) : SV_TARGET
{
	// Adjust texture scale based on distance
	float dist = distance(g_EyePosW, pIn.posW);
	if(dist >= TEXTURE_SCALE_FAR)
	{
		pIn.tex = pIn.tex * g_TextureScale * 0.25f;
	}
	else if(dist >= TEXTURE_SCALE_CLOSE)
	{
		pIn.tex = pIn.tex * g_TextureScale * 0.75f;
	}
	else
	{
		pIn.tex = pIn.tex * g_TextureScale;
	}

	float4 color      = tex2D(g_LowLevelTextureSampler, pIn.tex);
	float4 steepColor = tex2D(g_HighLevelTextureSampler, pIn.tex);
	
	// Combine with steep terrain color
	color = lerp(color, steepColor, pIn.grade);
	
	// Light the pixel
	SurfaceInfo v = {pIn.posW, pIn.normW, color, float4(0.0f, 0.0f, 0.0f, 0.0f)};
	Light light = {g_LightDir, g_LightAmbient, g_LightDiffuse, g_LightSpecular};

	float3 litColor = DirectionalLight(v, light, g_EyePosW, 1.0f);
	
	if(pIn.posW.y < g_WaterHeight)
	{
		// Add underwater color
		float waterDist = g_WaterHeight - pIn.posW.y;
		
		float waterFog = min(saturate((waterDist/g_DeepWaterFogDistance)), 0.95f);
		litColor = lerp(float4(litColor, 1.0f), g_WaterColor, waterFog).rgb;
	}
	else
	{
		// Calculate fog
		float3 toEye = pIn.posW - g_EyePosW;
		toEye = normalize(toEye);

		float fogLerp = saturate((dist - g_FogStart) / g_FogRange);
		float4 darkSkyColor = float4(g_SkyColor.rgb * g_DarkSkyOffset, 1.0f);
		float4 fogColor = lerp(g_SkyColor, darkSkyColor, 
				saturate(dot(float3(0.0f, 1.0f, 0.0f), toEye) + 0.1f));
	
		litColor = lerp(litColor, fogColor, fogLerp);
	}
	
	return float4(litColor, 1.0f);
} // end TerrainNoShadowPS

float4 TerrainReflectionPS(TerrainReflectionPS_IN pIn) : SV_TARGET
{
	// Clip pixels below water surface
	clip(pIn.clip);
	if(g_EyePosW.y < g_WaterHeight) clip(-1.0f);

	float4 color      = tex2D(g_LowLevelTextureSampler, pIn.tex);
	float4 steepColor = tex2D(g_HighLevelTextureSampler, pIn.tex);

	// Combine with steep terrain color
	color = lerp(color, steepColor, pIn.grade);

	SurfaceInfo v = {pIn.posW, pIn.normW, color, float4(0.0f, 0.0f, 0.0f, 0.0f)};
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
	
	return float4(litColor, color.a);
} // end TerrainReflectionPS

float4 TerrainOcclusionPS(TerrainOcclusionPS_IN pIn) : SV_TARGET
{
	// Output occlusion as black
	return float4(0.0f, 0.0f, 0.0f, 1.0f);
} // end TerrainOcclusionPS

// TECHNIQUES
technique TerrainTech
{
	pass P0
	{
		VertexShader = compile vs_3_0 TerrainVS();
		PixelShader  = compile ps_3_0 TerrainPS();
	}
}

technique TerrainNoShadowTech
{
	pass P0
	{
		VertexShader = compile vs_2_0 TerrainNoShadowVS();
		PixelShader  = compile ps_2_0 TerrainNoShadowPS();
	}
}

technique TerrainReflectionTech
{
	pass P0
	{
		VertexShader = compile vs_2_0 TerrainReflectionVS();
		PixelShader  = compile ps_2_0 TerrainReflectionPS();
	}
}

technique TerrainOcclusionTech
{
	pass P0
	{
		VertexShader = compile vs_2_0 TerrainOcclusionVS();
		PixelShader  = compile ps_2_0 TerrainOcclusionPS();
	}
}
