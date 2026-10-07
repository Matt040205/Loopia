Shader "Custom/SoftCloud"
{
    Properties
    {
        _Color ("Cor e opacidade", Color) = (1, 1, 1, 0.85)
        _Scale ("Detalhe do volume", Float) = 2.5
        _EdgeSoftness ("Difusao das bordas", Range(0.01, 1)) = 0.35
        _Density ("Densidade", Range(0, 1)) = 0.55
        _NoiseSpeed ("Movimento interno", Vector) = (0.02, 0.01, 0, 0)
        _CloudSeed ("Variacao da nuvem", Float) = 0
        _Extinction ("Espessura optica", Range(1, 12)) = 6
        _ShadowStrength ("Sombra no volume", Range(0, 5)) = 2
        _DepthSoftness ("Difusao junto a objetos", Float) = 1.2
        _FogBlend ("Mistura com a neblina", Range(0, 1)) = 1
        _FogFade ("Dissipacao na neblina", Range(0, 1)) = 0.2
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend One OneMinusSrcAlpha
        ZWrite Off
        // A profundidade real e avaliada em cada amostra do volume.
        ZTest Always
        Cull Off
        Pass
        {
            Name "CloudVolume"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _NoiseSpeed;
                float _Scale, _EdgeSoftness, _Density, _CloudSeed;
                float _Extinction, _ShadowStrength, _DepthSoftness, _FogBlend, _FogFade;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionOS = input.positionOS.xyz;
                output.positionWS = positions.positionWS;
                return output;
            }

            float hash(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }
            float noise(float3 p)
            {
                float3 i = floor(p), f = frac(p);
                f = f*f*(3.0-2.0*f);
                return lerp(lerp(lerp(hash(i), hash(i+float3(1,0,0)), f.x),
                                 lerp(hash(i+float3(0,1,0)), hash(i+float3(1,1,0)), f.x), f.y),
                            lerp(lerp(hash(i+float3(0,0,1)), hash(i+float3(1,0,1)), f.x),
                                 lerp(hash(i+float3(0,1,1)), hash(i+float3(1,1,1)), f.x), f.y), f.z);
            }
            float smoothUnion(float a, float b)
            {
                float k = 0.3;
                float h = saturate(0.5+0.5*(b-a)/k);
                return lerp(b,a,h)-k*h*(1-h);
            }
            float ellipsoid(float3 p, float3 center, float3 radius)
            {
                return length((p-center)/radius)-1;
            }
            float density(float3 p)
            {
                float variation = sin(_CloudSeed * 1.71);
                float shape = ellipsoid(p,float3(-.22,-.09,0),float3(.24,.27,.26));
                shape = smoothUnion(shape,ellipsoid(p,float3(.02,.03,-.04),float3(.28,.34,.3)));
                shape = smoothUnion(shape,ellipsoid(p,float3(.23,-.06,.02),float3(.23,.25,.26)));
                shape = smoothUnion(shape,ellipsoid(p,float3(-.09,.15,.03),float3(.2,.2,.23)));
                shape = smoothUnion(shape,ellipsoid(p,float3(.15,.13+variation*.035,.05),float3(.18,.21,.22)));
                float3 uv = p*max(1,_Scale)*2 + float3(_CloudSeed*.47,_CloudSeed*.21,_CloudSeed*.33);
                uv += float3(_NoiseSpeed.xy,_NoiseSpeed.x*.4)*_Time.y;
                float n = noise(uv)*.7+noise(uv*2.13+7.1)*.3;
                float d = saturate((-shape+(n-.5)*.45)/max(.08,_EdgeSoftness));
                // O volume termina antes das bordas do quad, sem contorno retangular.
                float border = saturate((.5-max(max(abs(p.x),abs(p.y)),abs(p.z)))/.055);
                return d*border*_Density;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 viewWS = normalize(GetWorldSpaceViewDir(input.positionWS));
                float3 rayDir = normalize(mul((float3x3)unity_WorldToObject,-viewWS));
                float3 origin = input.positionOS-rayDir*2;
                float3 safeDir = float3(abs(rayDir.x)<.00001?.00001:rayDir.x,
                                        abs(rayDir.y)<.00001?.00001:rayDir.y,
                                        abs(rayDir.z)<.00001?.00001:rayDir.z);
                float3 t0=(-.5-origin)/safeDir, t1=(.5-origin)/safeDir;
                float3 nearAxis=min(t0,t1), farAxis=max(t0,t1);
                float enter=max(max(nearAxis.x,nearAxis.y),nearAxis.z);
                float exit=min(min(farAxis.x,farAxis.y),farAxis.z);
                if(exit<=enter) return 0;

                float rawDepth=SampleSceneDepth(GetNormalizedScreenSpaceUV(input.positionCS.xy));
                float sceneEye=unity_OrthoParams.w>.5 ? LinearDepthToEyeDepth(rawDepth) :
                    LinearEyeDepth(rawDepth,_ZBufferParams);
                Light sun=GetMainLight();
                float3 sunOS=normalize(mul((float3x3)unity_WorldToObject,sun.direction));
                float3 ambient=max(SampleSH(float3(0,1,0)),float3(.035,.04,.05));
                float stepLength=(exit-enter)/24;
                float3 accumulated=0;
                float opacity=0;
                [loop] for(int i=0;i<24;i++)
                {
                    float3 p=origin+rayDir*(enter+(i+.5)*stepLength);
                    float d=density(p);
                    if(d<.001) continue;
                    float3 sampleWS=TransformObjectToWorld(p);
                    float sampleEye=-TransformWorldToView(sampleWS).z;
                    float softDepth=saturate((sceneEye-sampleEye)/max(.01,_DepthSoftness));
                    float a=(1-exp(-d*_Extinction*stepLength))*softDepth;
                    float occlusion=density(p+sunOS*.12)+density(p+sunOS*.25)*.6;
                    float transmission=exp(-occlusion*_ShadowStrength);
                    float3 lighting=ambient*.85+sun.color*(.22+.78*transmission);
                    accumulated+=(1-opacity)*a*lighting;
                    opacity+=(1-opacity)*a;
                    if(opacity>.985) break;
                }
                if(opacity<.001) return 0;
                float3 color=accumulated/opacity*_Color.rgb;
                float fog=InitializeInputDataFog(float4(input.positionWS,1),0);
                color=lerp(color,MixFog(color,fog),_FogBlend);
                bool fogActive=false;
                #if defined(FOG_LINEAR_KEYWORD_DECLARED)
                    fogActive=fogActive||FOG_LINEAR;
                #endif
                #if defined(FOG_EXP_KEYWORD_DECLARED)
                    fogActive=fogActive||FOG_EXP;
                #endif
                #if defined(FOG_EXP2_KEYWORD_DECLARED)
                    fogActive=fogActive||FOG_EXP2;
                #endif
                if(fogActive&&IsFogEnabled()) opacity*=lerp(1,ComputeFogIntensity(fog),_FogFade);
                opacity*=saturate(_Color.a);
                return half4(color*opacity,opacity);
            }
            ENDHLSL
        }
    }
}
