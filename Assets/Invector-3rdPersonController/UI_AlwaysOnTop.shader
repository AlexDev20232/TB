Shader "Custom/UI_AlwaysOnTop"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}     // Текстура картинки
        _Color   ("Tint", Color) = (1,1,1,1)      // Цвет/прозрачность из компонента Image
    }

    SubShader
    {
        // Рисуем в прозрачном оверлее, чтобы было поверх
        Tags
        {
            "Queue"="Overlay"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        ZWrite Off          // Не пишем в depth buffer
        ZTest Always        // ВСЕГДА отрисовываемся поверх
        Cull Off            // Видно с обеих сторон
        Blend SrcAlpha OneMinusSrcAlpha   // Обычная альфа-прозрачность

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;   // Цвет вершин (UI Image его передаёт)
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float2 uv    : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.uv    = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                return tex * i.color;
            }
            ENDCG
        }
    }

    FallBack "Unlit/Transparent"
}
