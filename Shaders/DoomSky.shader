Shader "Doom/DoomSky"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Palette ("Palette", 2D) = "white" {}
        _Colormap ("Colormap", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 cameraAngle : TEXCOORD0;
            };

            sampler2D _MainTex;
            sampler2D _Palette;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float pi = 3.14159;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                float3 cameraNormal = mul((float3x3)unity_CameraToWorld, float3(0,0,1));
                o.cameraAngle.x = cameraNormal.z;
                o.cameraAngle.z = atan2(cameraNormal.x, cameraNormal.z);
                // o.cameraAngle.y = atan2(cameraNormal.z, cameraNormal.y);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // return i.cameraAngle.x;

                float screenRatio = _ScreenParams.y / _ScreenParams.x;
                float textureRatio = _MainTex_TexelSize.z / _MainTex_TexelSize.w;
                float2 pos = i.vertex.xy / _ScreenParams.xy;// / ratio ;
                pos.x /= textureRatio;
                float f = pos.y - i.cameraAngle.x;
                

                pos.x+=i.cameraAngle.z;
                pos.y = 1.0 - pos.y;
                // pos.y += (i.cameraAngle.x * 0.5) -0.5;
                // pos.y = pos.y * (_MainTex_TexelSize.z / _MainTex_TexelSize.w) * .5;

                float paletteIndex = tex2D(_MainTex, pos).r;
                float4 col = tex2D(_Palette, float2(paletteIndex + (.5/256.0), 0.0) );
                return col;
            }
            ENDCG
        }
    }
}
