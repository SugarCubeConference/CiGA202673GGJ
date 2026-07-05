Shader "Custom/BackgroundFilter"
{
    Properties
    {
        [Header(Base)]
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Gaussian Blur)]
        _BlurRadius ("Blur Radius", Range(0, 20)) = 0.0
        _BlurIntensity ("Blur Intensity", Range(0, 1)) = 0.0

        [Header(Color Adjustments)]
        _Brightness ("Brightness", Range(-1, 1)) = 0.0
        _Contrast ("Contrast", Range(0, 3)) = 1.0
        _Saturation ("Saturation", Range(0, 3)) = 1.0
        _TintColor ("Tint Color", Color) = (1,1,1,1)
        _TintStrength ("Tint Strength", Range(0, 1)) = 0.0
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
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            // Properties
            float4 _Color;
            float _BlurRadius;
            float _BlurIntensity;
            float _Brightness;
            float _Contrast;
            float _Saturation;
            float4 _TintColor;
            float _TintStrength;

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            // Gaussian weight approximation (9-tap)
            half4 GaussianBlur(float2 uv)
            {
                if (_BlurRadius <= 0.001)
                    return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                float2 texelSize = _MainTex_TexelSize.xy * _BlurRadius;

                // 9-tap Gaussian kernel (3x3)
                half4 col = half4(0, 0, 0, 0);
                float weights[9] = {
                    1.0 / 16.0, 2.0 / 16.0, 1.0 / 16.0,
                    2.0 / 16.0, 4.0 / 16.0, 2.0 / 16.0,
                    1.0 / 16.0, 2.0 / 16.0, 1.0 / 16.0
                };

                float2 offsets[9] = {
                    float2(-1, -1), float2(0, -1), float2(1, -1),
                    float2(-1,  0), float2(0,  0), float2(1,  0),
                    float2(-1,  1), float2(0,  1), float2(1,  1)
                };

                for (int i = 0; i < 9; i++)
                {
                    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + offsets[i] * texelSize) * weights[i];
                }

                return col;
            }

            // Color adjustment functions
            half3 AdjustBrightness(half3 col, float brightness)
            {
                return col + brightness;
            }

            half3 AdjustContrast(half3 col, float contrast)
            {
                return (col - 0.5) * contrast + 0.5;
            }

            half3 AdjustSaturation(half3 col, float saturation)
            {
                float luminance = dot(col, half3(0.2126, 0.7152, 0.0722));
                return lerp(half3(luminance, luminance, luminance), col, saturation);
            }

            half4 frag(Varyings i) : SV_Target
            {
                // 1. Gaussian Blur
                half4 col = GaussianBlur(i.uv);

                // 2. Apply vertex color
                col *= i.color;

                // 3. Brightness
                col.rgb = AdjustBrightness(col.rgb, _Brightness);

                // 4. Contrast
                col.rgb = AdjustContrast(col.rgb, _Contrast);

                // 5. Saturation
                col.rgb = AdjustSaturation(col.rgb, _Saturation);

                // 6. Tint
                col.rgb = lerp(col.rgb, col.rgb * _TintColor.rgb, _TintStrength);

                return col;
            }
            ENDHLSL
        }
    }
    FallBack "Sprites/Default"
}
