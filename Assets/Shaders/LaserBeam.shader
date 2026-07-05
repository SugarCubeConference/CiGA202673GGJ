Shader "Custom/2D/LaserBeam"
{
    Properties
    {
        _CoreColor ("Core Color", Color) = (1, 1, 1, 1)
        _GlowColor ("Glow Color", Color) = (0, 0.8, 1, 1)
        _CoreWidth ("Core Width", Range(0, 1)) = 0.15
        _GlowWidth ("Glow Width", Range(0, 1)) = 0.5
        _GlowIntensity ("Glow Intensity", Range(0, 10)) = 2.0
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 2.0
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.3
        _ScrollSpeed ("Scroll Speed", Range(0, 10)) = 0.0
        _ScrollDensity ("Scroll Density", Range(1, 50)) = 10.0
        _FalloffPower ("Falloff Power", Range(0.1, 10)) = 2.0
        _NoiseScale ("Noise Scale", Range(0, 20)) = 5.0
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

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
                float fogFactor : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _CoreColor;
                half4 _GlowColor;
                half _CoreWidth;
                half _GlowWidth;
                half _GlowIntensity;
                half _PulseSpeed;
                half _PulseAmount;
                half _ScrollSpeed;
                half _ScrollDensity;
                half _FalloffPower;
                half _NoiseScale;
                half _NoiseStrength;
            CBUFFER_END

            // 简易 hash noise
            float hash(float2 p)
            {
                float h = dot(p, float2(127.1, 311.7));
                return frac(sin(h) * 43758.5453123);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.color = v.color;
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.uv;

                // u = 沿激光方向 (0~1), v = 垂直方向 (0~1, 0.5为中心)
                float along = uv.x;
                float across = abs(uv.y - 0.5) * 2.0; // 0=中心, 1=边缘

                // 噪声扰动（让边缘不那么规则）
                float n2 = noise(float2(along * _NoiseScale, 0.0));
                across += (n2 - 0.5) * _NoiseStrength;

                // 滚动扰动
                if (_ScrollSpeed > 0.001)
                {
                    float scrollNoise = noise(float2(along * _ScrollDensity, _Time.y * 2.0));
                    across += (scrollNoise - 0.5) * _NoiseStrength * 0.5;
                }

                // 脉冲
                float pulse = 1.0;
                if (_PulseSpeed > 0.001)
                {
                    pulse = 1.0 - _PulseAmount * (0.5 + 0.5 * sin(along * 6.2832 * 3.0 + _Time.y * _PulseSpeed));
                }

                // 核心层（白色/亮色，窄）
                float core = 1.0 - saturate(across / max(_CoreWidth, 0.001));
                core = pow(core, _FalloffPower);

                // 光晕层（彩色，宽）
                float glow = 1.0 - saturate(across / max(_GlowWidth, 0.001));
                glow = pow(glow, _FalloffPower * 0.5);

                // 混合颜色
                half3 coreCol = _CoreColor.rgb * _CoreColor.a * 2.0;
                half3 glowCol = _GlowColor.rgb * _GlowColor.a * _GlowIntensity;

                half3 finalColor = lerp(glowCol, coreCol, core) * (glow + core * 0.5) * pulse;

                // 顶点色透明度
                float alpha = i.color.a * saturate(glow + core);

                half4 col = half4(finalColor * alpha, 0.0); // additive blend, alpha 不需要
                col.rgb = MixFog(col.rgb, i.fogFactor);
                return col;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}
