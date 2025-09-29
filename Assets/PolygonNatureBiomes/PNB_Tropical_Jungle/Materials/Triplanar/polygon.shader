// Assets/Shaders/TriplanarToonUnlit.shader
Shader "SyntyStudios/TriplanarToonUnlit"
{
    Properties
    {
        // Трипланарные параметры
        _FallOff ("FallOff (острота смешивания)", Range(0,100)) = 100
        _Tiling  ("Tiling (масштаб UV)", Range(0,10)) = 1

        _Sides ("Sides (X/Z)", 2D) = "white" {}
        _Top   ("Top (Y+)",   2D) = "white" {}

        _Tint       ("Tint", Color) = (1,1,1,1)
        _Saturation ("Saturation", Range(0,2)) = 1.2
        _Brightness ("Brightness", Range(0,2)) = 1.1

        // Toon-шэйдинг (собственный, без Unity Light)
        _LightDir   ("Light Dir (world)", Vector) = (0.3, 1, 0.2, 0)
        _LightColor ("Light Color", Color) = (1,1,1,1)
        _Ambient    ("Ambient (0..1)", Range(0,1)) = 0.45
        _Steps      ("Steps (>=1)", Range(1,8)) = 3

        // Контур (опционально)
        _OutlineWidth ("Outline Width", Range(0,5)) = 0
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Cull Back
        ZWrite On

        CGINCLUDE
        #include "UnityCG.cginc"

        sampler2D _Sides;
        sampler2D _Top;
        float _Tiling;
        float _FallOff;
        float4 _Tint;
        float _Saturation;
        float _Brightness;

        float4 _LightDir;       // xyz — направление в мире
        float4 _LightColor;
        float  _Ambient;
        float  _Steps;

        struct appdata
        {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
        };

        struct v2f
        {
            float4 pos        : SV_POSITION;
            float3 worldPos   : TEXCOORD0;
            float3 worldNormal: TEXCOORD1;
        };

        v2f vert (appdata v)
        {
            v2f o;
            o.pos = UnityObjectToClipPos(v.vertex);
            o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            o.worldNormal = UnityObjectToWorldNormal(v.normal);
            return o;
        }

        // Трипланар: topTex для Y+, sidesTex для X/Z и для Y-
        float3 TriplanarAlbedo(sampler2D topTex, sampler2D sidesTex,
                               float3 wPos, float3 wNrm, float falloff, float tiling)
        {
            float3 n = normalize(wNrm);
            float3 an = pow(abs(n), falloff);
            an /= (an.x + an.y + an.z) + 1e-5;

            float3 sgn = sign(n);

            float2 uvx = tiling * wPos.zy * float2( sgn.x, 1);
            float2 uvy = tiling * wPos.xz * float2( sgn.y, 1);
            float2 uvz = tiling * wPos.xy * float2(-sgn.z, 1);

            float3 colX   = tex2D(sidesTex, uvx).rgb;
            float3 colYup = tex2D(topTex,   uvy).rgb;
            float3 colYdn = tex2D(sidesTex, uvy).rgb; // Y- берём как «боковой»
            float3 colZ   = tex2D(sidesTex, uvz).rgb;

            float negY = max(0, an.y * -sgn.y);
            float posY = max(0, an.y *  sgn.y);

            return colX * an.x + colZ * an.z + colYup * posY + colYdn * negY;
        }

        // Квантование лэмберта на ступени
        float ToonShade(float3 nWorld)
        {
            float3 L = normalize(_LightDir.xyz);
            float lambert = saturate(dot(normalize(nWorld), L));
            // дискретизация
            float steps = max(1.0, _Steps);
            float q = floor(lambert * steps) / (steps - 0.0001); // слегка растягиваем к 1
            // смесь с амбиентом
            return lerp(_Ambient, 1.0, q);
        }

        // Функция увеличения насыщенности
        float3 ApplySaturation(float3 color, float saturation)
        {
            float luminance = dot(color, float3(0.299, 0.587, 0.114));
            return lerp(luminance.rrr, color, saturation);
        }

        fixed4 frag (v2f i) : SV_Target
        {
            float3 albedo = TriplanarAlbedo(_Top, _Sides, i.worldPos, i.worldNormal, _FallOff, _Tiling);
            float shade = ToonShade(i.worldNormal);
            
            // Применяем насыщенность и яркость
            albedo = ApplySaturation(albedo, _Saturation);
            albedo *= _Brightness;
            
            float3 lit = albedo * _Tint.rgb * (_LightColor.rgb * shade);
            return fixed4(lit, 1);
        }
        ENDCG

        // Основной unlit-проход
        Pass
        {
            Name "UnlitToon"
            Tags { "LightMode"="Always" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            ENDCG
        }

        // Контур (опционально): если _OutlineWidth = 0 — визуально не рисуется
        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite On
            CGPROGRAM
            #pragma vertex vertOutline
            #pragma fragment fragOutline
            #pragma target 3.0
            #include "UnityCG.cginc"

            float _OutlineWidth;
            float4 _OutlineColor;

            struct vIn { float4 vertex:POSITION; float3 normal:NORMAL; };
            struct vOut { float4 pos:SV_POSITION; };

            vOut vertOutline(vIn v)
            {
                vOut o;
                // Экструзия по нормали в объектном пространстве
                float3 n = normalize(v.normal);
                float3 extruded = v.vertex.xyz + n * (_OutlineWidth * 0.01); // 0.01 = условный масштаб
                o.pos = UnityObjectToClipPos(float4(extruded,1));
                return o;
            }

            fixed4 fragOutline() : SV_Target { return _OutlineColor; }
            ENDCG
        }
    }

    Fallback Off
}