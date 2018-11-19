#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

//float intensity = clamp(1.0f - (dist / 32.0f), 0.0f, 1.0f);

//float minLight = 0.1f;
//float radius = 32.0f;
//float a = 0;
//float b = 1.0 / (radius*radius * minLight);
//float intensity = 1.0f / (1.0f + b *dist*dist);

//float intensity = clamp(1.0f - dist / radius, 0.0f, 1.0f); 
//intensity *= intensity;

texture shadowMap: register(t0);
sampler shadowMapSampler: register(s0);

matrix WorldViewProj;
float2 lightPos;
float intensity;
float radius;
float2 inv_tex_size;

struct VertexShaderInput
{
	float4 Position : POSITION0;
	float4 Color	: COLOR0;
	float2 TexCoord : TEXCOORD;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color	: COLOR0;
	float2 TexCoord0	:TEXCOORD0;
	float2 TexCoord1	:TEXCOORD1;
};

VertexShaderOutput MainVS(VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;
	output.Position = mul(input.Position, WorldViewProj);
	output.Color = input.Color;
	output.TexCoord0 = input.Position.xy;
	return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
	// lighting attentuation
	float dist = length(input.TexCoord0 - lightPos);
	float alpha = pow(clamp(1.0f - dist / radius, 0.0f, 1.0f), intensity);

	// shadow
	float shadow = tex2D(shadowMapSampler, input.TexCoord0 * inv_tex_size).a;
	return float4(input.Color.r, input.Color.g, input.Color.b, alpha * shadow) ;
}

technique BasicColorDrawing{
	pass P0{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
}