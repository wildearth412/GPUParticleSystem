// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/CurlNoiseParticleRender"
{
	//Properties
	//{
	//	_MainTex ("Texture", 2D) = "white" {}
	//}

	CGINCLUDE
	#include "UnityCG.cginc"

	// Struct of particle data;
	struct ParticleData
	{
		float3 velocity;
		float3 position;
		float lifespan;
		float age;          // age : 0 ~ 1
	};

	// Struct from VertexShader to GeometryShader.
	struct v2g
	{
		float3 position : TEXCOORD0;
		float4 color : COLOR;
	};

	// Struct from GeometryShader to FragmentShader.
	struct g2f
	{
		float4 position : POSITION;
		float2 texcoord : TEXCOORD0;
		float4 color : COLOR;
	};

	// Particles data.
	StructuredBuffer<ParticleData> _ParticleBuffer;
	// Particles texture.
	sampler2D _MainTex;
	float4 _MainTex_ST;
	// Particles size.
	float _ParticleSize;
	// The inverse view matrix.
	float4x4 _InvViewMatrix;

	// The coords of quad plane vertex position.
	static const float3 g_positions[4] =
	{
		float3(-1, 1, 0),
		float3(1, 1, 0),
		float3(-1,-1, 0),
		float3(1,-1, 0),
	};

	// The coords of quad plane UV position.
	static const float2 g_texcoords[4] = 
	{
		float2(0,0),
		float2(1,0),
		float2(0,1),
		float2(1,1),
	};

	// Vextex Shader.
	v2g vert(uint id : SV_VertexID)   // SV_VertexID : identifier per vertex.
	{
		v2g o = (v2g)0;
		// Particles position.
		o.position = _ParticleBuffer[id].position;
		// Particles color.
		float alpha = clamp(_ParticleBuffer[id].age, 0, 1);
		o.color = float4(0.5 + 0.5 * normalize(_ParticleBuffer[id].velocity),alpha);
		//o.color = float4(0.5 + 0.5 * normalize(_ParticleBuffer[id].position), 1.0);
		return o;
	}

	// Geometry Shader.
	[maxvertexcount(4)]
	void geom(point v2g In[1], inout TriangleStream<g2f> SpriteStream)
	{
		g2f o = (g2f)0;
		[unroll]
		for (int i = 0; i < 4; i++)
		{
			float3 position = g_positions[i] * _ParticleSize;
			position = mul(_InvViewMatrix, position) + In[0].position;
			o.position = UnityObjectToClipPos(float4(position,1.0));

			o.color = In[0].color;
			o.texcoord = g_texcoords[i];

			// Append vertex.
			SpriteStream.Append(o);
		}
		// Close up stream.
		SpriteStream.RestartStrip();
	}

	// Fragment Shader.
	fixed4 frag(g2f i) : SV_Target
	{
		return tex2D(_MainTex, i.texcoord.xy) * i.color;
	}
	ENDCG

	SubShader
	{
		Tags { "RenderType"="Opaque" "IgnoreProjector"="True" "RenderType"="Transparent" }
		LOD 100

		ZWrite Off
		//Blend One One
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma target 5.0
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag	
			ENDCG
		}
	}
}
