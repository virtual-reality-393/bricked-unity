using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class DepthRenderFeature : ScriptableRendererFeature
{
    private DepthTexturePass depthPass;
    public Material renderMaterial;

    class DepthTexturePass : ScriptableRenderPass
    {
        public DepthTexturePass(Material renderMaterial)
        {
            this.renderMaterial = renderMaterial;
        }
        

        public Material renderMaterial;
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resData = frameData.Get<UniversalResourceData>();
            RenderGraphUtils.BlitMaterialParameters parameters = new(resData.activeColorTexture, resData.activeColorTexture, renderMaterial, 0);
            renderGraph.AddBlitPass(parameters, "DepthTexturePass");
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(depthPass);
    }

    public override void Create()
    {
        depthPass = new DepthTexturePass(renderMaterial);
    }
}