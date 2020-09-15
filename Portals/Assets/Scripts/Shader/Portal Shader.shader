Shader "Unlit/Portal Shader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    Category
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100
        Cull Off

        SubShader
        {
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
                    //float2 uv : TEXCOORD0;
                    UNITY_FOG_COORDS(1)
                    float4 vertex : SV_POSITION;
                    float4 screenPos : TEXCOORD0;
                };

                sampler2D _MainTex;
                float4 _MainTex_ST;
                int displayMask;

                v2f vert(appdata v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.screenPos = ComputeScreenPos(o.vertex);
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    float2 uv = i.screenPos.xy / i.screenPos.w;
                    fixed4 col = tex2D(_MainTex, uv);

                    return col * displayMask + float4(0, 0, 0, 1) * (1 - displayMask);
                }
                ENDCG
            }
        }
    }
    Fallback "Standard" // for shadows.. TODO remove?
}
