using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public sealed class EagleVisionColorMaskRendererFeature : ScriptableRendererFeature
{
    private sealed class EagleVisionColorMaskPass : ScriptableRenderPass
    {
        private sealed class PassData
        {
            public TextureHandle source;
            public Material material;
        }

        private readonly Material material;
        private RTHandle temporaryColor;

        public EagleVisionColorMaskPass(Material material)
        {
            this.material = material;
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
        }

        public override void OnCameraSetup(CommandBuffer commandBuffer, ref RenderingData renderingData)
        {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;
            RenderingUtils.ReAllocateHandleIfNeeded(
                ref temporaryColor,
                descriptor,
                FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_EagleVisionColorMaskTemporary");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (material == null || temporaryColor == null)
                return;

            RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;
            if (source == null)
                return;

            CommandBuffer commandBuffer = CommandBufferPool.Get("Eagle Vision Color Mask");
            Blitter.BlitCameraTexture(commandBuffer, source, temporaryColor, material, 0);
            Blitter.BlitCameraTexture(commandBuffer, temporaryColor, source);
            context.ExecuteCommandBuffer(commandBuffer);
            CommandBufferPool.Release(commandBuffer);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (material == null)
                return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            if (resourceData.isActiveTargetBackBuffer)
                return;

            TextureHandle source = resourceData.activeColorTexture;
            TextureDesc destinationDescriptor = renderGraph.GetTextureDesc(source);
            destinationDescriptor.name = "_EagleVisionColorMaskRenderGraphTexture";
            destinationDescriptor.clearBuffer = false;

            TextureHandle destination = renderGraph.CreateTexture(destinationDescriptor);
            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<PassData>(
                       "Eagle Vision Color Mask",
                       out PassData passData))
            {
                passData.source = source;
                passData.material = material;

                builder.UseTexture(source, AccessFlags.Read);
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(
                        context.cmd,
                        data.source,
                        new Vector4(1f, 1f, 0f, 0f),
                        data.material,
                        0);
                });
            }

            resourceData.cameraColor = destination;
        }

        public void Dispose()
        {
            temporaryColor?.Release();
            temporaryColor = null;
        }
    }

    [SerializeField] private Shader colorMaskShader;

    private Material material;
    private EagleVisionColorMaskPass pass;

    public override void Create()
    {
        if (colorMaskShader == null)
            colorMaskShader = Shader.Find("Hidden/Mariusz/Eagle Vision Color Mask");

        if (colorMaskShader == null)
        {
            Debug.LogWarning("EagleVisionColorMaskRendererFeature: color mask shader was not found.");
            return;
        }

        material = CoreUtils.CreateEngineMaterial(colorMaskShader);
        pass = new EagleVisionColorMaskPass(material);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        Camera camera = renderingData.cameraData.camera;
        if (pass == null || camera == null || camera != Camera.main || camera.cameraType != CameraType.Game)
            return;

        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing)
    {
        pass?.Dispose();
        pass = null;
        CoreUtils.Destroy(material);
        material = null;
    }
}
