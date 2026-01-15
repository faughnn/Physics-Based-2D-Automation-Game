Shader "FallingSand/WorldRender"
{
    Properties
    {
        _CellTex ("Cell Texture", 2D) = "white" {}
        _PaletteTex ("Palette Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "Universal2D"
            Tags { "LightMode" = "Universal2D" }

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
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_CellTex);
            SAMPLER(sampler_CellTex);
            TEXTURE2D(_PaletteTex);
            SAMPLER(sampler_PaletteTex);

            float4 _CellTex_TexelSize;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Sample material ID (stored in red channel, 0-1 range maps to 0-255)
                float materialId = SAMPLE_TEXTURE2D(_CellTex, sampler_CellTex, input.uv).r;

                // Look up colour from palette (materialId is already 0-1, use as U coordinate)
                half4 colour = SAMPLE_TEXTURE2D(_PaletteTex, sampler_PaletteTex, float2(materialId, 0.5));

                // Add subtle variation based on cell position for visual interest
                float2 cellPos = floor(input.uv * _CellTex_TexelSize.zw);
                float noise = frac(sin(dot(cellPos, float2(12.9898, 78.233))) * 43758.5453);
                colour.rgb *= 0.95 + noise * 0.1;

                return colour;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
