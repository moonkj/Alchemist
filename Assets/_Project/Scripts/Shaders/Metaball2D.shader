// WHY: URP 2D Renderer 파이프라인에서 metaball blend 을 ShaderGraph 없이 HLSL 로 구현.
// 인접 BlockView 의 alpha field 합이 threshold 를 초과하는 영역을 fill 해
// 두 블록이 액체처럼 붙어 보이는 연출 제공.
Shader "Alchemist/Metaball2D"
{
    Properties
    {
        _MainTex ("Sprite", 2D) = "white" {}
        _MetaRadius ("Radius", Float) = 0.5
        _MetaThreshold ("Threshold", Range(0,1)) = 0.6
        _MetaTint ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "MetaballUnlit"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float  _MetaRadius;
                float  _MetaThreshold;
                float4 _MetaTint;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vpi = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = vpi.positionCS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // WHY: uv 중심에서의 거리를 potential field 로 해석.
                // 한 블록 기준 최대 1.0 (중심)에서 0.0 (경계) 로 감쇠.
                float2 c = IN.uv - 0.5;
                float d = length(c);
                float field = saturate(1.0 - (d / max(_MetaRadius, 0.0001)));
                float alpha = step(_MetaThreshold, field) * field;

                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half4 col = tex * IN.color * _MetaTint;
                col.a *= alpha;
                return col;
            }
            ENDHLSL
        }
    }

    FallBack "Sprites/Default"
}
