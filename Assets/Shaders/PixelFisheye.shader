Shader "Custom/PixelFisheye"
{
    Properties
    {
        _BlitTexture ("Base Texture", 2D) = "white" {}
        _PixelResolution ("Pixel Resolution", Vector) = (160, 90, 0, 0)
        _FisheyeStrength ("Fisheye Strength", Range(0, 2)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "PixelFisheye"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BlitTexture);
            SAMPLER(sampler_BlitTexture);
            float4 _BlitTexture_TexelSize;
            float2 _PixelResolution;
            float _FisheyeStrength;

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            // Generate full-screen triangle from vertex ID (no mesh needed)
            Varyings Vert(uint vertexID : SV_VertexID)
            {
                Varyings output;
                // Full-screen triangle: 3 vertices covering entire screen
                output.texcoord = float2((vertexID << 1) & 2, vertexID & 2);
                output.positionHCS = float4(output.texcoord * 2.0 - 1.0, 0.0, 1.0);
                // Flip Y for Unity's coordinate system
                output.positionHCS.y *= -1.0;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                // === Fisheye distortion ===
                float2 center = float2(0.5, 0.5);
                float2 offset = uv - center;
                float dist = length(offset);

                // Barrel distortion (fisheye)
                float strength = _FisheyeStrength;
                float distortion = 1.0 + strength * dist * dist;
                float2 distortedUV = center + offset * distortion;

                // Clamp to avoid sampling outside texture
                if (distortedUV.x < 0.0 || distortedUV.x > 1.0 ||
                    distortedUV.y < 0.0 || distortedUV.y > 1.0)
                {
                    return half4(0, 0, 0, 1);
                }

                // === Pixelation ===
                float2 pixelUV = floor(distortedUV * _PixelResolution) / _PixelResolution;

                return SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, pixelUV);
            }
            ENDHLSL
        }
    }
}
