Shader "Custom/SpriteLightSweep"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _SweepSpeed ("Sweep Speed", Float) = 0.3
        _SweepWidth ("Sweep Width", Range(0.01, 0.3)) = 0.08
        _SweepIntensity ("Sweep Intensity", Range(0, 3)) = 1.5
        _SweepPause ("Sweep Pause (seconds)", Range(0, 3)) = 1.0

        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0

        [HideInInspector] _StencilRef ("Stencil Reference", Float) = 0
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_StencilRef]
            ReadMask [_StencilReadMask]
            Comp Always
            Pass Replace
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment frag
            #pragma multi_compile _ PIXELSNAP_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 texcoord   : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord   : TEXCOORD0;
                float4 color      : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _SweepSpeed;
                float _SweepWidth;
                float _SweepIntensity;
                float _SweepPause;
            CBUFFER_END

            float4 SpritePixelSnap(float4 positionCS)
            {
                float2 halfPixelCount = _ScreenParams.xy * 0.5;
                float2 pixelPosition = floor((positionCS.xy / positionCS.w) * halfPixelCount + 0.5);
                positionCS.xy = (pixelPosition / halfPixelCount) * positionCS.w;
                return positionCS;
            }

            Varyings SpriteVert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                #ifdef PIXELSNAP_ON
                o.positionCS = SpritePixelSnap(o.positionCS);
                #endif
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);

                // === Diagonal sweep from bottom-right to top-left ===
                // Bottom-right (1,0) -> Top-left (0,1)
                // Perpendicular axis: sweepPos = (1 + uv.y - uv.x) * 0.5
                //   At (1,0) bottom-right = 0
                //   At (0,1) top-left    = 1
                float sweepPos = (1.0 + i.texcoord.y - i.texcoord.x) * 0.5;

                // Animate with pause: sweep across, then pause before restarting
                float cycleTime = 1.0 / max(_SweepSpeed, 0.001);
                float moveTime = cycleTime - _SweepPause;
                float raw = _Time.y / cycleTime;          // repeating cycle
                float t = frac(raw);                       // 0..1 within cycle

                // Map t into the moving portion (0..1 of sweep), then hold at edges
                float sweepT = saturate(t * cycleTime / max(moveTime, 0.001));
                // sweepT: 0 at start, 1 at end, clamped during pause

                // Distance from pixel to sweep center
                float dist = abs(sweepPos - sweepT);

                // Sharp highlight band (no wrapping - clean single sweep)
                float highlight = 1.0 - smoothstep(0.0, _SweepWidth, dist);

                // Apply sweep glow (additive white light)
                half3 sweepColor = half3(1, 1, 1) * highlight * _SweepIntensity;
                half4 finalColor;
                finalColor.rgb = texColor.rgb * i.color.rgb + sweepColor * texColor.a;
                finalColor.a = texColor.a * i.color.a;

                return finalColor;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
