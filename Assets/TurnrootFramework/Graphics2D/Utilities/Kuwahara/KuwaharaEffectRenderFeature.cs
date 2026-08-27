using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Turnroot.Graphics2D.Utilities.Kuwahara
{
    [DisallowMultipleRendererFeature("Kuwahara Effect")]
    [System.Serializable]
    public class KuwaharaEffectRenderFeature : ScriptableRendererFeature
    {
        public RenderPassEvent RenderOrder = RenderPassEvent.BeforeRenderingPostProcessing;

        KuwaharaEffectPass _pass;

        public override void Create()
        {
            _pass = new KuwaharaEffectPass();
            _pass.renderPassEvent = RenderOrder;
        }

        public override void AddRenderPasses(
            ScriptableRenderer renderer,
            ref RenderingData renderingData
        )
        {
            if (renderingData.cameraData.cameraType == CameraType.SceneView)
                return;
            _pass.renderPassEvent = RenderOrder;
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _pass.DeInit();
        }
    }
}

