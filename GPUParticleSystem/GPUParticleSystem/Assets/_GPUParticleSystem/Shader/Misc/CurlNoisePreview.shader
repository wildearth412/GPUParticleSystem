Shader "Custom/CurlNoisePreview"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}

	CGINCLUDE
	#include "UnityCG.cginc"
	#include "Assets/_GPUParticleSystem/Shader/Lib/Noise.cginc"

	struct appdata
	{		
		float4 position : POSITION;
		float2 uv : TEXCOORD0;
	};

	struct v2f
	{
		float4 position : SV_POSITION;
		float2 uv : TEXCOORD0;
		float4 wpos : float4;
	};

	sampler2D _MainTex;
	float4 _MainTex_ST;

	v2f vert(appdata v) 
	{
		v2f o;
		o.position = UnityObjectToClipPos(v.position);
		o.wpos = mul(unity_ObjectToWorld, v.position);
		o.uv = TRANSFORM_TEX(v.uv, _MainTex);
		return o;
	}

	fixed4 frag(v2f i) : SV_Target
	{
		float4 col;
		
		float cx = curlX(i.wpos, 0.0009765625);
		float cy = curlY(i.wpos, 0.0009765625);
		float cz = curlZ(i.wpos, 0.0009765625);
		col.rgb = float3(cx,cy,cz);
		//col.rgb = snoise3D(i.wpos * 0.75);
		//col.rgb = float3(1.0,1.0,1.0);
		col.a = 1.0;
		return col;
	}
	ENDCG

	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		ZWrite Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag	
			ENDCG
		}
	}
}
