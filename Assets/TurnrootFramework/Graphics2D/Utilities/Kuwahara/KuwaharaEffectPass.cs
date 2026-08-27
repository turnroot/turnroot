using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Turnroot.Graphics2D.Utilities.Kuwahara
{
    [System.Serializable]
    public sealed class KuwaharaEffectPass : ScriptableRenderPass
    {
        // ---------------------------------------------------------------------------
        // Fields
        // ---------------------------------------------------------------------------

        Material _effectMaterial;
        KuwaharaEffectType _effectType;

        // Shader keyword strings must match the #pragma multi_compile_local in the shader
        static readonly string KW_ANIMATE_SIZE = "ANIMATE_SIZE";
        static readonly string KW_ANIMATE_ORIGIN = "ANIMATE_ORIGIN";

        public KuwaharaEffectPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        // ---------------------------------------------------------------------------
        // Unity 6 – RenderGraph path
        // ---------------------------------------------------------------------------

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var stack = VolumeManager.instance.stack;
            var effect = stack.GetComponent<KuwaharaEffectPPComponent>();
            if (effect == null || !effect.IsActive())
                return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();

            // Skip scene-view cameras to avoid double-processing
            if (cameraData.isSceneViewCamera)
                return;

            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;

            TextureHandle source = resourceData.activeColorTexture;
            int passCount = effect.Passes.value;

            switch (effect.EffectType.value)
            {
                case KuwaharaEffectType.Basic:
                    SetupBasic(effect);
                    RunPingPong(renderGraph, source, desc, passCount, 0);
                    break;

                case KuwaharaEffectType.Generalized:
                    SetupGeneralized(effect);
                    RunPingPong(renderGraph, source, desc, passCount, 0);
                    break;

                case KuwaharaEffectType.Anisotropic:
                    SetupAnisotropic(effect);
                    RunAnisotropic(renderGraph, source, desc, passCount);
                    break;
            }
        }

        // ---------------------------------------------------------------------------
        // Render helpers
        // ---------------------------------------------------------------------------

        /// Ping-pong between two temp RTs for <passCount> iterations, then copy back.
        void RunPingPong(
            RenderGraph rg,
            TextureHandle source,
            RenderTextureDescriptor desc,
            int passCount,
            int shaderPass
        )
        {
            TextureHandle a = UniversalRenderer.CreateRenderGraphTexture(
                rg,
                desc,
                "_KuwTemp_A",
                false
            );
            TextureHandle b = UniversalRenderer.CreateRenderGraphTexture(
                rg,
                desc,
                "_KuwTemp_B",
                false
            );

            // First pass always reads from source
            AddBlitPass(rg, source, a, shaderPass);

            for (int i = 1; i < passCount; i++)
            {
                bool even = (i % 2) == 0;
                AddBlitPass(rg, even ? b : a, even ? a : b, shaderPass);
            }

            TextureHandle last = (passCount % 2 == 1) ? a : b;
            AddCopyPass(rg, last, source);
        }

        /// Full anisotropic pipeline: structure tensor → blur H → blur V+eigen → filter passes.
        void RunAnisotropic(
            RenderGraph rg,
            TextureHandle source,
            RenderTextureDescriptor desc,
            int passCount
        )
        {
            TextureHandle stensor = UniversalRenderer.CreateRenderGraphTexture(
                rg,
                desc,
                "_KuwST",
                false
            );
            TextureHandle eigen1 = UniversalRenderer.CreateRenderGraphTexture(
                rg,
                desc,
                "_KuwEV1",
                false
            );
            TextureHandle eigen2 = UniversalRenderer.CreateRenderGraphTexture(
                rg,
                desc,
                "_KuwEV2",
                false
            );
            TextureHandle a = UniversalRenderer.CreateRenderGraphTexture(
                rg,
                desc,
                "_KuwTemp_A",
                false
            );
            TextureHandle b = UniversalRenderer.CreateRenderGraphTexture(
                rg,
                desc,
                "_KuwTemp_B",
                false
            );

            // Preprocessing
            AddBlitPass(rg, source, stensor, 0); // structure tensor
            AddBlitPass(rg, stensor, eigen1, 1); // horizontal gaussian blur
            AddBlitPass(rg, eigen1, eigen2, 2); // vertical gaussian blur + eigen decomp

            // Kuwahara filter passes; eigen2 is the TFM bound as a global texture
            AddBlitPassWithGlobal(rg, source, a, 3, "_TFM", eigen2);

            for (int i = 1; i < passCount; i++)
            {
                bool even = (i % 2) == 0;
                AddBlitPassWithGlobal(rg, even ? b : a, even ? a : b, 3, "_TFM", eigen2);
            }

            TextureHandle last = (passCount % 2 == 1) ? a : b;
            AddCopyPass(rg, last, source);
        }

        // ---------------------------------------------------------------------------
        // RenderGraph pass builders
        // ---------------------------------------------------------------------------

        class BlitData
        {
            public TextureHandle src;
            public TextureHandle tfm; // optional global texture
            public string tfmName;
            public bool hasTfm;
            public Material mat;
            public int pass;
        }

        void AddBlitPass(RenderGraph rg, TextureHandle src, TextureHandle dst, int shaderPass)
        {
            using var builder = rg.AddRasterRenderPass<BlitData>("Kuwahara", out var data);
            data.src = src;
            data.mat = _effectMaterial;
            data.pass = shaderPass;
            data.hasTfm = false;

            builder.UseTexture(src, AccessFlags.Read);
            builder.SetRenderAttachment(dst, 0, AccessFlags.Write);
            builder.SetRenderFunc(
                static (BlitData d, RasterGraphContext ctx) =>
                {
                    Blitter.BlitTexture(ctx.cmd, d.src, new Vector4(1, 1, 0, 0), d.mat, d.pass);
                }
            );
        }

        void AddBlitPassWithGlobal(
            RenderGraph rg,
            TextureHandle src,
            TextureHandle dst,
            int shaderPass,
            string globalName,
            TextureHandle globalTex
        )
        {
            using var builder = rg.AddRasterRenderPass<BlitData>("Kuwahara+TFM", out var data);
            data.src = src;
            data.tfm = globalTex;
            data.tfmName = globalName;
            data.hasTfm = true;
            data.mat = _effectMaterial;
            data.pass = shaderPass;

            builder.UseTexture(src, AccessFlags.Read);
            builder.UseTexture(globalTex, AccessFlags.Read);
            builder.SetRenderAttachment(dst, 0, AccessFlags.Write);
            builder.AllowGlobalStateModification(true);
            builder.SetRenderFunc(
                static (BlitData d, RasterGraphContext ctx) =>
                {
                    ctx.cmd.SetGlobalTexture(d.tfmName, d.tfm);
                    Blitter.BlitTexture(ctx.cmd, d.src, new Vector4(1, 1, 0, 0), d.mat, d.pass);
                }
            );
        }

        class CopyData
        {
            public TextureHandle src;
        }

        void AddCopyPass(RenderGraph rg, TextureHandle src, TextureHandle dst)
        {
            using var builder = rg.AddRasterRenderPass<CopyData>(
                "Kuwahara Copy-Back",
                out var data
            );
            data.src = src;

            builder.UseTexture(src, AccessFlags.Read);
            builder.SetRenderAttachment(dst, 0, AccessFlags.Write);
            builder.SetRenderFunc(
                static (CopyData d, RasterGraphContext ctx) =>
                {
                    Blitter.BlitTexture(ctx.cmd, d.src, new Vector4(1, 1, 0, 0), 0, false);
                }
            );
        }

        // ---------------------------------------------------------------------------
        // Material setup
        // ---------------------------------------------------------------------------

        void SetupBasic(KuwaharaEffectPPComponent c)
        {
            EnsureMaterial(KuwaharaEffectType.Basic, "Shaders/Kuwahara");

            // Use shader keywords instead of integer uniforms — avoids branching per pixel
            SetKeyword(_effectMaterial, KW_ANIMATE_SIZE, c.AnimateKernelSize.value);
            SetKeyword(_effectMaterial, KW_ANIMATE_ORIGIN, c.AnimateKernelOrigin.value);

            _effectMaterial.SetInt("_KernelSize", c.KernelSize.value);
            _effectMaterial.SetInt("_MinKernelSize", c.MinKernelSize.value);
            _effectMaterial.SetFloat("_SizeAnimationSpeed", c.SizeAnimationSpeed.value);
            _effectMaterial.SetFloat("_NoiseFrequency", c.NoiseFrequency.value);
        }

        void SetupGeneralized(KuwaharaEffectPPComponent c)
        {
            EnsureMaterial(KuwaharaEffectType.Generalized, "Shaders/GeneralizedKuwahara");

            _effectMaterial.SetInt("_KernelSize", c.KernelSize.value);
            _effectMaterial.SetInt("_N", 8);
            _effectMaterial.SetFloat("_Q", c.Sharpness.value);
            _effectMaterial.SetFloat("_Hardness", c.Hardness.value);
            _effectMaterial.SetFloat("_ZeroCrossing", c.ZeroCrossing.value);
            _effectMaterial.SetFloat(
                "_Zeta",
                c.UseZeta.value ? c.Zeta.value : 2f / (c.KernelSize.value / 2f)
            );
        }

        void SetupAnisotropic(KuwaharaEffectPPComponent c)
        {
            EnsureMaterial(KuwaharaEffectType.Anisotropic, "Shaders/AnisotropicKuwahara");

            _effectMaterial.SetInt("_KernelSize", c.KernelSize.value);
            _effectMaterial.SetInt("_N", 8);
            _effectMaterial.SetFloat("_Q", c.Sharpness.value);
            _effectMaterial.SetFloat("_Hardness", c.Hardness.value);
            _effectMaterial.SetFloat("_ZeroCrossing", c.ZeroCrossing.value);
            _effectMaterial.SetFloat("_Alpha", c.Alpha.value);
            _effectMaterial.SetFloat(
                "_Zeta",
                c.UseZeta.value ? c.Zeta.value : 2f / (c.KernelSize.value / 2f)
            );
        }

        // ---------------------------------------------------------------------------
        // Utilities
        // ---------------------------------------------------------------------------

        void EnsureMaterial(KuwaharaEffectType type, string shaderPath)
        {
            if (_effectType == type && _effectMaterial)
                return;
            if (_effectMaterial)
                Object.Destroy(_effectMaterial);
            _effectMaterial = new Material(Resources.Load<Shader>(shaderPath));
            _effectType = type;
        }

        static void SetKeyword(Material mat, string keyword, bool enabled)
        {
            if (enabled)
                mat.EnableKeyword(keyword);
            else
                mat.DisableKeyword(keyword);
        }

        public void DeInit()
        {
            if (_effectMaterial)
            {
                Object.Destroy(_effectMaterial);
                _effectMaterial = null;
            }
        }
    }
}

