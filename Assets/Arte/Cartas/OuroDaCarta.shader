Shader "Loopia/UI/Ouro da carta"
{
    Properties
    {
        [PerRendererData] _MainTex ("Moldura", 2D) = "white" {}
        _Ouro ("Gradiente dourado", 2D) = "white" {}
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="False" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Moldura"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct V
            {
                float4 pos : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 cor : COLOR;
            };

            struct F
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 local : TEXCOORD1;
                fixed4 cor : COLOR;
            };

            sampler2D _MainTex;
            sampler2D _Ouro;
            float4 _ClipRect;

            F vert(V v)
            {
                F o;
                o.pos = UnityObjectToClipPos(v.pos);
                o.uv = v.uv;
                o.local = v.pos.xy;
                o.cor = v.cor;
                return o;
            }

            fixed4 frag(F i) : SV_Target
            {
                fixed4 molde = tex2D(_MainTex, i.uv);
                // O canal G do vertice controla o realce. O pigmento da moldura vem do PNG.
                float realce = 1.0 - i.cor.g;
                float avanco = frac(_Time.y * .16);
                float diagonal = i.uv.x + (1.0 - i.uv.y) * .58;
                float centro = lerp(-.26, 1.84, avanco);
                float faixa = 1.0 - smoothstep(.02, .23, abs(diagonal - centro));
                float metal = smoothstep(.07, .30, molde.r - molde.b);
                fixed3 ouro = tex2D(_Ouro, float2(saturate(.25 + .54 * faixa), .5)).rgb;
                molde.rgb = lerp(molde.rgb, max(molde.rgb, ouro),
                    metal * (.08 * realce + faixa * (.12 + .28 * realce)));
                molde.a *= i.cor.a;
                #ifdef UNITY_UI_CLIP_RECT
                    molde.a *= UnityGet2DClipping(i.local, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                    clip(molde.a - .001);
                #endif
                return molde;
            }
            ENDCG
        }
    }
}
