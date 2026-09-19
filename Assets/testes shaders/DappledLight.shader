Shader "Custom/DappledLight"
{
    // Efeito de sombra de copa de árvore ("dappled light"), 100% procedural
    // (nenhuma textura externa necessária). Multiply-blend: escurece o que
    // está embaixo em manchas orgânicas que se movem lentamente, simulando
    // vento passando pelas folhas.
    Properties
    {
        _Color ("Cor da sombra", Color) = (0.15, 0.18, 0.25, 1) // sombra levemente azulada, não preto puro
        _Scale ("Escala do ruído (tamanho das manchas)", Float) = 4
        _Speed ("Velocidade do vento (X,Y)", Vector) = (0.04, 0.025, 0, 0)
        _Density ("Densidade (quanto da área fica coberta)", Range(0,1)) = 0.55
        _Softness ("Suavidade da borda das manchas", Range(0.01, 1)) = 0.35
        _Opacity ("Opacidade geral", Range(0,1)) = 0.6
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend DstColor Zero // Multiply: resultado = cor_de_baixo * cor_desse_shader
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
                float4 _Speed;
                float _Density;
                float _Softness;
                float _Opacity;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            // Hash 2D simples, base pra gerar ruído sem textura
            float hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // Ruído de valor (value noise) suavizado — cada "célula" tem um valor
            // aleatório e interpolamos suavemente entre elas.
            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float a = hash(i);
                float b = hash(i + float2(1, 0));
                float c = hash(i + float2(0, 1));
                float d = hash(i + float2(1, 1));

                float2 u = f * f * (3.0 - 2.0 * f); // smoothstep interno
                return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Duas camadas de ruído em escalas diferentes = manchas mais orgânicas,
                // menos "quadriculadas" do que uma única camada.
                float2 scrolledUV = IN.uv * _Scale + _Speed.xy * _Time.y;
                float n = valueNoise(scrolledUV) * 0.65 + valueNoise(scrolledUV * 2.17 + 13.7) * 0.35;

                // mask = 1 onde a "folha" bloqueia luz (escurece), 0 onde a luz passa livre
                float mask = smoothstep(_Density - _Softness, _Density + _Softness, n);
                float darkenAmount = mask * _Opacity;

                // Interpola entre "branco" (não escurece nada, multiply neutro) e a cor de sombra
                half3 result = lerp(half3(1, 1, 1), _Color.rgb, darkenAmount);
                return half4(result, 1);
            }
            ENDHLSL
        }
    }
}
