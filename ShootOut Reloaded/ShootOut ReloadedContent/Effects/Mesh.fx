// Mesh.fx

#include "Lighthelper.fx"

// VARIABLES
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

float4x4 g_World;

bool   g_UseTextures;
float4 g_DiffuseMaterial;
float4 g_SpecularMaterial;

// TEXTURES
Texture2D g_DiffMap;
Texture2D g_NormMap;
Texture2D g_SpecMap;
Texture2D g_ShadowMap;

sampler g_DiffMapSampler = sampler_state
{
	texture = <g_DiffMap>;
	minfilter = LINEAR;
	magfilter = LINEAR;
	mipfilter = LINEAR;
	AddressU  = WRAP;
	AddressV  = WRAP;
};

sampler g_NormMapSampler = sampler_state
{
	texture = <g_NormMap>;
	minfilter = LINEAR;
	magfilter = LINEAR;
	mipfilter = LINEAR;
	AddressU  = WRAP;
	AddressV  = WRAP;
};

sampler g_SpecMapSampler = sampler_state
{
	texture = <g_SpecMap>;
	minfilter = LINEAR;
	magfilter = LINEAR;
	mipfilter = LINEAR;
	AddressU  = WRAP;
	AddressV  = WRAP;
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
struct MeshVS_IN
{
	float3 posL	 : POSITION;
	float3 tangL : TANGENT;
	float3 normL : NORMAL;
	float2 tex   : TEXCOORD;
};

struct MeshPS_IN
{
	float4 posH  : SV_POSITION;
	float3 posW  : TEXCOORD0;
	float3 tangW : TEXCOORD1;
	float3 normW : TEXCOORD2;
	float2 tex   : TEXCOORD3;
	float4 lightPos : TEXCOORD4;
};

struct MeshNoShadowPS_IN
{
	float4 posH  : SV_POSITION;
	float3 posW  : TEXCOORD0;
	float3 tangW : TEXCOORD1;
	float3 normW : TEXCOORD2;
	float2 tex   : TEXCOORD3;
};

struct MeshOcclusionPS_IN
{
	float4 posH : SV_POSITION;
	float2 tex  : TEXCOORD0;
}; 

struct MeshShadowPS_IN
{
	float4 posH      : SV_POSITION;
	float4 lightPosH : TEXCOORD0;
	float2 tex       : TEXCOORD1;
};

// VERTEX SHADERS
MeshPS_IN MeshVS(MeshVS_IN vIn)
{
	MeshPS_IN vOut;

	float4x4 WVP = mul(g_World, g_View);
	WVP = mul(WVP, g_Projection);

	// Transform coordinates
	vOut.posH = mul(float4(vIn.posL, 1.0f), WVP);
	
	vOut.posW = mul(float4(vIn.posL, 1.0f), g_World).xyz;
	vOut.tangW = mul(float4(vIn.tangL, 0.0f), g_World).xyz;
	vOut.normW = mul(float4(vIn.normL, 0.0f), g_World).xyz;

	vOut.tex = vIn.tex;

	// Output shadow coordinates
	float4x4 lightWVP = mul(g_World, g_LightView);
	lightWVP = mul(lightWVP, g_LightProjection);

	vOut.lightPos = mul(float4(vIn.posL, 1.0f), lightWVP);

	return vOut;
} // end MeshVS

MeshNoShadowPS_IN MeshNoShadowVS(MeshVS_IN vIn)
{
	MeshNoShadowPS_IN vOut;

	float4x4 WVP = mul(g_World, g_View);
	WVP = mul(WVP, g_Projection);

	// Transform coordinates
	vOut.posH = mul(float4(vIn.posL, 1.0f), WVP);
	
	vOut.posW = mul(float4(vIn.posL, 1.0f), g_World).xyz;
	vOut.tangW = mul(float4(vIn.tangL, 0.0f), g_World).xyz;
	vOut.normW = mul(float4(vIn.normL, 0.0f), g_World).xyz;

	vOut.tex = vIn.tex;

	return vOut;
} // end MeshNoShadowVS

MeshOcclusionPS_IN MeshOcclusionVS(MeshVS_IN vIn)
{
	MeshOcclusionPS_IN vOut;

	float4x4 WVP = mul(g_World, g_View);
	WVP = mul(WVP, g_Projection);

	// Transform coordinates
	vOut.posH = mul(float4(vIn.posL, 1.0f), WVP);
	vOut.tex  = vIn.tex;

	return vOut;
} // end MeshOcclusionVS

MeshShadowPS_IN MeshShadowVS(MeshVS_IN vIn)
{
	MeshShadowPS_IN vOut;

	float4x4 LightWVP = mul(g_World, g_LightView);
	LightWVP = mul(LightWVP, g_LightProjection);

	vOut.posH = mul(float4(vIn.posL, 1.0f), LightWVP);
	vOut.lightPosH = vOut.posH;
	vOut.tex = vIn.tex;

	return vOut;
} // end MeshShadowVS

MeshPS_IN MeshReflectionVS(MeshVS_IN vIn)
{
	MeshPS_IN vOut;

	float4x4 WVP = mul(g_World, g_ReflectionView);
	WVP = mul(WVP, g_ReflectionProjection);

	// Transform coordinates
	vOut.posH = mul(float4(vIn.posL, 1.0f), WVP);
	
	vOut.posW = mul(float4(vIn.posL, 1.0f), g_World).xyz;
	vOut.tangW = float4(0.0f, 0.0f, 0.0f, 0.0f); // Not used. Zero out
	vOut.normW = mul(float4(vIn.normL, 0.0f), g_World).xyz;

	vOut.tex = vIn.tex;

	// Output shadow coordinates
	float4x4 lightWVP = mul(g_World, g_LightView);
	lightWVP = mul(lightWVP, g_LightProjection);

	vOut.lightPos = mul(float4(vIn.posL, 1.0f), lightWVP);

	return vOut;
} // end MeshReflectionVS

// PIXEL SHADERS
float4 MeshPS(MeshPS_IN pIn) : SV_TARGET
{
	float4 diffuse = float4(0.0f, 0.0f, 0.0f, 0.0f);
	float4 specular = float4(0.0f, 0.0f, 0.0f, 0.0f);
	float3 finalNorm = normalize(pIn.normW);

	if(g_UseTextures)
	{
		// Set data from textures
		diffuse = tex2D(g_DiffMapSampler, pIn.tex);
		clip(diffuse.a - 0.2f);

		float3 normal = tex2D(g_NormMapSampler, pIn.tex).xyz;
		specular = tex2D(g_SpecMapSampler, pIn.tex);

		// Transform normal from normal map
		normal = (2.0f * normal) - 1.0f;
		normal = normalize(normal);

		float3 T = normalize(pIn.tangW);
		float3 N = normalize(pIn.normW);
		float3 B = cross(N, T);
		float3x3 TBN = float3x3(T, B, N);

		finalNorm = normalize(mul(normal, TBN));
	}

	// Combine with material data
	diffuse += g_DiffuseMaterial;
	specular += g_SpecularMaterial;
	specular.a *= 256.0f;

	// Light the pixel
	SurfaceInfo v = {pIn.posW, finalNorm, diffuse, specular};
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

	return float4(litColor, 1.0f);
} // end MeshPS

float4 MeshNoShadowPS(MeshNoShadowPS_IN pIn) : SV_TARGET
{
	float4 diffuse = float4(0.0f, 0.0f, 0.0f, 0.0f);
	float4 specular = float4(0.0f, 0.0f, 0.0f, 0.0f);
	float3 finalNorm = normalize(pIn.normW);

	if(g_UseTextures)
	{
		// Set data from textures
		diffuse = tex2D(g_DiffMapSampler, pIn.tex);
		clip(diffuse.a - 0.2f);

		float3 normal = tex2D(g_NormMapSampler, pIn.tex).xyz;
		specular = tex2D(g_SpecMapSampler, pIn.tex);

		// Transform normal from normal map
		normal = (2.0f * normal) - 1.0f;
		normal = normalize(normal);

		float3 T = normalize(pIn.tangW);
		float3 N = normalize(pIn.normW);
		float3 B = cross(N, T);
		float3x3 TBN = float3x3(T, B, N);

		finalNorm = normalize(mul(normal, TBN));
	}

	// Combine with material data
	diffuse += g_DiffuseMaterial;
	specular += g_SpecularMaterial;
	specular.a *= 256.0f;

	// Light the pixel
	SurfaceInfo v = {pIn.posW, finalNorm, diffuse, specular};
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

	return float4(litColor, 1.0f);
} // end MeshNoShadowPS

float4 MeshCheapPS(MeshPS_IN pIn) : SV_TARGET
{
		float4 diffuse = float4(0.0f, 0.0f, 0.0f, 0.0f);
	float4 specular = float4(0.0f, 0.0f, 0.0f, 0.0f);
	float3 finalNorm = normalize(pIn.normW);

	if(g_UseTextures)
	{
		// Set data from textures
		diffuse = tex2D(g_DiffMapSampler, pIn.tex);
		clip(diffuse.a - 0.2f);

		return diffuse;
	}

	// Combine with material data
	diffuse += g_DiffuseMaterial;
	return diffuse;
}

float4 MeshOcclusionPS(MeshOcclusionPS_IN pIn) : SV_TARGET
{
	// Clip transparent pixels
	if(g_UseTextures)
	{
		clip(tex2D(g_DiffMapSampler, pIn.tex).a - 0.1f);
	}

	// Output black as occluder
	return float4(0.0f, 0.0f, 0.0f, 1.0f);
} // end MeshOcclusionPS

float4 MeshShadowPS(MeshShadowPS_IN pIn) : SV_TARGET
{
	// Clip transparent pixels
	if(g_UseTextures)
	{
		clip(tex2D(g_DiffMapSampler, pIn.tex).a - 0.1f);
	}

	// Output the depth information
	float depth = 1.0f - (pIn.lightPosH.z / pIn.lightPosH.w);
	return float4(depth, 0.0f, 0.0f, 1.0f);
}

float4 MeshReflectionPS(MeshPS_IN pIn) : SV_TARGET
{
	float4 diffuse = float4(0.0f, 0.0f, 0.0f, 0.0f);
	float4 specular = float4(0.0f, 0.0f, 0.0f, 0.0f);
	float3 finalNorm = normalize(pIn.normW);

	if(g_UseTextures)
	{
		// Set data from textures
		diffuse = tex2D(g_DiffMapSampler, pIn.tex);
		clip(diffuse.a - 0.2f);
	}

	// Combine with material data
	diffuse += g_DiffuseMaterial;
	specular += g_SpecularMaterial;
	specular.a *= 256.0f;

	// Light the pixel
	SurfaceInfo v = {pIn.posW, finalNorm, diffuse, specular};
	Light light = {g_LightDir, g_LightAmbient, g_LightDiffuse, g_LightSpecular};

	float shadowFactor = CalcShadowFactor(pIn.lightPos, g_ShadowMapSampler, 
							distance(pIn.posW, g_EyePosW));
	float3 litColor = DirectionalLight(v, light, g_EyePosW, shadowFactor);

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
} // end MeshRelfectionPS

// TECHNIQUES
technique MeshTech
{
	pass P0
	{
		VertexShader = compile vs_3_0 MeshVS();
		PixelShader  = compile ps_3_0 MeshPS();

		CullMode = CCW;
	}
}

technique MeshTechNoCull
{
	pass P0
	{
		VertexShader = compile vs_3_0 MeshVS();
		PixelShader  = compile ps_3_0 MeshPS();

		CullMode = NONE;
	}
}

technique MeshNoShadowTech
{
	pass P0
	{
		VertexShader = compile vs_3_0 MeshNoShadowVS();
		PixelShader  = compile ps_3_0 MeshNoShadowPS();
	}
}

technique MeshOcclusionTech
{
	pass P0
	{
		VertexShader = compile vs_2_0 MeshOcclusionVS();
		PixelShader  = compile ps_2_0 MeshOcclusionPS();
	}
}

technique MeshShadowTech
{
	pass P0
	{
		VertexShader = compile vs_3_0 MeshShadowVS();
		PixelShader  = compile ps_3_0 MeshShadowPS();
	}
}

technique MeshReflectionTech
{
	pass P0
	{
		VertexShader = compile vs_3_0 MeshReflectionVS();
		PixelShader  = compile ps_3_0 MeshReflectionPS();
	}
}