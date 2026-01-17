Shader "FallingSand/WorldRender"
{
    Properties
    {
        _CellTex ("Cell Texture", 2D) = "white" {}
        _PaletteTex ("Palette Texture", 2D) = "white" {}
        _VariationTex ("Variation Texture", 2D) = "black" {}
        _EmissionTex ("Emission Texture", 2D) = "black" {}
        _DensityTex ("Density Texture", 2D) = "black" {}
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
            TEXTURE2D(_VariationTex);
            SAMPLER(sampler_VariationTex);
            TEXTURE2D(_EmissionTex);
            SAMPLER(sampler_EmissionTex);
            TEXTURE2D(_DensityTex);
            SAMPLER(sampler_DensityTex);

            float4 _CellTex_TexelSize;

            // Effect toggles (set via Shader.SetGlobalFloat)
            float _NoiseEnabled;
            float _LightingEnabled;
            float _SoftEdgesEnabled;
            float _GlowIntensity;

            // Light direction for pseudo-lighting
            static const float3 _LightDir = normalize(float3(0.3, -0.5, 1.0));

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            // Helper to get material ID at UV
            float GetMaterialId(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_CellTex, sampler_CellTex, uv).r;
            }

            // Helper to get color from palette
            half4 GetPaletteColor(float materialId)
            {
                return SAMPLE_TEXTURE2D(_PaletteTex, sampler_PaletteTex, float2(materialId, 0.5));
            }

            // Helper to get density at UV
            float GetDensity(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_DensityTex, sampler_DensityTex, uv).r;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float2 texel = _CellTex_TexelSize.xy;

                // Sample material ID (stored in red channel, 0-1 range maps to 0-255)
                float materialId = GetMaterialId(uv);

                // Look up colour from palette
                half4 colour = GetPaletteColor(materialId);

                // Cell position for noise
                float2 cellPos = floor(uv * _CellTex_TexelSize.zw);
                float noise = frac(sin(dot(cellPos, float2(12.9898, 78.233))) * 43758.5453);

                // === NOISE VARIATION EFFECT ===
                if (_NoiseEnabled > 0.5)
                {
                    // Sample variation amount for this material
                    float variation = SAMPLE_TEXTURE2D(_VariationTex, sampler_VariationTex, float2(materialId, 0.5)).r;
                    float noiseAmount = variation * 0.25; // Scale factor
                    colour.rgb *= 1.0 - noiseAmount + noise * noiseAmount * 2.0;
                }
                else
                {
                    // Default subtle variation
                    colour.rgb *= 0.95 + noise * 0.1;
                }

                // === SOFT EDGES EFFECT ===
                if (_SoftEdgesEnabled > 0.5)
                {
                    // Sample neighbor material IDs
                    float matL = GetMaterialId(uv + float2(-texel.x, 0));
                    float matR = GetMaterialId(uv + float2(texel.x, 0));
                    float matU = GetMaterialId(uv + float2(0, texel.y));
                    float matD = GetMaterialId(uv + float2(0, -texel.y));

                    // Check if we're at a boundary (neighbor has different material)
                    float threshold = 0.004; // ~1/256 for material ID difference
                    bool isBoundary = abs(matL - materialId) > threshold ||
                                      abs(matR - materialId) > threshold ||
                                      abs(matU - materialId) > threshold ||
                                      abs(matD - materialId) > threshold;

                    if (isBoundary)
                    {
                        // Blend with neighbor colors
                        half4 colL = GetPaletteColor(matL);
                        half4 colR = GetPaletteColor(matR);
                        half4 colU = GetPaletteColor(matU);
                        half4 colD = GetPaletteColor(matD);
                        half4 avgColor = (colL + colR + colU + colD) * 0.25;
                        colour = lerp(colour, avgColor, 0.35);
                    }
                }

                // === LIGHTING EFFECT ===
                if (_LightingEnabled > 0.5)
                {
                    // Compute normal from density gradient
                    float densL = GetDensity(uv + float2(-texel.x, 0));
                    float densR = GetDensity(uv + float2(texel.x, 0));
                    float densU = GetDensity(uv + float2(0, texel.y));
                    float densD = GetDensity(uv + float2(0, -texel.y));

                    float3 normal = normalize(float3(densL - densR, densD - densU, 0.5));
                    float lighting = saturate(dot(normal, _LightDir));
                    colour.rgb *= 0.7 + lighting * 0.4;
                }

                // === GLOW EFFECT ===
                if (_GlowIntensity > 0.01)
                {
                    float emission = SAMPLE_TEXTURE2D(_EmissionTex, sampler_EmissionTex, float2(materialId, 0.5)).r;
                    // Output HDR values for emissive materials
                    colour.rgb += colour.rgb * emission * _GlowIntensity * 2.0;
                }

                return colour;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
