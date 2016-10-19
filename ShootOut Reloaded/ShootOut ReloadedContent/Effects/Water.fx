// Water.fx

#include "Lighthelper.fx"

// VARIABLES
float g_Time;
float4x4 g_View;
float4x4 g_Projection;
float4x4 g_ReflectionView;
float4x4 g_ReflectionProjection;
float3   g_EyePosW;

float3 g_LightDir;
float4 g_LightAmbient;
float4 g_LightDiffuse;
float4 g_LightSpecular;

float  g_WaterHeight;
float4 g_WaterColor;

float  g_FogStart;
float  g_FogRange;
float4 g_SkyColor;
float  g_DarkSkyOffset;

float g_TransparencyRatio;
float g_ReflAmount;
float g_RefrAmount;
float g_WaveHeight;
float g_WaveSpeed;

// TEXTURES
Texture2D g_WaterNormalMap;
Texture2D g_ReflectionMap;
Texture2D g_RefractionMap;

sampler g_WaterNormalSampler = sampler_state
{
	texture = <g_WaterNormalMap>;
	minfilter = LINEAR;
	magfilter = LINEAR;
	mipfilter = LINEAR;
	AddressU = WRAP;
	AddressV = WRAP;
};

sampler g_ReflectionMapSampler = sampler_state
{
	texture = <g_ReflectionMap>;
	minfilter = LINEAR;
	magfilter = LINEAR;
	mipfilter = LINEAR;
	AddressU = CLAMP;
	AddressV = CLAMP;
};

sampler g_RefractionMapSampler = sampler_state
{
	texture = <g_RefractionMap>;
	minfilter = LINEAR;
	magfilter = LINEAR;
	mipfilter = LINEAR;
	AddressU = CLAMP;
	AddressV = CLAMP;
};

// SHADER I/O SRUCTS
struct WaterVS_IN
{
	float3 posL  : POSITION;
	float3 tangL : TANGENT;
	float3 normL : NORMAL;
	float2 tex   : TEXCOORD;
};

struct WaterPS_IN
{
	float4 posH  : SV_POSITION;
	float3 posW  : TEXCOORD0;
	float3 tangW : TEXCOORD1;
	float3 normW : TEXCOORD2;
	float2 tex   : TEXCOORD3;
	float4 reflTex : TEXCOORD4;
	float4 refrTex : TEXCOORD5;
};

// VERTEX SHADER
WaterPS_IN WaterVS(WaterVS_IN vIn)
{
	WaterPS_IN vOut;

	// Calculate wave offset
	float heightOffset = 0.0f;
	if(g_WaveHeight > 0.0f)
	{
		float xWave = cos(g_Time * g_WaveSpeed + vIn.tex.x);
		float zWave = sin(g_Time * g_WaveSpeed + vIn.tex.y);
		float waveResult = xWave * zWave; 
		heightOffset = (g_WaveHeight/2) + (waveResult * g_WaveHeight);
	}

	// Create world matrix from water height
	float4x4 world;
	world[0] = float4(1.0f, 0.0f, 0.0f, 0.0f);
	world[1] = float4(0.0f, 1.0f, 0.0f, 0.0f);
	world[2] = float4(0.0f, 0.0f, 1.0f, 0.0f);
	world[3] = float4(0.0f, g_WaterHeight + heightOffset, 0.0f, 1.0f);

	float4x4 WVP = mul(world, g_View);
	WVP = mul(WVP, g_Projection);

	vOut.posH  = mul(float4(vIn.posL, 1.0f), WVP);
	vOut.posW  = mul(float4(vIn.posL, 1.0f), world);
	vOut.tangW = mul(float4(vIn.tangL, 0.0f), world);
	vOut.normW = mul(float4(vIn.normL, 0.0f), world);
	vOut.tex = vIn.tex;

	// Output reflection and refraction tex-coords
	vOut.reflTex = mul(float4(vIn.posL, 1.0f), world);
	vOut.reflTex = mul(vOut.reflTex, g_ReflectionView);
	vOut.reflTex = mul(vOut.reflTex, g_ReflectionProjection);

	vOut.refrTex = vOut.posH;

	return vOut;
} // end WaterVS

// PIXEL SHADER
float4 WaterPS(WaterPS_IN pIn) : SV_TARGET
{
	float dist = distance(g_EyePosW, pIn.posW);
	if(dist > g_FogStart + g_FogRange)
	{
		return g_SkyColor;
	}

	// Get normal from normal map
	float texScrollSpeed = g_WaveHeight + g_WaveSpeed;
	float3 norm1 = tex2D(g_WaterNormalSampler, pIn.tex + (g_Time * 0.05f) * texScrollSpeed).xzy;  // Flip Y and Z components
	float3 norm2 = tex2D(g_WaterNormalSampler, float2(pIn.tex.x, pIn.tex.y + (g_Time * 0.07f) * texScrollSpeed)).xzy;

	float3 norm  = 2.0f * norm1 - 1.0f;
	norm += 2.0f * norm2 - 1.0f;

	float3 lightNorm = 2.0f * norm1.xzy - 1.0f;
	lightNorm += 2.0f * norm2.xzy - 1.0f;

	// Rotate the light normal
	if(g_WaveHeight > 0.0f && dist < g_FogRange)
	{
		float xWave = cos(g_Time * g_WaveSpeed + pIn.tex.x);
		float zWave = sin(g_Time * g_WaveSpeed + pIn.tex.y);
		float waveResult = xWave * zWave; 

		float3x3 rotX;
		rotX[0] = float3(1.0f, 0.0f, 0.0f);
		rotX[1] = float3(0.0f, cos(waveResult), -sin(waveResult));
		rotX[2] = float3(0.0f, sin(waveResult), cos(waveResult));

		float3x3 rotZ;
		rotZ[0] = float3(cos(waveResult), -sin(waveResult), 0.0f);
		rotZ[1] = float3(sin(waveResult), cos(waveResult), 0.0f);
		rotZ[2] = float3(0.0f, 0.0f, 1.0f);
		lightNorm = normalize(mul(mul(lightNorm, rotX), rotZ));
	}

	float3 T = pIn.tangW;
	float3 N = pIn.normW;
	float3 B = cross(N, T);
	float3x3 TBN = float3x3(T, B, N);

	float3 finalNorm = normalize(mul(norm, TBN));
	lightNorm = normalize(mul(lightNorm, TBN));

	// Get reflection color
	float4 reflColor = float4(0.0f, 0.0f, 0.0f, 0.0f);
	if(g_ReflAmount > 0.0f)
	{
		pIn.reflTex.xy /= pIn.reflTex.w;
		pIn.reflTex.x = 0.5f * pIn.reflTex.x + 0.5f;
		pIn.reflTex.y = -0.5f * pIn.reflTex.y + 0.5f;
		pIn.reflTex.xy += finalNorm.xy * g_ReflAmount;
		reflColor = tex2D(g_ReflectionMapSampler, pIn.reflTex.xy);
	}
	
	// Get refraction color
	pIn.refrTex.xy /= pIn.refrTex.w;
	pIn.refrTex.x = 0.5f * pIn.refrTex.x + 0.5f;
	pIn.refrTex.y = -0.5f * pIn.refrTex.y + 0.5f;
	pIn.refrTex.xy += finalNorm.xy * g_RefrAmount;
	float4 refrColor = tex2D(g_RefractionMapSampler, pIn.refrTex.xy);

	// Combine reflection and refraction
	float waterFog = min(saturate(dist / 50.0f), g_TransparencyRatio);

	float4 finalColor = lerp(refrColor, reflColor, waterFog);

	// Add lighting
	if(g_ReflAmount > 0.0f)
	{
		SurfaceInfo v = {pIn.posW, lightNorm, finalColor, float4(1.0f, 1.0f, 1.0f, 256.0f)};
		Light light = {g_LightDir, g_LightAmbient, g_LightDiffuse, g_LightSpecular};
		float3 litColor = CalcSpecTerm(v, light, g_EyePosW);

		finalColor.rgb += litColor;
	}

	// Add sky fog
	float3 toEye = normalize(pIn.posW - g_EyePosW);
	float fogLerp = saturate((dist - g_FogStart) / g_FogRange);
	float4 darkSkyColor = float4(g_SkyColor.rgb * g_DarkSkyOffset, 1.0f);
	float4 fogColor = lerp(g_SkyColor, darkSkyColor, 
			saturate(dot(float3(0.0f, 1.0f, 0.0f), toEye) + 0.1f));

	finalColor = lerp(finalColor, fogColor, fogLerp);
	finalColor.a = 1.0f;

	return finalColor;
} // end WaterPS

// TECHNIQUE
technique WaterTech
{
	pass P0
	{
		VertexShader = compile vs_3_0 WaterVS();
		PixelShader  = compile ps_3_0 WaterPS();
	}
}