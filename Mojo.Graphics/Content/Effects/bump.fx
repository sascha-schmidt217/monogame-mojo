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

texture NormalTexture : register(t1);
sampler NormalSampler : register(s1);



struct VertexShaderInput
{
	float4 Position : POSITION0;
	float4 Color	: COLOR0;
	float2 TexCoord0 : TEXCOORD0;
	float2 a_TexCoord1 : TEXCOORD1;
};

struct VertexShaderOutput
{
	float4 Position		: SV_POSITION;
	float4 Color		: COLOR0;
	float2 TexCoord0	: TEXCOORD0;
	float2x2 v_TanMatrix : TANGENT;
};

VertexShaderOutput MainVS(VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;

	output.Position = mul(input.Position, WorldViewProjection);
	output.Color = input.Color;
	output.TexCoord0 = input.TexCoord0;
	output.v_TanMatrix = float2x2(input.a_TexCoord1.x, input.a_TexCoord1.y, -input.a_TexCoord1.y, input.a_TexCoord1.x);

	return output;
}

struct PixelShaderOutput
{
	float4 Color0 : COLOR0;  float4 Color1 : COLOR1;
};

PixelShaderOutput MainPS(VertexShaderOutput input) : COLOR
{
	float4 diffuse = tex2D(DiffuseSampler, input.TexCoord0);
	float3 normal = tex2D(NormalSampler, input.TexCoord0).xyz;

	normal.xy = mul(input.v_TanMatrix, normal.xy *2.0f - 1.0f);
	normal.xy = normal.xy * 0.5f + 0.5f;


	PixelShaderOutput output = (PixelShaderOutput)0;
	output.Color0 = diffuse * input.Color;
	output.Color1 = float4(normal.rgb * diffuse.a, diffuse.a);

	return output;
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};