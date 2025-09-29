Shader "Custom/ArrowLine"
{
    Properties
    {
        _MainTex ("Стрелки (текстура)", 2D) = "white" {}
        _Color   ("Цвет умножения", Color) = (1,1,1,1)
        _Speed   ("Скорость прокрутки", Float) = 2
        _Tiling  ("Повторы по длине", Float) = 5
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

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
                float4 color  : COLOR;
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
            float  _Speed;
            float  _Tiling;
            float  _Time; // из UnityCG

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                // u — вдоль линии, v — поперёк. Масштабируем u, + смещение временем.
                float2 uv = TRANSFORM_TEX(v.uv, _MainTex);
                uv.x = uv.x * _Tiling + _Time * _Speed;
                o.uv = uv;

                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                return tex * i.color;
            }
            ENDCG
        }
    }

    FallBack Off
}
