Shader "Unlit/TextureAnimPlayer_Distortion"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_PosTex("position texture", 2D) = "black"{}
		_NmlTex("normal texture", 2D) = "white"{}
		_DT ("delta time", float) = 0
		_Length ("animation length", Float) = 1
		_Dist("distort factor", Float) = 1
		[Toggle(ANIM_LOOP)] _Loop("loop", Float) = 0
	}
	SubShader
	{
		//Tags { "RenderType"="Opaque" }
		//LOD 100 Cull Off

		Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
		LOD 100

		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile ___ ANIM_LOOP

			#include "UnityCG.cginc"

			#define ts _PosTex_TexelSize

			struct appdata
			{
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float3 normal : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex, _PosTex, _NmlTex;
			float4 _PosTex_TexelSize;
			float _Length, _DT;
			float _Dist;
			
			v2f vert (appdata v, uint vid : SV_VertexID)
			{
				float t = (_Time.y - _DT) / _Length;
#if ANIM_LOOP
				t = fmod(t, 1.0);
#else
				t = saturate(t);
#endif
				float x = (vid + 0.5) * ts.x;
				float y = t;
				float4 pos = tex2Dlod(_PosTex, float4(x, y, 0, 0));
				float3 normal = tex2Dlod(_NmlTex, float4(x, y, 0, 0));

				v2f o;
				//o.vertex = UnityObjectToClipPos(pos);
				// Fixed Feature.
				//o.vertex = UnityObjectToClipPos(pos + float4(_SinTime.w, _SinTime.z, _SinTime.w,0));
				o.vertex = UnityObjectToClipPos( (pos + float4(sin( (pos.z + _Time.y) * _Dist+_Time.y) * 0.3f,0, 0, 0)) * float4(1.0f, 1.0f, 3.0f*sin(_Time.x * 10.0f % 1.5707963f), 1.0f));
				o.normal = UnityObjectToWorldNormal(normal);
				o.uv = v.uv;
				return o;
			}
			
			half4 frag (v2f i) : SV_Target
			{
				half diff = dot(i.normal, float3(0,1,0))*0.5 + 0.5;
				half4 col = tex2D(_MainTex, i.uv);
				half aa = sin(_Time.x * 10.0f % 1.5707963f);
				col.a = 1.0f - aa * aa;
				return diff * col;
			}
			ENDCG
		}
	}
}
