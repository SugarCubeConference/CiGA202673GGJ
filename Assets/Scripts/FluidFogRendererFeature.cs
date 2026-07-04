using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 流体云雾全屏后处理 RendererFeature。
/// 将鼠标屏幕位置传递给 FluidFog shader，渲染跟随鼠标的流动气雾。
/// 
/// 使用方式：在 URP Renderer 中添加此 Feature。
/// </summary>
public class FluidFogRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class FluidFogSettings
    {
        /// <summary>仅对 Tag 匹配的相机生效</summary>
        public string cameraTag = "";

        [Header("气雾参数")]
        [ColorUsage(false, true)]
        public Color fogColor = new Color(0.6f, 0.8f, 1.0f, 1.0f);
        [Range(0.05f, 1.0f)]
        public float radius = 0.35f;
        [Range(0, 2)]
        public float intensity = 1.2f;
        [Range(0.1f, 5)]
        public float flowSpeed = 1.5f;
        [Range(1, 20)]
        public float noiseScale = 6.0f;
        [Range(0.01f, 1.0f)]
        public float edgeSoftness = 0.3f;

        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    }

    public FluidFogSettings settings = new FluidFogSettings();
    private FluidFogRenderPass _renderPass;

    public override void Create()
    {
        _renderPass = new FluidFogRenderPass(settings);
        _renderPass.renderPassEvent = settings.renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!string.IsNullOrEmpty(settings.cameraTag))
        {
            var cam = renderingData.cameraData.camera;
            if (!cam.CompareTag(settings.cameraTag)) return;
        }

        _renderPass.Setup(renderer);
        renderer.EnqueuePass(_renderPass);
    }

    protected override void Dispose(bool disposing)
    {
        _renderPass?.Dispose();
    }

    private class FluidFogRenderPass : ScriptableRenderPass
    {
        private readonly FluidFogSettings _settings;
        private Material _material;
        private RTHandle _tempRT;
        private ScriptableRenderer _renderer;

        private static readonly int FogColorId = Shader.PropertyToID("_FogColor");
        private static readonly int MousePositionId = Shader.PropertyToID("_MousePosition");
        private static readonly int RadiusId = Shader.PropertyToID("_Radius");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int FlowSpeedId = Shader.PropertyToID("_FlowSpeed");
        private static readonly int NoiseScaleId = Shader.PropertyToID("_NoiseScale");
        private static readonly int EdgeSoftnessId = Shader.PropertyToID("_EdgeSoftness");

        public FluidFogRenderPass(FluidFogSettings settings)
        {
            _settings = settings;
            Shader shader = Shader.Find("Custom/FluidFog");
            if (shader != null && shader.isSupported)
            {
                _material = CoreUtils.CreateEngineMaterial(shader);
            }
        }

        public void Setup(ScriptableRenderer renderer)
        {
            _renderer = renderer;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref _tempRT, descriptor, name: "_FluidFogTempRT");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_material == null) return;

            CommandBuffer cmd = CommandBufferPool.Get("FluidFog");

            // 获取鼠标屏幕坐标，转换为 UV（左下角原点）
            Vector3 mouseScreen = Input.mousePosition;
            float mouseU = mouseScreen.x / Screen.width;
            float mouseV = mouseScreen.y / Screen.height;

            _material.SetColor(FogColorId, _settings.fogColor);
            _material.SetVector(MousePositionId, new Vector4(mouseU, mouseV, 0, 0));
            _material.SetFloat(RadiusId, _settings.radius);
            _material.SetFloat(IntensityId, _settings.intensity);
            _material.SetFloat(FlowSpeedId, _settings.flowSpeed);
            _material.SetFloat(NoiseScaleId, _settings.noiseScale);
            _material.SetFloat(EdgeSoftnessId, _settings.edgeSoftness);

            RTHandle source = _renderer.cameraColorTargetHandle;

            Blitter.BlitCameraTexture(cmd, source, _tempRT, _material, 0);
            Blitter.BlitCameraTexture(cmd, _tempRT, source);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            _tempRT?.Release();
            _tempRT = null;
            CoreUtils.Destroy(_material);
            _material = null;
        }
    }
}