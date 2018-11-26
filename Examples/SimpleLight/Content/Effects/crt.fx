#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_3
#define PS_SHADERMODEL ps_4_0_level_9_3
#endif

float ImgWidth;
float ImgHeight;
//[Range(0,1)]
float RgbStrength;
//Range(-3, 20)] 
float Contrast;
//[Range(-200, 200)] 
float Brightness;

float scanline_factor;

float Norm;
float Strength;
float Zoom;
float Time;

matrix WorldViewProjection;

texture ScreenTexture : register(t0);
sampler TextureSampler : register(s0);

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
	float2 abspos       : TEXCOORD1;
};

VertexShaderOutput MainVS(VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;

	output.Position = mul(input.Position, WorldViewProjection);
	output.TexCoord0 = input.TexCoord;

	// Resulting X pixel-coordinate of the pixel we're drawing. 
	// Assumes (-0.5, 0.5) quad and output size in World matrix 
	// as currently done in DOSBox D3D patch 
	//output.abspos = float2((input.Position.x + 0.5) * WorldViewProjection._11, (input.Position.y - 0.5) * (-WorldViewProjection._22));


	return output;
}

float2 pincushion(float2 pos, float2 SizeHalf, float2 TexSize, float norm, float strength)
{
	// pincushion transform
	float2 d = pos - SizeHalf;
	float2 r = d / norm;
	float r_len = length(r);
	float2 r_unit = r / r_len;
	float new_dist = r_len + strength * (r_len * r_len * r_len);
	float2 warp = r_unit * (new_dist * norm  * Zoom) * (1.0f - strength) + SizeHalf;

	// to relative trexture coordinate
	float2 textureSource = warp / TexSize;
	return textureSource;
}


float4 MainPS(VertexShaderOutput input) : COLOR
{
	const float4 fr = float4(1.0f, 1.0f - RgbStrength, 1.0f - RgbStrength, 1.0f);
	const float4 fg = float4(1.0f - RgbStrength, 1.0f, 1.0f - RgbStrength, 1.0f);
	const float4 fb = float4(1.0f - RgbStrength, 1.0f - RgbStrength, 1.0f, 1.0f);

	const float strength = 0.5f;
	const float2 offset = float2(0.0f, 0.0f);
	const float2 size = float2(ImgWidth, ImgHeight);;
	const float2 SizeHalf = float2(ImgWidth / 2, ImgHeight / 2) + offset;
	const float2 invSize = float2(1.0f / ImgWidth, 1.0f / ImgHeight);
	const float inv_refDistance = 1.0f;

	// rgb pattern
	float2 coord = input.TexCoord0 * float2(ImgWidth, ImgHeight);

	float2 tex_coord = pincushion(coord, SizeHalf, size, Norm, Strength);
	float4 tex = tex2D(TextureSampler, tex_coord);

	// clip
	if (tex_coord.x < 0.0f || tex_coord.x >1.0f ||
		tex_coord.y < 0.0f || tex_coord.y >1.0f)
	{
		return float4(0.0f, 0.0f, 0.0f, 1.0f);
	}

	int pp = (int)coord.x % 3;
	if (pp == 0)
	{
		tex *= fr;
	}
	else if (pp == 1)
	{
		tex *= fg;
	}
	else
	{
		tex *= fb;
	}

	// brightness
	tex += (Brightness / 255);

	// contrast
	tex = tex - Contrast * (tex - 1.0f) * tex * (tex - 0.5f);

	//scanline
	tex *= ((1.0f - scanline_factor) + scanline_factor *
		(sin((coord.y-0.5f)*3.14/3*2)*0.15f + 0.85f + (sin((Time+coord.y)*0.1f)*0.01f)));

	return tex;
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};