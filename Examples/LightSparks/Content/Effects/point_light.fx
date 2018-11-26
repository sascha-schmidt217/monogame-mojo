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

texture spacularMap: register(t2);
sampler specularMapSampler: register(s2);

matrix WorldViewProj;
float2 inv_tex_size;
float2 lightPos;
float2 lightDir;
float intensity;
float radius;
float inner;
float outer;
float m_LightDepth;
float useNormalmap;

struct Shading
{
	float Diffuse;
	float Specular;
};

struct VertexShaderInput
{
	float4 Position : POSITION0;
	float4 Color	: COLOR0;
	float2 TexCoord : TEXCOORD;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 LightColor		: COLOR0;
	float2 TexCoord0	:TEXCOORD0;
	float2 light_dir	: TEXCOORD1;
	float3 light_dir_depth	: TEXCOORD2;
};

VertexShaderOutput VSSpotLight(VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;
	output.Position = mul(input.Position, WorldViewProj);
	output.LightColor = input.Color;
	output.TexCoord0 = input.Position.xy  * inv_tex_size;
	output.light_dir = (input.Position.xy - lightPos);
	output.light_dir_depth = float3(output.light_dir, m_LightDepth);
	return output;
}

VertexShaderOutput VSPointLight(VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;
	output.Position = mul(input.Position, WorldViewProj);
	output.LightColor = input.Color;
	output.TexCoord0 = input.Position.xy  * inv_tex_size;
	output.light_dir = (lightPos - input.Position.xy) *float2(1, -1);
	output.light_dir_depth = float3(output.light_dir, m_LightDepth);
	return output;
}

///////////////////////////////////////////////////////////////////////////////////////////////////

float PointLightIntensity(float2 light_dir, float r, float i)
{
	return  pow(clamp(1.0f - length(light_dir) / r, 0.0f, 1.0f), i);
}

float SpotLightIntensity(float2 dir, float r, float i)
{
	float angle = acos(dot(normalize(lightDir), normalize(dir)));
	float f = 1.0f - smoothstep(inner, outer, angle);
	return pow(clamp(1.0f - length(dir) / r, 0.0f, 1.0f), i) * f;
}

Shading CalculateNormalMapping(float4 normal, float3 lightDirNorm)
{
	Shading shading = (Shading)0;
	shading.Diffuse = max(dot(normal.xyz, lightDirNorm), 0.0);
	shading.Specular = pow(saturate(dot(reflect(-lightDirNorm, normal.xyz), float3(0.0, 0.0, 1.0))), 128) * normal.w;
	return shading;
}

float4 GetNormalVector(float2 TexCoord0)
{
	float4 normal = tex2D(normalMapSampler, TexCoord0);
	normal.w = normal.z;
	normal.xy = mad(normal.xy, 2.0f, -1.0f);
	normal.z = sqrt(1.0 - dot(normal.xy, normal.xy));
	return normal;
}

float GetShadowFactor(float2 TexCoord0)
{
	return tex2D(shadowMapSampler, TexCoord0).a;
}

///////////////////////////////////////////////////////////////////////////////////////////////////
// point light

float4 PSPointLightNormalShadow(VertexShaderOutput input) : COLOR
{
	float2 light_dir =  input.light_dir;
	float factor = GetShadowFactor(input.TexCoord0) * PointLightIntensity(light_dir, radius, intensity);
	float4 normal = GetNormalVector(input.TexCoord0);
	Shading shading = CalculateNormalMapping(normal, normalize(input.light_dir_depth));
	return float4(input.LightColor.rgb * shading.Diffuse * factor, shading.Specular * factor);
}

float4 PSPointLightNormal(VertexShaderOutput input) : COLOR
{
	float2 light_dir = input.light_dir;
	float alpha = PointLightIntensity(light_dir, radius, intensity);
	float4 normal = GetNormalVector(input.TexCoord0);
	Shading shading = CalculateNormalMapping(normal, normalize(input.light_dir_depth));
	return float4(input.LightColor.rgb * shading.Diffuse * alpha, shading.Specular * alpha);
}

float4 PSPointLight_Shadow(VertexShaderOutput input) : COLOR
{
	float2 light_dir = input.light_dir;
	float factor = GetShadowFactor(input.TexCoord0) * PointLightIntensity(light_dir, radius, intensity);
	return float4(input.LightColor.rgb  * factor, 0.0f);
}

float4 PSPointLight(VertexShaderOutput input) : COLOR
{
	float2 light_dir = input.light_dir;
	float factor = PointLightIntensity(light_dir, radius, intensity);
	return float4(input.LightColor.rgb  * factor, 0.0f);
}

/////////////////////////////////////////////////////////////////////////////
// spot light

float4 PSSpotLightNormalShadow(VertexShaderOutput input) : COLOR
{
	float2 light_dir = input.light_dir;
	float factor = GetShadowFactor(input.TexCoord0) * SpotLightIntensity(light_dir, radius, intensity);
	float4 normal = GetNormalVector(input.TexCoord0);
	Shading shading = CalculateNormalMapping(normal, normalize(input.light_dir_depth));
	return float4(input.LightColor.rgb * shading.Diffuse * factor, shading.Specular * factor);
}

float4 PSSpotLightNormal(VertexShaderOutput input) : COLOR
{
	float2 light_dir = input.light_dir;
	float alpha = SpotLightIntensity(light_dir, radius, intensity);
	float4 normal = GetNormalVector(input.TexCoord0);
	Shading shading = CalculateNormalMapping(normal, normalize(input.light_dir_depth));
	return float4(input.LightColor.rgb * shading.Diffuse * alpha, shading.Specular * alpha);
}


float4 PSSpotLight_Shadow(VertexShaderOutput input) : COLOR
{
	float2 light_dir = input.light_dir;
	float factor = GetShadowFactor(input.TexCoord0) * SpotLightIntensity(light_dir, radius, intensity);
	return float4(input.LightColor.rgb  * factor, 0.0f);
}

float4 PSSpotLight(VertexShaderOutput input) : COLOR
{
	float2 light_dir = input.light_dir;
	float factor = SpotLightIntensity(light_dir, radius, intensity);
	return float4(input.LightColor.rgb  * factor, 0.0f);
}

/////////////////////////////////////////////////////////////////////////////
// point light

technique Mojo_PointLight {
	pass P0 {
		VertexShader = compile VS_SHADERMODEL VSPointLight();
		PixelShader = compile PS_SHADERMODEL PSPointLight();
	}
}

technique Mojo_PointLight_Shadow {
	pass P0 {
		VertexShader = compile VS_SHADERMODEL VSPointLight();
		PixelShader = compile PS_SHADERMODEL PSPointLight_Shadow();
	}
}

technique Mojo_PointLight_Normal {
	pass P0 {
		VertexShader = compile VS_SHADERMODEL VSPointLight();
		PixelShader = compile PS_SHADERMODEL PSPointLightNormal();
	}
}

technique Mojo_PointLight_Normal_Shadow {
	pass P0 {
		VertexShader = compile VS_SHADERMODEL VSPointLight();
		PixelShader = compile PS_SHADERMODEL PSPointLightNormalShadow();
	}
}

//////////////////////////////////////
// spot light

technique Mojo_SpotLight {
	pass P0 {
		VertexShader = compile VS_SHADERMODEL VSSpotLight();
		PixelShader = compile PS_SHADERMODEL PSSpotLight();
	}
}

technique Mojo_SpotLight_Shadow {
	pass P0 {
		VertexShader = compile VS_SHADERMODEL VSSpotLight();
		PixelShader = compile PS_SHADERMODEL PSSpotLight_Shadow();
	}
}

technique Mojo_SpotLight_Normal {
	pass P0 {
		VertexShader = compile VS_SHADERMODEL VSSpotLight();
		PixelShader = compile PS_SHADERMODEL PSSpotLightNormal();
	}
}

technique Mojo_SpotLight_Normal_Shadow {
	pass P0 {
		VertexShader = compile VS_SHADERMODEL VSSpotLight();
		PixelShader = compile PS_SHADERMODEL PSSpotLightNormalShadow();
	}
}










