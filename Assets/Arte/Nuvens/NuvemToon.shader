// Nuvens toon (Sprint 6). Duas faixas de cor com sombra nas dobras entre os gomos (cor do vertice R = abertura),
// iluminadas pela MESMA luz da cena (cor do sol + ambiente do ceu), para acompanharem a hora do dia como as ilhas.
// A direcao do desenho das faixas e presa a camera (o sol do jogo fica quase atras dela e apagaria o volume).
// Teto de brilho sem mudar o matiz (fica abaixo do limiar do bloom) e nevoa por altura abaixo das ilhas.
// Gomos vivos: cada gomo (centro em UV2, dados em UV3) incha ou brota no proprio ritmo pelo relogio global
// _TempoDasNuvens (CamadaDeNuvens). Nascer e sumir: dissolucao por ruido (_Dissolver, por nuvem).
Shader "Loopia/Nuvem Toon"
{
    Properties
    {
        _CorLuz ("Cor na luz", Color) = (0.95, 0.96, 0.98, 1)
        _CorSombra ("Cor na sombra", Color) = (0.58, 0.65, 0.78, 1)
        _Limiar ("Limiar da sombra", Range(0, 1)) = 0.52
        _Suavidade ("Suavidade da transicao", Range(0.001, 0.5)) = 0.07
        _Desenho ("Direcao do desenho (x direita da camera, y cima, z para a camera)", Vector) = (-0.6, 0.75, 0.3, 0)
        _ForcaDoSol ("Forca do sol", Range(0, 2)) = 0.5
        _SolNaSombra ("Sol que ainda chega na sombra", Range(0, 1)) = 0.3
        _ForcaDoCeu ("Forca do ceu (ambiente)", Range(0, 2)) = 0.9
        _Dobras ("Sombra nas dobras", Range(0, 1)) = 0.85
        _Borda ("Brilho de borda no lado da sombra", Range(0, 1)) = 0.25
        _Teto ("Teto de brilho", Range(0.3, 1)) = 0.86
        _Nevoa ("Nevoa por altura (y sem nevoa, faixa, forca)", Vector) = (-3.5, 9, 0.45, 0)
        _Neblina ("Mistura com a neblina da camera", Range(0, 1)) = 1
        _Animacao ("Animacao dos gomos", Range(0, 1)) = 1
        _EscalaDoRuido ("Escala do ruido da dissolucao", Float) = 7
        _BordaDaDissolucao ("Borda da dissolucao", Range(0, 0.3)) = 0.07
        [HideInInspector] _FaseDaNuvem ("Fase da nuvem", Float) = 0
        [HideInInspector] _Dissolver ("Dissolver", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _CorLuz, _CorSombra, _Desenho, _Nevoa;
            float _Limiar, _Suavidade, _ForcaDoSol, _SolNaSombra, _ForcaDoCeu, _Dobras, _Borda, _Teto, _Neblina;
            float _Animacao, _EscalaDoRuido, _BordaDaDissolucao, _FaseDaNuvem, _Dissolver;
        CBUFFER_END
        float _TempoDasNuvens;

        struct Entrada
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 cor : COLOR;
            float4 centro : TEXCOORD2;
            float4 gomo : TEXCOORD3;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        // Gomo estavel (y = 0): so incha, de 1 a 1 + amplitude. Gomo brotando (y = 1): vai de "amplitude" a 1,08.
        // Malha sem esses canais (tudo zero) fica parada.
        float3 Deformar(float3 p, float4 centro, float4 gomo)
        {
            float onda = 0.5 + 0.5 * sin(_TempoDasNuvens * gomo.w + gomo.x + _FaseDaNuvem);
            float escala = gomo.y < 0.5
                ? 1.0 + gomo.z * onda * _Animacao
                : lerp(1.08, lerp(gomo.z, 1.08, onda), _Animacao);
            return centro.xyz + (p - centro.xyz) * escala;
        }

        float Espalhar(float3 p)
        {
            p = frac(p * 0.1031);
            p += dot(p, p.yzx + 33.33);
            return frac((p.x + p.y) * p.z);
        }
        float Ruido(float3 p)
        {
            float3 i = floor(p), f = frac(p);
            f = f * f * (3.0 - 2.0 * f);
            return lerp(lerp(lerp(Espalhar(i), Espalhar(i + float3(1, 0, 0)), f.x),
                             lerp(Espalhar(i + float3(0, 1, 0)), Espalhar(i + float3(1, 1, 0)), f.x), f.y),
                        lerp(lerp(Espalhar(i + float3(0, 0, 1)), Espalhar(i + float3(1, 0, 1)), f.x),
                             lerp(Espalhar(i + float3(0, 1, 1)), Espalhar(i + float3(1, 1, 1)), f.x), f.y), f.z);
        }
        // Margem acima do limiar: < 0 = recortado. Com _Dissolver 0 nada e recortado; com 1, tudo.
        float MargemDaDissolucao(float3 pObjeto)
        {
            float n = Ruido(pObjeto * _EscalaDoRuido) * 0.7 + Ruido(pObjeto * _EscalaDoRuido * 2.3 + 5.1) * 0.3;
            return n - (_Dissolver * 1.02 - 0.01);
        }
        ENDHLSL

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Saida
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float abertura : TEXCOORD2;
                float neblina : TEXCOORD3;
                float3 positionOS : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Saida vert(Entrada i)
            {
                Saida o;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs p = GetVertexPositionInputs(Deformar(i.positionOS.xyz, i.centro, i.gomo));
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.abertura = i.cor.r;
                o.neblina = ComputeFogFactor(p.positionCS.z);
                o.positionOS = i.positionOS.xyz;
                return o;
            }

            half4 frag(Saida i) : SV_Target
            {
                float margem = MargemDaDissolucao(i.positionOS);
                clip(margem);

                float3 n = normalize(i.normalWS);
                float3 v = normalize(GetWorldSpaceViewDir(i.positionWS));
                // Linhas da matriz de vista: [0] direita da camera, [2] aponta para a camera.
                float3 direita = UNITY_MATRIX_V[0].xyz;
                float3 paraCamera = UNITY_MATRIX_V[2].xyz;
                float3 desenho = normalize(direita * _Desenho.x + float3(0, 1, 0) * _Desenho.y + paraCamera * _Desenho.z);

                float abertura = lerp(1.0, saturate(i.abertura), _Dobras);
                float meiaLambert = dot(n, desenho) * 0.5 + 0.5;
                float t = smoothstep(_Limiar - _Suavidade, _Limiar + _Suavidade, meiaLambert * lerp(0.55, 1.0, abertura));
                // Perto do recorte da dissolucao a nuvem "esfiapa" na cor da sombra.
                if (_Dissolver > 0.001) t *= smoothstep(0.0, max(1e-3, _BordaDaDissolucao), margem);
                float3 tom = lerp(_CorSombra.rgb, _CorLuz.rgb, t) * lerp(0.82, 1.0, abertura);

                // Mesma luz das ilhas: cor do sol (dia/fim de tarde/lua) + ambiente do ceu.
                Light sol = GetMainLight();
                float3 luz = sol.color * _ForcaDoSol * lerp(_SolNaSombra, 1.0, t) + SampleSH(n) * _ForcaDoCeu;
                float3 cor = tom * luz;
                cor += pow(1.0 - saturate(dot(n, v)), 3.0) * _Borda * (1.0 - t) * luz * 0.5;

                // Teto de brilho preservando o matiz: compressao suave da luminancia e nenhum canal acima do teto.
                float joelho = _Teto * 0.8;
                float l = Luminance(cor);
                if (l > joelho)
                {
                    float acima = l - joelho;
                    float novo = joelho + acima / (1.0 + acima / max(1e-3, _Teto - joelho));
                    cor *= novo / l;
                }
                float maior = max(cor.r, max(cor.g, cor.b));
                if (maior > _Teto) cor *= _Teto / maior;

                // Quanto mais abaixo das ilhas, mais a nuvem se mistura com a cor da neblina (le como distancia).
                float nevoa = saturate((_Nevoa.x - i.positionWS.y) / max(0.01, _Nevoa.y)) * _Nevoa.z;
                float3 corDaNevoa = lerp(unity_FogColor.rgb, Luminance(unity_FogColor.rgb).xxx, 0.35);
                cor = lerp(cor, corDaNevoa, nevoa);
                cor = lerp(cor, MixFog(cor, i.neblina), _Neblina);
                return half4(cor, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            struct SaidaProfundidade { float4 positionCS : SV_POSITION; float3 positionOS : TEXCOORD0; };
            SaidaProfundidade vert(Entrada i)
            {
                SaidaProfundidade o;
                UNITY_SETUP_INSTANCE_ID(i);
                o.positionCS = TransformObjectToHClip(Deformar(i.positionOS.xyz, i.centro, i.gomo));
                o.positionOS = i.positionOS.xyz;
                return o;
            }
            half frag(SaidaProfundidade i) : SV_Target { clip(MargemDaDissolucao(i.positionOS)); return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            struct SaidaNormal { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 positionOS : TEXCOORD1; };
            SaidaNormal vert(Entrada i)
            {
                SaidaNormal o;
                UNITY_SETUP_INSTANCE_ID(i);
                o.positionCS = TransformObjectToHClip(Deformar(i.positionOS.xyz, i.centro, i.gomo));
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.positionOS = i.positionOS.xyz;
                return o;
            }
            half4 frag(SaidaNormal i) : SV_Target
            {
                clip(MargemDaDissolucao(i.positionOS));
                return half4(NormalizeNormalPerPixel(i.normalWS), 0);
            }
            ENDHLSL
        }
    }
}
