// Gaze reticle: a thin ring drawn from the quad's UVs, always on top (ignores depth, so it also shows over the
// passthrough image and over the object it points at). Lives in a Resources folder so builds always include it.
Shader "DiyVr/ReticleOverlay"
{
    Properties
    {
        _Color ("Colour", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Overlay" "IgnoreProjector" = "True" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float r = length(i.uv - 0.5) * 2.0;           // 0 centre, 1 edge of the quad
                float outer = 1.0 - smoothstep(0.90, 1.0, r);
                float inner = smoothstep(0.55, 0.65, r);      // hole in the middle
                fixed4 c = _Color;
                c.a *= outer * inner;
                return c;
            }
            ENDCG
        }
    }
}
