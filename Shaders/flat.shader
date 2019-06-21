// Shader for the textures and flats in a level

Shader "Doom/Flat" {
 
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
            #include "doomlight.cginc"

            struct v2f {
                float4 vertex : SV_POSITION;
                float3 worldPosition : TEXCOORD0;
            };

            sampler2D _MainTex;
            sampler2D _Palette;
            sampler2D _Colormap;
            float _Brightness;

            v2f vert (appdata_base v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPosition = mul (unity_ObjectToWorld, v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                
                float odepth = doomLight(i.vertex.z, _Brightness);


                float indexCol = tex2D(_MainTex, i.worldPosition.xz).r;

                float alpha = tex2D(_MainTex, i.worldPosition.xz).a;
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