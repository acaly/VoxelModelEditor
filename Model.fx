struct VS_IN
{
	float3 pos : POSITION;
	uint dir : TEXCOORD1;
	float4 col : COLOR;
};

struct GS_IN
{
	float4 pos : POSITION;
	float4 dir_u : TEXCOORD1;
	float4 dir_v : TEXCOORD2;
	float4 col : COLOR;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float4 col : COLOR;
};

struct PS_IN4
{
	PS_IN pp, pn, np, nn;
};

cbuffer VS_CONSTANT_BUFFER : register(b0)
{
	float4x4 worldViewProj;
}

cbuffer GS_CONSTANT_BUFFER : register(b0)
{
	uint EdgeIndex;
}

static const float4 dir_u_list[6] =
{
	float4(0, 0, 0.5, 0),
	float4(0, 0, 0.5, 0),
	float4(0.5, 0, 0, 0),
	float4(0.5, 0, 0, 0),
	float4(0, 0.5, 0, 0),
	float4(0, 0.5, 0, 0),
};

static const float4 dir_v_list[6] =
{
	float4(0, 0.5, 0, 0),
	float4(0, -0.5, 0, 0),
	float4(0, 0, 0.5, 0),
	float4(0, 0, -0.5, 0),
	float4(0.5, 0, 0, 0),
	float4(-0.5, 0, 0, 0),
};

static const float4 grid_color = float4(0.3, 0.3, 0.3, 1);

static const float3 light_dir = float3(1, -0.8, 0.4);

GS_IN VS(VS_IN input)
{
	GS_IN output = (GS_IN)0;

	output.pos = mul(float4(input.pos, 1), worldViewProj);
	output.dir_u = mul(dir_u_list[input.dir], worldViewProj);
	output.dir_v = mul(dir_v_list[input.dir], worldViewProj);

	float3 dir_n = mul(cross(dir_u_list[input.dir].xyz, dir_v_list[input.dir].xyz), worldViewProj);
	output.col = float4((input.col.rgb + float3(0.2, 0.2, 0.2)) * saturate(0.6 + dot(dir_n, light_dir) * 0.4), 1);

	return output;
}

PS_IN4 CreateLine(float4 a, float4 b)
{
	float2 wh = float2(400, 300);

	PS_IN4 point4 = (PS_IN4)0;

	a /= a.w;
	b /= b.w;

	float2 a_s = a.xy * wh;
	float2 b_s = b.xy * wh;

	float2 dir1_s = normalize(a_s - b_s) * 0.6;
	float2 dir2_s = float2(-dir1_s.y, dir1_s.x);
	
	float2 pp_s = a_s + dir1_s + dir2_s;
	float2 pn_s = a_s + dir1_s - dir2_s;
	float2 np_s = b_s - dir1_s + dir2_s;
	float2 nn_s = b_s - dir1_s - dir2_s;

	point4.pp.pos = float4(pp_s / wh, a.z - 0.00001, 1);
	point4.pn.pos = float4(pn_s / wh, a.z - 0.00001, 1);
	point4.np.pos = float4(np_s / wh, b.z - 0.00001, 1);
	point4.nn.pos = float4(nn_s / wh, b.z - 0.00001, 1);

	point4.pp.col = grid_color;
	point4.pn.col = grid_color;
	point4.np.col = grid_color;
	point4.nn.col = grid_color;

	return point4;
}

[maxvertexcount(4)]
void GS(point GS_IN input[1], inout TriangleStream<PS_IN> triStream)
{
	PS_IN4 point4 = (PS_IN4)0;

	point4.pp.pos = input[0].pos + input[0].dir_u + input[0].dir_v;
	point4.pn.pos = input[0].pos + input[0].dir_u - input[0].dir_v;
	point4.np.pos = input[0].pos - input[0].dir_u + input[0].dir_v;
	point4.nn.pos = input[0].pos - input[0].dir_u - input[0].dir_v;

	point4.pp.col = input[0].col;
	point4.pn.col = input[0].col;
	point4.np.col = input[0].col;
	point4.nn.col = input[0].col;

	if (EdgeIndex != 0)
	{
		if (EdgeIndex == 1)
		{
			point4 = CreateLine(point4.pp.pos, point4.pn.pos);
		}
		else if (EdgeIndex == 2)
		{
			point4 = CreateLine(point4.pn.pos, point4.nn.pos);
		}
		else if (EdgeIndex == 3)
		{
			point4 = CreateLine(point4.nn.pos, point4.np.pos);
		}
		else if (EdgeIndex == 4)
		{
			point4 = CreateLine(point4.np.pos, point4.pp.pos);
		}
	}

	triStream.Append(point4.pp);
	triStream.Append(point4.pn);
	triStream.Append(point4.np);
	triStream.Append(point4.nn);
	triStream.RestartStrip();
}

float4 PS(PS_IN input) : SV_Target
{
	return input.col;
}
