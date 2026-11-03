// Unlit textured shader used for the distortion meshes and the test content.
// Works in URP (no LightMode tag = SRPDefaultUnlit). With _BlackOutside = 1, UVs outside 0..1 give black.
Shader "DiyVr/StereoUnlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        [Toggle] _BlackOutside ("Black outside UV 0..1", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        Cull Off
        ZWrite On
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _BlackOutside;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * _Color;
                if (_BlackOutside > 0.5 && (i.uv.x < 0 || i.uv.x > 1 || i.uv.y < 0 || i.uv.y > 1))
                    c = fixed4(0, 0, 0, 1);
                return c;
            }
            ENDCG
        }
    }
}
