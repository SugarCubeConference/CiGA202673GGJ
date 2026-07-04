using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PixelFisheyeRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        /// <summary>仅对 Tag 匹配的相机生效（默认 MainCamera）。空字符串 = 全部相机。</summary>
        public string cameraTag = "MainCamera";
        public Vector2 pixelResolution = new Vector2(160, 90);
        [Range(0f, 2f)]
        public float fisheyeStrength = 0.5f;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    }

    public Settings settings = new Settings();
    PixelFisheyeRenderPass renderPass;

    public override void Create()
    {
        Debug.Log("[PixelFisheye] Create called");
        renderPass = new PixelFisheyeRenderPass(settings);
        renderPass.renderPassEvent = settings.renderPassEvent;
        Debug.Log("[PixelFisheye] Material created: " + (renderPass.Material != null));
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!string.IsNullOrEmpty(settings.cameraTag))
        {
            var cam = renderingData.cameraData.camera;
            if (!cam.CompareTag(settings.cameraTag)) return;
        }
        if (settings.pixelResolution.x <= 0 || settings.pixelResolution.y <= 0)
            return;
        renderPass.Setup(renderer);
        renderer.EnqueuePass(renderPass);
    }

    protected override void Dispose(bool disposing)
    {
        renderPass?.Dispose();
    }

    class PixelFisheyeRenderPass : ScriptableRenderPass
    {
        private Settings settings;
        private Material material;
        public Material Material => material;
        private RTHandle tempRT;
        private ScriptableRenderer inputRenderer;

        private static readonly int PixelResolutionID = Shader.PropertyToID("_PixelResolution");
        private static readonly int FisheyeStrengthID = Shader.PropertyToID("_FisheyeStrength");

        public PixelFisheyeRenderPass(Settings settings)
        {
            this.settings = settings;
            var shader = Shader.Find("Custom/PixelFisheye");
            if (shader != null && shader.isSupported)
            {
                material = CoreUtils.CreateEngineMaterial(shader);
            }
        }

        public void Setup(ScriptableRenderer renderer)
        {
            inputRenderer = renderer;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.width = (int)settings.pixelResolution.x;
            desc.height = (int)settings.pixelResolution.y;
            RenderingUtils.ReAllocateIfNeeded(ref tempRT, desc, name: "_PixelFisheyeTemp");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (material == null)
                return;

            CommandBuffer cmd = CommandBufferPool.Get("PixelFisheye");

            material.SetVector(PixelResolutionID, new Vector4(
                settings.pixelResolution.x,
                settings.pixelResolution.y, 0, 0));
            material.SetFloat(FisheyeStrengthID, settings.fisheyeStrength);

            var source = inputRenderer.cameraColorTargetHandle;
            Blitter.BlitCameraTexture(cmd, source, tempRT, material, 0);
            Blitter.BlitCameraTexture(cmd, tempRT, source);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            tempRT?.Release();
        }
    }
}
