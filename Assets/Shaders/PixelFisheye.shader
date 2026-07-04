Shader "Custom/PixelFisheye"
{
    Properties
    {
        _BlitTexture ("Base Texture", 2D) = "white" {}
        _PixelResolution ("Pixel Resolution", Vector) = (160, 90, 0, 0)
        _FisheyeStrength ("Fisheye Strength", Range(0, 2)) = 0.5
        _SweepProgress ("Sweep Progress", Range(0, 1)) = 0
        _SweepWidth ("Sweep Width", Range(0.01, 0.5)) = 0.12
        _SweepIntensity ("Sweep Intensity", Range(0, 1)) = 0.28
        _SweepBlur ("Sweep Blur", Range(0, 1)) = 0.35
        _SweepColorShift ("Sweep Color Shift", Range(0, 1)) = 0.04
        _SweepNoise ("Sweep Noise", Range(0, 1)) = 0.02
        _SweepBrightness ("Sweep Brightness", Range(0, 1)) = 0.03
        _BorderIntensity ("Border Intensity", Range(0, 1)) = 0.22
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
            float _SweepProgress;
            float _SweepWidth;
            float _SweepIntensity;
            float _SweepBlur;
            float _SweepColorShift;
            float _SweepNoise;
            float _SweepBrightness;
            float _BorderIntensity;

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
                float aspect = _ScreenParams.x / max(_ScreenParams.y, 1.0);
                float2 radialOffset = float2(offset.x * aspect, offset.y);
                float dist = length(radialOffset);

                float strength = _FisheyeStrength;
                float distortion = 1.0 + strength * dist * dist;
                float2 distortedOffset = radialOffset * distortion;
                distortedOffset.x /= aspect;
                float2 distortedUV = saturate(center + distortedOffset);

                // === Pixelation ===
                float2 pixelUV = floor(distortedUV * _PixelResolution) / _PixelResolution;

                half4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, pixelUV);

                // === Top-to-bottom sweep filter ===
                float sweepDistance = abs(uv.y - _SweepProgress);
                float sweepMask = 1.0 - smoothstep(_SweepWidth * 0.35, _SweepWidth, sweepDistance);
                sweepMask *= _SweepIntensity;

                float2 texel = 1.0 / max(_PixelResolution, float2(1.0, 1.0));
                half4 filtered = color;
                filtered.rgb += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, saturate(pixelUV + float2(-texel.x, 0))).rgb;
                filtered.rgb += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, saturate(pixelUV + float2(texel.x, 0))).rgb;
                filtered.rgb += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, saturate(pixelUV + float2(0, texel.y))).rgb;
                filtered.rgb /= 4.0;
                filtered.rgb = lerp(color.rgb, filtered.rgb, _SweepBlur);

                float lineNoise = frac(sin(dot(float2(floor(uv.y * _PixelResolution.y), floor(_SweepProgress * 97.0)), float2(12.9898, 78.233))) * 43758.5453);
                filtered.rgb = lerp(filtered.rgb, filtered.bgr, _SweepColorShift);
                filtered.rgb += (lineNoise - 0.5) * _SweepNoise;

                color.rgb = lerp(color.rgb, filtered.rgb, sweepMask);
                color.rgb *= 1.0 + sweepMask * _SweepBrightness;

                float2 borderUV = abs(uv - 0.5) * 2.0;
                float edge = smoothstep(0.62, 1.0, max(borderUV.x, borderUV.y));
                float corner = smoothstep(0.55, 1.18, length(borderUV));
                float border = saturate(edge * 0.65 + corner * 0.35) * _BorderIntensity;
                color.rgb *= 1.0 - border;

                return color;
            }
            ENDHLSL
        }
    }
}
