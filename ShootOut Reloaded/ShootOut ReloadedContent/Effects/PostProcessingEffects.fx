// PostProcessingEffects.fx

// GLOBAL CONSTANTS
int NUM_SAMPLES = 64;
float Density = 1.0f;
float Weight = 0.03f;
float Decay = 0.99f;
float Exposure = 1.0f;

// VARIABLES
float4x4 g_View;
float4x4 g_Projection;

float3 g_LightPosition;

// TEXTURES
Texture2D g_FrameBuffer;
Texture2D g_LightOcclusionMap;
Texture2D g_BloomMap;

sampler g_FrameBufferSampler = sampler_state
{
	texture = <g_FrameBuffer>;
	minfilter = LINEAR;
	magfilter = LINEAR;
	mipfilter = LINEAR;
	AddressU  = CLAMP;
	AddressV  = CLAMP;
};

sampler g_LightOcclusionMapSampler = sampler_state
{
	texture = <g_LightOcclusionMap>;
	minfilter = LINEAR;
	magfilter = LINEAR;
	mipfilter = LINEAR;
	AddressU  = CLAMP;
	AddressV  = CLAMP;
};

sampler g_BloomMapSampler = sampler_state
{
	texture = <g_BloomMap>;
	minfilter = LINEAR;
	magfilter = LINEAR;
	mipfilter = LINEAR;
	AddressU  = CLAMP;
	AddressV  = CLAMP;
};

// SHADER I/O STRUCTS
struct VS_IN
{
	float3 posL : POSITION;
	float2 tex  : TEXCOORD;
};

struct LightScatteringPS_IN
{
	float4 posH      : SV_POSITION;
	float2 tex       : TEXCOORD0;
	float4 lightPosH : TEXCOORD1;
};

struct BloomPS_IN 
{
	float4 posH : SV_POSITION;
	float2 tex	: TEXCOORD0;
};

struct CopyPS_IN 
{
	float4 posH : SV_POSITION;
	float2 tex	: TEXCOORD0;
};

// VERTEX SHADERS
LightScatteringPS_IN LightScatteringVS(VS_IN vIn)
{
	LightScatteringPS_IN vOut;

	// Input coordinates are in screen space
	vOut.posH = float4(vIn.posL, 1.0f);
	vOut.tex  = vIn.tex;

	// Output light position in screen space
	float4x4 VP = mul(g_View, g_Projection);
	
	vOut.lightPosH = mul(float4(g_LightPosition, 1.0f), VP);
	vOut.lightPosH.xy /= vOut.lightPosH.w;
	vOut.lightPosH.x = 0.5f * vOut.lightPosH.x + 0.5f;
	vOut.lightPosH.y = -0.5f * vOut.lightPosH.y + 0.5f;

	return vOut;
} // end LightScatteringVS

BloomPS_IN BloomVS(VS_IN vIn)
{
	BloomPS_IN vOut;

	// Input position is in screen space
	vOut.posH = float4(vIn.posL, 1.0f);
	vOut.tex = vIn.tex;

	return vOut;
} // end BloomVS

CopyPS_IN CopyVS(VS_IN vIn)
{
	CopyPS_IN vOut;

	// Input position is in screen space
	vOut.posH = float4(vIn.posL, 1.0f);
	vOut.tex = vIn.tex;

	return vOut;
}

// PIXEL SHADERS
float4 LightScatteringPS(LightScatteringPS_IN pIn) : SV_TARGET
{
	// Calculate vector from pixel to light source in screen space.  
	float2 deltaTexCoord = pIn.lightPosH.xy - pIn.tex;
	 
	// Divide by number of samples and scale by control factor.  
	deltaTexCoord *= 1.0f / NUM_SAMPLES * Density;
	  
    // Store initial sample.  
	float3 color = tex2D(g_FrameBufferSampler, pIn.tex).rgb; 
	 
	// Set up illumination decay factor.  
	float illuminationDecay = 1.0f;    

	for (int i = 0; i < NUM_SAMPLES; i++)  
	{  
		// Step sample location along ray.  
		pIn.tex += deltaTexCoord;  
    
		// Retrieve sample at new location.  
		float3 sample = tex2D(g_LightOcclusionMapSampler, pIn.tex).rgb;
		
		// Apply sample attenuation scale/decay factors.  
		sample *= illuminationDecay * Weight;  
    
		// Accumulate combined color.  
		color += sample; 
     
		// Update exponential decay factor.  
		illuminationDecay *= Decay; 
	} 
   
	// Output final color with a further scale control factor.  
	return float4( color, 1);
} // end LightScatteringPS

float4 LightScatteringPS2(LightScatteringPS_IN pIn) : SV_TARGET
{
	// Calculate vector from pixel to light source in screen space.  
	float2 deltaTexCoord = pIn.lightPosH.xy - pIn.tex;
	 
	// Divide by number of samples and scale by control factor.  
	deltaTexCoord *= 1.0f / NUM_SAMPLES * Density;
	    
	float3 color = float3(0.0f, 0.0f, 0.0f); 
	 
	// Set up illumination decay factor.  
	float illuminationDecay = 1.0f;    

	for (int i = 0; i < NUM_SAMPLES; i++)  
	{  
		// Step sample location along ray.  
		pIn.tex += deltaTexCoord;  
    
		// Retrieve sample at new location.  
		float3 sample = tex2D(g_LightOcclusionMapSampler, pIn.tex).rgb;
		
		// Apply sample attenuation scale/decay factors.  
		sample *= illuminationDecay * Weight;  
    
		// Accumulate combined color.  
		color += sample; 
     
		// Update exponential decay factor.  
		illuminationDecay *= Decay; 
	} 
   
	// Output final color with a further scale control factor.  
	return float4( color, 1);
} // end LightScatteringPS2

float4 LightScatteringPS3(LightScatteringPS_IN pIn) : SV_TARGET
{
	// Calculate vector from pixel to light source in screen space.  
	float2 deltaTexCoord = pIn.lightPosH.xy - pIn.tex;
	 
	// Divide by number of samples and scale by control factor.  
	deltaTexCoord *= 1.0f / NUM_SAMPLES * Density;
	    
	float3 color = float3(0.0f, 0.0f, 0.0f); 
	 
	// Set up illumination decay factor.  
	float illuminationDecay = 1.0f;    

	for (int i = 0; i < NUM_SAMPLES; i++)  
	{  
		// Step sample location along ray.  
		pIn.tex += deltaTexCoord;  
    
		// Retrieve sample at new location.  
		float4 sample = tex2D(g_LightOcclusionMapSampler, pIn.tex);
		if(sample.a > 0.0f)
		{
			// Apply sample attenuation scale/decay factors.  
			sample *= illuminationDecay * Weight;  
    
			// Accumulate combined color.  
			color += sample.rgb; 
		}
     
		// Update exponential decay factor.  
		illuminationDecay *= Decay; 
	} 
   
	// Output final color with a further scale control factor.  
	return float4( color, 1);
} // end LightScatteringPS3

float4 BloomPS(BloomPS_IN pIn) : SV_TARGET
{
	float3 frameSample = tex2D(g_FrameBufferSampler, pIn.tex).rgb;
	float3 bloomSample = tex2D(g_BloomMapSampler, pIn.tex).rgb * Exposure;

	// Add color from the from the Bloom Map
	return float4(frameSample + bloomSample, 1.0f);
}

float4 CopyPS(CopyPS_IN pIn) : SV_TARGET
{
	return tex2D(g_FrameBufferSampler, pIn.tex);
}

float4 CopyOcclusionPS(CopyPS_IN pIn) : SV_TARGET
{
	float4 sample = tex2D(g_FrameBufferSampler, pIn.tex);

	// Clip if near zero alpha channel
	clip(sample.a - 0.1f);

	return float4(0.0f, 0.0f, 0.0f, 1.0f);//sample;
}

// TECHNIQUES 

// Performs light scattering AND bloom
technique LightScatteringTech
{
	pass P0
	{
		VertexShader = compile vs_3_0 LightScatteringVS();
		PixelShader  = compile ps_3_0 LightScatteringPS();
	}
}

// Performs light scattering only. (Requires call to BloomTech to finish effect)
technique LightScatteringTech2
{
	pass P0
	{
		VertexShader = compile vs_3_0 LightScatteringVS();
		PixelShader  = compile ps_3_0 LightScatteringPS2();
	}
}

// Performs light scattering only. Uses the alpha channel of input texture for occlusion.
// (Requires call to BloomTech to finish effect)
technique LightScatteringTech3
{
	pass P0
	{
		VertexShader = compile vs_3_0 LightScatteringVS();
		PixelShader  = compile ps_3_0 LightScatteringPS3();
	}
}

technique BloomTech
{
	pass P0
	{
		VertexShader = compile vs_3_0 BloomVS();
		PixelShader  = compile ps_3_0 BloomPS();
	}
}

// Performs a straight texture copy. (Will completely overwrite contents of target buffer)
technique CopyTech
{
	pass P0
	{
		VertexShader = compile vs_3_0 CopyVS();
		PixelShader  = compile ps_3_0 CopyPS();
	}
}

// Performs a texture copy, copying only non-zero alpha pixels. (Will not overwrite all contents of target buffer)
technique CopyOcclusionTech
{
	pass P0
	{
		VertexShader = compile vs_3_0 CopyVS();
		PixelShader  = compile ps_3_0 CopyOcclusionPS();
	}
}