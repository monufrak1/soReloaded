// Light.fx

// VARIALBES
float4x4 g_View;
float4x4 g_Projection;

float4 g_LightDiffuse;

float4x4 g_World;
float    g_Size;

// TEXTURES
Texture2D g_LightTexture;

sampler g_LightTextureSampler = sampler_state
{
	texture = <g_LightTexture>;
	minfilter = LINEAR;
	magfilter = LINEAR;
	mipfilter = LINEAR;
	AddressU  = WRAP;
	AddressV  = WRAP;
};

// SHADER I/O STRUCTS
struct LightVS_IN
{
	float3 posL : POSITION;
	float2 tex  : TEXCOORD;
};

struct LightPS_IN
{
	float4 posH : SV_POSITION;
	float2 tex  : TEXCOORD;
};

// VERTEX SHADER
LightPS_IN LightVS(LightVS_IN vIn)
{
	LightPS_IN vOut;

	// g_World matrix represents a camera facing billboard
	float4x4 WVP = mul(g_World, g_View);
	WVP = mul(WVP, g_Projection);

	vOut.posH = mul(float4(vIn.posL * g_Size, 1.0f), WVP);
	vOut.tex  = vIn.tex;

	return vOut;
} // end LightVS

// PIXEL SHADER
float4 LightPS(LightPS_IN pIn) : SV_TARGET
{
	float4 diffuse = tex2D(g_LightTextureSampler, pIn.tex);

	clip(diffuse.a - 0.1f);

	// Alpha channel is 0 because light does not occlude
	return float4(g_LightDiffuse.rgb, 0.0f) * diffuse;
} // end LightPS

// TECHNIQUE
technique LightTech
{
	pass P0
	{
		VertexShader = compile vs_2_0 LightVS();
		PixelShader  = compile ps_2_0 LightPS();
	}
}