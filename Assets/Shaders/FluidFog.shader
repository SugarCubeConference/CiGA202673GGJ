Shader "Custom/FluidFog"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _FogColor ("气雾颜色", Color) = (0.6, 0.8, 1.0, 1.0)
        _MousePosition ("鼠标位置 (UV)", Vector) = (0.5, 0.5, 0, 0)
        _Radius ("气雾半径", Range(0.05, 1.0)) = 0.35
        _Intensity ("气雾强度", Range(0, 2)) = 1.2
        _FlowSpeed ("流动速度", Range(0.1, 5)) = 1.5
        _NoiseScale ("噪点缩放", Range(1, 20)) = 6.0
        _EdgeSoftness ("边缘柔和度", Range(0.01, 1.0)) = 0.3
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "FluidFog"
            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_X(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _FogColor;
            float2 _MousePosition;
            float _Radius;
            float _Intensity;
            float _FlowSpeed;
            float _NoiseScale;
            float _EdgeSoftness;

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

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = i.uv;
                return o;
            }

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(hash(i), hash(i + float2(1, 0)), f.x),
                    lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), f.x),
                    f.y);
            }

            float fbm(float2 p)
            {
                float v = 0;
                float a = 0.5;
                float total = 0;
                for (int j = 0; j < 4; j++)
                {
                    v += a * noise(p);
                    total += a;
                    p *= 2.1;
                    a *= 0.55;
                }
                return v / total;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 scene = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                float2 toMouse = i.uv - _MousePosition;
                float dist = length(toMouse);
                float radial = smoothstep(_Radius, _Radius - _EdgeSoftness, dist);

                float t1 = _Time.y * _FlowSpeed * 0.5;
                float t2 = _Time.y * _FlowSpeed * 0.7;
                float t3 = _Time.y * _FlowSpeed * 1.1;

                float n1 = fbm(i.uv * _NoiseScale + float2(t1, t1));
                float n2 = fbm(i.uv * _NoiseScale * 1.3 + float2(-t2, t2 * 0.6));
                float n3 = fbm(i.uv * _NoiseScale * 0.7 + float2(t3 * 0.4, -t3 * 0.8));
                float fog = radial * (n1 * 0.5 + n2 * 0.3 + n3 * 0.2) * _Intensity;
                fog = saturate(fog);

                half4 col;
                col.rgb = lerp(scene.rgb, _FogColor.rgb, fog * _FogColor.a);
                col.a = scene.a;
                return col;
            }
            ENDHLSL
        }
    }
}