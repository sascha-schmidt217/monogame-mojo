#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_3
#define PS_SHADERMODEL ps_4_0_level_9_3
#endif

matrix WorldViewProjection;

texture DiffuseTexture : register(t0);
sampler DiffuseSampler : register(s0);

texture LightmapTexture : register(t1);
sampler LightmapSampler : register(s1);

struct VertexShaderInput
{
	float4 Position : POSITION0;
	float4 Color	: COLOR0;
	float2 TexCoord : TEXCOORD;
};

struct VertexShaderOutput
{
	float4 Position		: SV_POSITION;
	float2 TexCoord0	: TEXCOORD0;
};

VertexShaderOutput MainVS(VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;
	output.Position = mul(input.Position, WorldViewProjection);
	output.TexCoord0 = input.TexCoord;
	return output;
}


float4 MainPS(VertexShaderOutput input) : COLOR
{
	float4 diffuse = tex2D(DiffuseSampler, input.TexCoord0);
	float4 lighting = tex2D(LightmapSampler, input.TexCoord0);

	float4 color = float4(diffuse.rgb * lighting.rgb + lighting.a, 1.0f);
	return color;
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};