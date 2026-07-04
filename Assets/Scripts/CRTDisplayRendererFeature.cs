using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CRTDisplayRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class CRTDisplaySettings
    {
        /// <summary>仅对 Tag 匹配的相机生效（默认 MainCamera）。空字符串 = 全部相机。</summary>
        public string cameraTag = "MainCamera";
        public float scanlineIntensity = 0.15f;
        public float scanlineCount = 480f;
        [Range(0f, 0.1f)]
        public float curvature = 0.03f;
        public float chromaticAberration = 1.5f;
        public float vignetteIntensity = 0.8f;
        public float vignetteSmoothness = 2.0f;
        public float brightness = 1.1f;
        public float contrast = 1.2f;
        public float rgbPixelSize = 3f;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    }

    public CRTDisplaySettings settings = new CRTDisplaySettings();
    private CRTDisplayRenderPass _renderPass;

    public override void Create()
    {
        _renderPass = new CRTDisplayRenderPass(settings);
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

    private class CRTDisplayRenderPass : ScriptableRenderPass
    {
        private readonly CRTDisplaySettings _settings;
        private Material _material;
        private RTHandle _tempRT;
        private readonly int _tempRTPropertyId = Shader.PropertyToID("_TempRT");

        private ScriptableRenderer _renderer;

        public CRTDisplayRenderPass(CRTDisplaySettings settings)
        {
            _settings = settings;
            Shader shader = Shader.Find("Custom/CRTDisplay");
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
            RenderingUtils.ReAllocateIfNeeded(ref _tempRT, descriptor, name: "_CRTDisplayTempRT");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_material == null)
                return;

            CommandBuffer cmd = CommandBufferPool.Get("CRTDisplay");

            _material.SetFloat("_ScanlineIntensity", _settings.scanlineIntensity);
            _material.SetFloat("_ScanlineCount", _settings.scanlineCount);
            _material.SetFloat("_Curvature", Mathf.Clamp(_settings.curvature, 0f, 0.1f));
            _material.SetFloat("_ChromaticAberration", _settings.chromaticAberration);
            _material.SetFloat("_VignetteIntensity", _settings.vignetteIntensity);
            _material.SetFloat("_VignetteSmoothness", _settings.vignetteSmoothness);
            _material.SetFloat("_Brightness", _settings.brightness);
            _material.SetFloat("_Contrast", _settings.contrast);
            _material.SetFloat("_RGBPixelSize", _settings.rgbPixelSize);

            RTHandle source = _renderer.cameraColorTargetHandle;

            Blitter.BlitCameraTexture(cmd, source, _tempRT, _material, 0);
            Blitter.BlitCameraTexture(cmd, _tempRT, source);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
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
