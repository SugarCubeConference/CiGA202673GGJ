Shader "Custom/CRTDisplay"
{
    Properties
    {
        _BlitTexture ("Base Texture", 2D) = "white" {}
        _ScanlineInterval ("Scanline Interval (px)", Range(1, 100)) = 6
        _ScanlineWidth ("Scanline Width", Range(0.01, 1.0)) = 0.15
        _ScanlineSharpness ("Scanline Sharpness", Range(0.5, 8.0)) = 2.0
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.6
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

            float _ScanlineInterval;
            float _ScanlineWidth;
            float _ScanlineSharpness;
            float _ScanlineIntensity;
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

                uv2 = saturate(uv2);

                // b. Chromatic aberration
                float2 caOffsetR = -offset * _ChromaticAberration * texelSize;
                float2 caOffsetB =  offset * _ChromaticAberration * texelSize;
                float r = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv2 + caOffsetR).r;
                float g = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv2).g;
                float b = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv2 + caOffsetB).b;
                half4 color = half4(r, g, b, 1.0);

                // c. RGB pixel grid (先处理像素化，再叠加扫描线)
                float2 pixelatedUV = floor(uv2 * _ScreenParams.xy / _RGBPixelSize) * _RGBPixelSize / _ScreenParams.xy;
                float2 subPixelOffset = 1.0 / _ScreenParams.xy;
                float pr = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, pixelatedUV + float2(-subPixelOffset.x, 0)).r;
                float pg = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, pixelatedUV).g;
                float pb = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, pixelatedUV + float2(subPixelOffset.x, 0)).b;
                color = half4(pr, pg, pb, 1.0);

                // d. 示波器风格精细扫描线
                // UV.y 作为自变量，乘以屏幕高度
                float yCoord = uv2.y * _ScreenParams.y;
                // 正弦函数生成周期波形，间距由像素数控制
                float sinWave = sin(yCoord / _ScanlineInterval * 6.28318);
                // 精细化线条：abs + smoothstep 控制暗线宽度，值越小线条越精细
                float scanlineMask = smoothstep(0.0, _ScanlineWidth, abs(sinWave));
                // 锐度调节，值越大线条边缘越锐利
                scanlineMask = pow(scanlineMask, _ScanlineSharpness);
                // 和原像素颜色混合，实现示波器效果
                color.rgb = lerp(color.rgb, color.rgb * scanlineMask, _ScanlineIntensity);

                // e. Vignette
                float edge = saturate(pow(length(uv2 - 0.5) * 1.35, _VignetteSmoothness));
                float vignette = lerp(1.0, 0.72, saturate(edge * _VignetteIntensity));

                // f. Final
                color.rgb = clamp((color.rgb * vignette - 0.5) * _Contrast + 0.5, 0, 1) * _Brightness;

                return color;
            }
            ENDHLSL
        }
    }
}
