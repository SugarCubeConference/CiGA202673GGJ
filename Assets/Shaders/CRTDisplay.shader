Shader "Custom/CRTDisplay"
{
    Properties
    {
        _BlitTexture ("Base Texture", 2D) = "white" {}
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.15
        _ScanlineCount ("Scanline Count", Float) = 480
        _Curvature ("Curvature", Range(0, 0.1)) = 0.03
        _ChromaticAberration ("Chromatic Aberration", Range(0, 5)) = 1.5
        _VignetteIntensity ("Vignette Intensity", Range(0, 2)) = 0.8
        _VignetteSmoothness ("Vignette Smoothness", Range(0.1, 5)) = 2.0
        _Brightness ("Brightness", Range(0.5, 2)) = 1.1
        _Contrast ("Contrast", Range(0.5, 2)) = 1.2
        _RGBPixelSize ("RGB Pixel Size", Range(1, 10)) = 3
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
            Name "CRTDisplay"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            float _ScanlineIntensity;
            float _ScanlineCount;
            float _Curvature;
            float _ChromaticAberration;
            float _VignetteIntensity;
            float _VignetteSmoothness;
            float _Brightness;
            float _Contrast;
            float _RGBPixelSize;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float2 uv = float2((input.vertexID << 1) & 2, input.vertexID & 2);
                output.uv = uv;
                output.positionCS = float4(uv * 2.0 - 1.0, 0, 1);
                output.positionCS.y *= -1.0;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float2 texelSize = 1.0 / _ScreenParams.xy;

                // a. Barrel distortion
                float2 offset = uv - 0.5;
                float dist = dot(offset, offset);
                float2 uv2 = uv + offset * dist * _Curvature;

                // g. Discard if uv2 outside [0,1]
                if (uv2.x < 0.0 || uv2.x > 1.0 || uv2.y < 0.0 || uv2.y > 1.0)
                    discard;

                // b. Chromatic aberration
                float2 caOffsetR = -offset * _ChromaticAberration * texelSize;
                float2 caOffsetB =  offset * _ChromaticAberration * texelSize;
                float r = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv2 + caOffsetR).r;
                float g = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv2).g;
                float b = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv2 + caOffsetB).b;
                half4 color = half4(r, g, b, 1.0);

                // c. Scanlines
                float scanline = 1.0 - _ScanlineIntensity * (0.5 + 0.5 * sin(uv2.y * _ScanlineCount * 6.28318));

                // d. RGB pixel grid
                float2 pixelatedUV = floor(uv2 * _ScreenParams.xy / _RGBPixelSize) * _RGBPixelSize / _ScreenParams.xy;
                float2 subPixelOffset = 1.0 / _ScreenParams.xy;
                float pr = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, pixelatedUV + float2(-subPixelOffset.x, 0)).r;
                float pg = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, pixelatedUV).g;
                float pb = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, pixelatedUV + float2(subPixelOffset.x, 0)).b;
                color = half4(pr, pg, pb, 1.0);

                // e. Vignette
                float vignette = 1.0 - _VignetteIntensity * pow(length(uv2 - 0.5) * 2.0, _VignetteSmoothness);

                // f. Final
                color.rgb = clamp((color.rgb * scanline * vignette - 0.5) * _Contrast + 0.5, 0, 1) * _Brightness;

                return color;
            }
            ENDHLSL
        }
    }
}
