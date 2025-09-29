Shader "Custom/Gradient/Unlit"
{
    Properties
    {
        _TopColor    ("Верхний цвет", Color) = (1,1,1,1)
        _BottomColor ("Нижний цвет", Color)  = (0,0,0,1)
        _Axis     ("Ось (x,y,z)", Vector) = (0,1,0,0)
        _UseWorld ("Пространство (0=Local,1=World)", Float) = 0
        _Offset ("Смещение", Float) = 0
        _Scale  ("Длина (1/длина)", Float) = 1
    }
    SubShader
    {
        Tags{ "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _TopColor, _BottomColor;
            float4 _Axis;
            float  _UseWorld, _Offset, _Scale;

            struct v2f {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 localPos : TEXCOORD1;
            };

            v2f vert(appdata_full v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.localPos = v.vertex.xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 pos  = (_UseWorld > 0.5) ? i.worldPos : i.localPos;
                float3 axis = normalize(_Axis.xyz + 1e-6);
                float t = dot(pos, axis);
                t = (t - _Offset) * _Scale;
                t = saturate(t);
                return lerp(_BottomColor, _TopColor, t);
            }
            ENDCG
        }
    }
}
