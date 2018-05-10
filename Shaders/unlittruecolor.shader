// Shader used for UI and other sprites that aren't placed in a level. Not affected by depth.

Shader "Doom/Unlit Truecolor Texture" {
 
Properties {
    _MainTex ("Texture", 2D) = "white" {}
    _Brightness ("Brightness", float) = 1.0
}

SubShader {
    Tags {"Queue"="Geometry" "IgnoreProjector"="True" "RenderType"="Opaque"}
    LOD 200

    Pass {  
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float _Brightness;
            float4 _MainTex_ST;

            v2f vert (appdata_base v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);                
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

        fixed4 frag (v2f i) : SV_Target
        {
            float4 col = tex2D(_MainTex, i.texcoord );
            clip(col.a - 0.9);
            return col;
        }
        ENDCG
    }
}

}