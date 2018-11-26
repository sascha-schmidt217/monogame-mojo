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

texture SpecularTexture : register(t2);
sampler SpecularSampler : register(s2);

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


VertexShaderOutput VSTextureNormal(VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;
	output.Position = mul(input.Position, WorldViewProjection);
	output.Color = input.Color;
	output.TexCoord0 = input.TexCoord0;
	output.v_TanMatrix = float2x2(input.a_TexCoord1.x, input.a_TexCoord1.y, -input.a_TexCoord1.y, input.a_TexCoord1.x);
	return output;
}

VertexShaderOutput VSPrimitve(VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;
	output.Position = mul(input.Position, WorldViewProjection);
	output.Color = input.Color;
	return output;
}

/////////////////////////////////////////////////////////////////////


struct PixelShaderOutput_DiffuseNormalSpecular
{
	float4 Diffuse : COLOR0;  
	float4 Normal : COLOR1;
	float4 Specular : COLOR2;
};

struct PixelShaderOutput_DiffuseNormal
{
	float4 Diffuse : COLOR0;
	float4 Normal : COLOR1;
};

/////////////////////////////////////////////////////////////////////

PixelShaderOutput_DiffuseNormalSpecular PS_DiffuseNormalSpecular(VertexShaderOutput input) : COLOR
{
	PixelShaderOutput_DiffuseNormalSpecular output = (PixelShaderOutput_DiffuseNormalSpecular)0;
	float4 diffuse = tex2D(DiffuseSampler, input.TexCoord0);
	float3 normal = tex2D(NormalSampler, input.TexCoord0).xyz;
	float4 specular = tex2D(SpecularSampler, input.TexCoord0);
	normal.xy = mul(input.v_TanMatrix, normal.xy * 2.0f - 1.0f);
	normal.xy = normal.xy * 0.5f + 0.5f;
	output.Diffuse = diffuse * input.Color;
	output.Normal = float4(normal.rgb * diffuse.a, diffuse.a);
	output.Specular = specular * diffuse.a;
	return output;
}

PixelShaderOutput_DiffuseNormal PS_DiffuseNormal(VertexShaderOutput input) : COLOR
{
	PixelShaderOutput_DiffuseNormal output = (PixelShaderOutput_DiffuseNormal)0;
	float4 diffuse = tex2D(DiffuseSampler, input.TexCoord0);
	float3 normal = tex2D(NormalSampler, input.TexCoord0).xyz;
	normal.xy = mul(input.v_TanMatrix, normal.xy * 2.0f - 1.0f);
	normal.xy = normal.xy * 0.5f + 0.5f;
	float4 diffuseColor = diffuse * input.Color;
	output.Diffuse = diffuseColor;
	output.Normal = float4(normal.rgb * diffuseColor.a, diffuseColor.a);
	return output;
}

float4 PS_Diffuse(VertexShaderOutput input) : COLOR
{
	return tex2D(DiffuseSampler, input.TexCoord0) * input.Color;
}

/////////////////////////////////////////////////////////////////////

PixelShaderOutput_DiffuseNormalSpecular PS_PrimitiveNormalSpecular(VertexShaderOutput input) : COLOR
{
	PixelShaderOutput_DiffuseNormalSpecular output = (PixelShaderOutput_DiffuseNormalSpecular)0;
	float4 diffuse = input.Color;
	float3 normal = float3(0.5f, 0.5f, 1.0f);
	normal.xy = mul(input.v_TanMatrix, normal.xy * 2.0f - 1.0f);
	normal.xy = normal.xy * 0.5f + 0.5f;
	output.Diffuse = diffuse;
	output.Normal = float4(normal * diffuse.a, diffuse.a);
	output.Specular = float4(0,0,0,1) * diffuse.a;
	return output;
}


PixelShaderOutput_DiffuseNormal PS_PrimitiveNormal(VertexShaderOutput input) : COLOR
{
	PixelShaderOutput_DiffuseNormal output = (PixelShaderOutput_DiffuseNormal)0;
	float4 diffuse = input.Color;
	float3 normal = float3(0.5f, 0.5f, 0);
	output.Diffuse = input.Color;
	output.Normal = float4(normal * diffuse.a, diffuse.a);
	return output;
}

float4 PS_Primitive(VertexShaderOutput input) : COLOR
{
	return input.Color;
}


/////////////////////////////////////////////////////////////////////

technique MojoEffect_Primitive
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL VSPrimitve();
		PixelShader = compile PS_SHADERMODEL PS_Primitive();
	}
};

technique MojoEffect_PrimitiveNormal
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL VSPrimitve();
		PixelShader = compile PS_SHADERMODEL PS_PrimitiveNormal();
	}
};


technique MojoEffect_PrimitiveNormalSpecular
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL VSPrimitve();
		PixelShader = compile PS_SHADERMODEL PS_PrimitiveNormalSpecular();
	}
};


/////////////////////////////////////////////////////////////////////

technique MojoEffect_Diffuse
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL VSTextureNormal();
		PixelShader = compile PS_SHADERMODEL PS_Diffuse();
	}
};

technique MojoEffect_DiffuseNormal
{
	pass P0_pDiffuseTexture
	{
		VertexShader = compile VS_SHADERMODEL VSTextureNormal();
		PixelShader = compile PS_SHADERMODEL PS_DiffuseNormal();
	}
};

technique MojoEffect_DiffuseNormalSpecular
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL VSTextureNormal();
		PixelShader = compile PS_SHADERMODEL PS_DiffuseNormalSpecular();
	}
};




