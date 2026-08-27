using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Turnroot.Graphics2D.Utilities.Kuwahara
{
    [Serializable]
    [VolumeComponentMenu("Custom/Kuwahara")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public sealed class KuwaharaEffectPPComponent : VolumeComponent, IPostProcessComponent
    {
        public bool IsActive() => active && Enabled.value;

        public bool IsTileCompatible() => true;

        [Tooltip("Enable or disable the Kuwahara effect")]
        public BoolParameter Enabled = new BoolParameter(true, overrideState: true);

        [Tooltip("Type of Kuwahara effect implementation")]
        public VolumeParameter<KuwaharaEffectType> EffectType =
            new VolumeParameter<KuwaharaEffectType> { value = KuwaharaEffectType.Basic };

        public ClampedIntParameter Passes = new ClampedIntParameter(1, 1, 4);

        [Header("Settings for Basic type:")]
        public ClampedFloatParameter NoiseFrequency = new ClampedFloatParameter(10f, 0f, 30f);
        public ClampedIntParameter KernelSize = new ClampedIntParameter(5, 1, 20);
        public BoolParameter AnimateKernelSize = new BoolParameter(false);
        public ClampedIntParameter MinKernelSize = new ClampedIntParameter(1, 1, 20);
        public ClampedFloatParameter SizeAnimationSpeed = new ClampedFloatParameter(1f, 0.1f, 5f);
        public BoolParameter AnimateKernelOrigin = new BoolParameter(false);

        [Header("Settings for Generalized and Anisotropic:")]
        public ClampedFloatParameter Sharpness = new ClampedFloatParameter(8f, 0.1f, 18f);
        public ClampedFloatParameter Hardness = new ClampedFloatParameter(8f, 1f, 100f);
        public ClampedFloatParameter ZeroCrossing = new ClampedFloatParameter(0.58f, 0.01f, 2f);
        public BoolParameter UseZeta = new BoolParameter(false);
        public ClampedFloatParameter Zeta = new ClampedFloatParameter(1f, 0.01f, 3f);

        [Header("Settings for Anisotropic:")]
        public ClampedFloatParameter Alpha = new ClampedFloatParameter(1f, 0.01f, 2f);
    }
}

