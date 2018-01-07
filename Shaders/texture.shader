// Shader for the textures and flats in a level

Shader "Doom/Texture" {
 
Properties {
    _MainTex ("Render Map", 2D) = "white" {}
    _Palette ("Palette", 2D) = "white" {}
    _Colormap ("Colormap", 2D) = "white" {}
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
            sampler2D _Palette;
            sampler2D _Colormap;
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
                float depth = 1.0 - saturate(i.vertex.z);
                #if defined(UNITY_REVERSED_Z)
                    depth = 1.0 - depth;
                #endif
                depth *= 32.0;
                float li = (_Brightness * 2.0) - (224.0 / 256.0);
                li = saturate(li);
                float maxlight = (_Brightness * 2.0) - (40.0 / 256.0);
                maxlight = saturate(maxlight);
                float dscale = depth * 0.4 * (1.0 - unity_OrthoParams.w);
                float odepth = saturate(li + dscale) + 0.01;


                float indexCol = tex2D(_MainTex, i.texcoord).r;

                float alpha = tex2D(_MainTex, i.texcoord).a;
                float colormapIndex = indexCol;
                float brightnessLookup = (floor((1.0-odepth) * 32.0)) / 32.0;

                float paletteIndex = tex2D(_Colormap, float2(colormapIndex + (0.5/256.0), brightnessLookup * (32.0/34.0)));

                
                float4 col = tex2D(_Palette, float2(paletteIndex + (.5/256.0), 0.0));
                col.a = alpha;
                clip(col.a - 0.9);
                return col;
            }
        ENDCG
    }
}

}