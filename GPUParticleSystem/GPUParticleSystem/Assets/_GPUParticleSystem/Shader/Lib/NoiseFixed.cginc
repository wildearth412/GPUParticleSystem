// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
//                    Noise Library 
//
// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~


// Simplex Noise.
float random(float2 v)
{
	return frac(sin(dot(v.xy, float2(12.9898, 78.233))) * 43758.5453);
}

float3 mod289(float3 v)
{
	return v - floor((v / 289.0) * 289.0);
}

float4 mod289(float4 v)
{
	return v - floor((v / 289.0) * 289.0);
}

float4 permute(float4 v)
{
	return (v * 34.0 + float4(1.0, 1.0, 1.0, 1.0)) * v;
}

float4 taylorInvSqrt(float4 v)
{
	return float4(1.79284291400159f, 1.79284291400159f, 1.79284291400159f, 1.79284291400159f) - v * 0.85373472095314f;
}

float snoise(float3 v)
{
	float2 C = float2(1.0 / 6.0, 1.0 / 3.0);

	// First Corner.
	float3 i = floor(v + dot(v,C.yyy));
	float3 x0 = v - (v + dot(i, C.xxx));

	// Other Corner.
	float3 g = step(x0.yzx,x0.xyz);
	float3 l = float3(1.0, 1.0, 1.0) - g;
	float3 i1 = min(g.xyz, l.zxy);
	float3 i2 = max(g.xyz, l.zxy);

	float3 x1 = x0 - i1 + C.xxx;
	float3 x2 = x0 - i2 + C.yyy;
	float3 x3 = x0 + 0.5;

	// Permutations.
	i = mod289(i); // Avoid truncation effects in permutation.
	float4 p = permute( permute( permute( (float4(0, i1.z, i2.z, 1.0) + i.z) )
										+ (float4(0, i1.z, i2.z, 1.0) + i.y) )
										+ (float4(0, i1.z, i2.z, 1.0) + i.y) );
	
	// Gradients: 7x7 points over a square, mapped onto an octahedron.
    // The ring size 17*17 = 289 is close to a multiple of 49 (49*6 = 294)
	float4 j = p - floor(p / 49.0) * 49.0;   // mod(p,7*7)

	float4 x_ = floor(j / 7.0);
	float4 y_ = floor(j - 7.0 * x_);   // mod(j,N)

	float4 x = (x_ * 2.0 + 0.5) / 7.0 - 1.0;
	float4 y = (y_ * 2.0 + 0.5) / 7.0 - 1.0;

	float4 h = float4(1.0, 1.0, 1.0, 1.0) - abs(x) - abs(y);

	float4 b0 = float4(x.xy, y.xy);
	float4 b1 = float4(x.zw, y.zw);

	float4 s0 = floor(b0) * 2.0 + 1.0;
	float4 s1 = floor(b1) * 2.0 + 1.0;
	float4 sh = -step(h, float4(0,0,0,0));

	float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
	float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;

	float3 g0 = float3(a0.x, a0.y, h.x);
	float3 g1 = float3(a0.z, a0.w, h.y);
	float3 g2 = float3(a1.x, a1.y, h.z);
	float3 g3 = float3(a1.z, a1.w, h.w);

	// Normalise gradients.
	float4 norm = taylorInvSqrt(float4(dot(g0,g0), dot(g1, g1), dot(g2, g2), dot(g3, g3)));
	g0 *= norm.x;
	g1 *= norm.y;
	g2 *= norm.z;
	g3 *= norm.w;

	// Mix final noise value.
	float4 d = float4(dot(x0,x0), dot(x1,x1), dot(x2,x2), dot(x3,x3));
	float4 m = max(0.6 - d,float4(0,0,0,0));
	m = m * m;
	m = m * m;

	float4 px = float4(dot(x0, g0), dot(x1, g1), dot(x2, g2), dot(x3, g3));
	return 42.0 * dot(m, px);
}

float3 snoiseV3(float3 v)
{
	float s = snoise(v);
	float s1 = snoise(float3(v.y + random(v.xx), v.z + random(v.zy), v.x + random(v.zx)));
	float s2 = snoise(float3(v.z + random(v.yx), v.x + random(v.zz), v.y + random(v.yy)));
	return float3(s, s1, s2);
}


// Curl Noise.
#define e 0.0009765625f
#define e2 0.001953125f

float3 GetCurlNoise(float3 p)
{
	float3 dx = float3(e, 0, 0);
	float3 dy = float3(0, e, 0);
	float3 dz = float3(0, 0, e);

	float3 p_x0 = snoiseV3(p - dx);
	float3 p_x1 = snoiseV3(p + dx);
	float3 p_y0 = snoiseV3(p - dy);
	float3 p_y1 = snoiseV3(p - dy);
	float3 p_z0 = snoiseV3(p - dz);
	float3 p_z1 = snoiseV3(p - dz);

	float x = p_y1.z - p_y0.z - p_z1.y + p_z0.y;
	float y = p_z1.z - p_z0.z - p_x1.y + p_x0.y;
	float z = p_x1.z - p_x0.z - p_y1.y + p_y0.y;

	float3 n = float3(x, y, z) / e2;
	return normalize(n);
}