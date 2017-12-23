// Shader for the sky. Requires CameraDepth.cs to be attached to the player camera to function correctly.

Shader "Doom/Sky" {
 
Properties {
    _RenderMap ("Render Map", 2D) = "white" {}
    _Palette ("Palette", 2D) = "white" {}
    _CameraAngle ("Camera Angle", float) = 1.0
}

SubShader {
    Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Opaque"}
    LOD 200

    Blend SrcAlpha OneMinusSrcAlpha 

    Pass {  
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define M_PI 3.1415926535897932384626433832795

            #include "UnityCG.cginc"

            struct v2f {
                float4 vertex : SV_POSITION;
            };

            sampler2D _RenderMap;
            sampler2D _Palette;
            float4 _RenderMap_TexelSize;
            float _CameraAngle;

            v2f vert (appdata_base v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 texcoord = i.vertex / _ScreenParams.xy;
                texcoord.x += (_CameraAngle * (3.0/360));
                texcoord.y = -texcoord.y * (_RenderMap_TexelSize.z / _RenderMap_TexelSize.w) * 0.5;
                float indexCol = tex2D(_RenderMap, texcoord).r;
                float4 col = tex2D(_Palette, float2(indexCol + (.5/256.0), 0.0));
                return col;
            }
        ENDCG
    }
}

}