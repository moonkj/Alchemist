// WHY: Vertex 단에서 사인파 기반 squash/stretch 를 적용해 jelly 탄성 효과.
// CPU JellyDeformer 가 transform.localScale 만 조작하는 것과 별도로, 모서리별 비균질 변형이 필요할 때
// Mid/High 품질에서만 활성화.
Shader "Alchemist/JellyDeform"
{
    Properties
    {
        _MainTex ("Sprite", 2D) = "white" {}
        _JellyAmplitude ("Amplitude", Range(0,0.3)) = 0.05
        _JellyFrequency ("Frequency", Range(0,20)) = 6.0
        _JellyPhase ("Phase", Float) = 0.0
        _Tint ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "JellyUnlit"
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
                float  _JellyAmplitude;
                float  _JellyFrequency;
                float  _JellyPhase;
                float4 _Tint;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                // WHY: uv.y 기준 사인파로 x 방향 오프셋 주어 sway 만 표현 (상하 대칭 wobble 방지).
                float wave = sin(IN.uv.y * _JellyFrequency + _JellyPhase) * _JellyAmplitude;
                float3 pos = IN.positionOS.xyz;
                pos.x += wave;
                VertexPositionInputs vpi = GetVertexPositionInputs(pos);
                OUT.positionCS = vpi.positionCS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                return tex * IN.color * _Tint;
            }
            ENDHLSL
        }
    }

    FallBack "Sprites/Default"
}
