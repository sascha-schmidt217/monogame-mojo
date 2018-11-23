#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_3
#define PS_SHADERMODEL ps_4_0_level_9_3
#endif


texture shadowMap: register(t0);
sampler shadowMapSampler: register(s0);

texture normalMap: register(t1);
sampler normalMapSampler: register(s1);

matrix WorldViewProj;
float2 lightPos;
float2 lightDir;
float intensity;
float radius;
float inner;
float outer;
float2 inv_tex_size;
float m_LightDepth;
float useNormalmap;

struct VertexShaderInput
{
	float4 Position : POSITION0;
	float4 Color	: COLOR0;
	float2 TexCoord : TEXCOORD;
};

struct VertexShaderOutput
{
	float4 Position		: SV_POSITION;
	float4 LightColor	: COLOR0;
	float2 TexCoord0		: TEXCOORD0;
	float2 light_dir	: TEXCOORD1;
};

VertexShaderOutput MainVS(VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;
	output.Position = mul(input.Position, WorldViewProj);
	output.LightColor = input.Color;
	output.TexCoord0 = input.Position.xy;

	float2 p_light = input.Position.xy - lightPos;
	//p_light.y *= -1;

	output.light_dir = p_light;


	return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
	const float specular_factor = 1.0f;

	// cutoff angle factor
	float2 dir = input.light_dir;
	float angle = acos(dot(normalize(lightDir), normalize(dir)));
	float f = 1.0f - smoothstep(inner, outer, angle);

	// lighting attentuation
	float alpha = pow(clamp(1.0f - length(dir) / radius, 0.0f, 1.0f), intensity);

	// shadow
	float shadow = tex2D(shadowMapSampler, input.TexCoord0 * inv_tex_size).a;

	float factor = alpha * f * shadow;

	if (useNormalmap != 0)
	{
		// normal
		//
		float3 normal = tex2D(normalMapSampler, input.TexCoord0 * inv_tex_size).xyz;
		float gloss = normal.z;
		normal.xy = normal.xy * 2.0 - 1.0;
		normal.z = sqrt(1.0 - dot(normal.xy, normal.xy));

		// normal lighting
		float3 light_dir_norm = normalize(float3(dir*float2(-1, 1), m_LightDepth));
		float ndotl = max(dot(normal, light_dir_norm), 0.0);

		// specular
		float3 hvec = normalize(light_dir_norm + float3(0.0, 0.0, 1.0));
		float ndoth = max(dot(normal, hvec), 0.0);
		float specular = pow(ndoth, 128.0) * gloss;

		// rgb = lightmap
		// a = specular reflection
		return float4(input.LightColor.rgb * ndotl * factor, specular * factor);
	}
	else
	{
		return float4(input.LightColor.rgb  * factor, 0.0f);
	}
	
}

technique BasicColorDrawing {
	pass P0 {
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
}