Shader "Projection/Homography"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
        _BackgroundColor("Background Color", Color) = (0, 0, 0, 0)
        _EdgeSmoothness("Edge Smoothness", Float) = 0.5
	}
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			sampler2D _MainTex;
            float4 _BackgroundColor;
            float _EdgeSmoothness;
            // we end up using a 4x4 matrix instead of a 3x3 matrix as Unity only has a built in type for 4x4 matrices
            float4x4 _HomographyMatrix;

			fixed4 frag (v2f i) : SV_Target
			{
                // apply homography on uv:
                // homogeneous coordinates, and add one dimensions to match 4x4 matrix
                float4 huv = float4(i.uv, 1, 0);
                float4 r = mul(_HomographyMatrix, huv);
                // non homogeneous coordinates
                float2 uv = float2(r.x / r.z, r.y / r.z);

                float d = 0.05 * _EdgeSmoothness;
                float backgroundFactor = max(
                    smoothstep(0.5 - d, 0.5, abs(uv.x - 0.5)),
                    smoothstep(0.5 - d, 0.5, abs(uv.y - 0.5)));

				return lerp(tex2D(_MainTex, uv), _BackgroundColor, backgroundFactor);
			}
			ENDCG
		}
	}
}
