Shader "Custom/SoftCloud"
{
    // Nuvem procedural (sem textura externa): um miolo denso com borda
    // suave, quebrada por ruído pra parecer orgânica em vez de um círculo
    // perfeito. Pensada pra ficar embaixo das ilhas flutuantes, reforçando
    // a leitura de altura/voo.
    Properties
    {
        _Color ("Cor da nuvem", Color) = (1, 1, 1, 0.85)
        _Scale ("Escala do ruído (detalhe da silhueta)", Float) = 2.5
        _EdgeSoftness ("Suavidade da borda", Range(0.01, 1)) = 0.35
        _Density ("Densidade (tamanho do miolo)", Range(0, 1)) = 0.55
        _NoiseSpeed ("Velocidade de variação da silhueta (X,Y)", Vector) = (0.02, 0.01, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Scale;
                float _EdgeSoftness;
                float _Density;
                float4 _NoiseSpeed;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = hash(i);
                float b = hash(i + float2(1, 0));
                float c = hash(i + float2(0, 1));
                float d = hash(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Círculo suave: miolo denso (perto de 1) até borda transparente (perto de 0)
                float2 centered = IN.uv - 0.5;
                float dist = length(centered) * 2.0; // 0 = centro, 1 = borda do quad
                float baseShape = 1.0 - dist;

                // Ruído em duas camadas pra quebrar a borda circular perfeita
                float2 noiseUV = IN.uv * _Scale + _NoiseSpeed.xy * _Time.y;
                float n = valueNoise(noiseUV) * 0.6 + valueNoise(noiseUV * 2.3 + 9.1) * 0.4;

                float shape = baseShape + (n - 0.5) * 0.6;
                float alpha = smoothstep(1.0 - _Density - _EdgeSoftness, 1.0 - _Density + _EdgeSoftness, shape);

                half4 result = _Color;
                result.a *= alpha;
                return result;
            }
            ENDHLSL
        }
    }
}
