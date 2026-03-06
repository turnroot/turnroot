using UnityEngine;
using UnityEngine.UI;

namespace TurnrootFramework.Graphics2D
{
    /// <summary>
    /// UI component that applies a twinkle effect to an Image using a mask texture.
    /// Supports various blend modes and animated mask transformations.
    /// </summary>
    [RequireComponent(typeof(Image))]
    [ExecuteAlways]
    public class UITwinkleMask : MonoBehaviour
    {
        public enum BlendMode
        {
            Add = 0,
            Lighten = 1,
            Subtract = 2,
            Darken = 3,
            Both = 4,
        }

        [Header("Mask Texture")]
        [SerializeField]
        private Texture2D maskTexture;

        [Header("Effect Settings")]
        [SerializeField, Range(0f, 2f)]
        private float intensity = 1f;

        [SerializeField]
        private BlendMode blendMode = BlendMode.Both;

        [Header("Lighten/Add Settings")]
        [SerializeField, Range(0f, 1f)]
        private float lightenThreshold = 0.5f;

        [SerializeField, Range(0f, 2f)]
        private float lightenIntensity = 1f;

        [Header("Darken/Subtract Settings")]
        [SerializeField, Range(0f, 1f)]
        private float darkenThreshold = 0.5f;

        [SerializeField, Range(0f, 2f)]
        private float darkenIntensity = 1f;

        [Header("Mask Transform")]
        [SerializeField]
        private Vector2 maskScale = Vector2.one;

        [SerializeField]
        private float maskRotation = 0f;

        [Header("Animation")]
        [SerializeField]
        private bool animateOffset = false;

        [SerializeField]
        private Vector2 offsetSpeed = new Vector2(0.1f, 0.1f);

        [SerializeField]
        private bool animateRotation = false;

        [SerializeField]
        private float rotationSpeed = 10f;

        [SerializeField]
        private bool animateScale = false;

        [SerializeField]
        private Vector2 scaleRange = new Vector2(0.8f, 1.2f);

        [SerializeField]
        private float scaleSpeed = 1f;

        [SerializeField]
        private bool pulsateIntensity = false;

        [SerializeField]
        private Vector2 intensityRange = new Vector2(0.5f, 1.5f);

        [SerializeField]
        private float intensityPulseSpeed = 1f;

        private Image image;
        private Material material;
        private Vector2 currentOffset;
        private float baseIntensity;
        private float baseRotation;
        private Vector2 baseScale;

        private static readonly int MaskTexID = Shader.PropertyToID("_MaskTex");
        private static readonly int MaskOffsetID = Shader.PropertyToID("_MaskOffset");
        private static readonly int MaskScaleID = Shader.PropertyToID("_MaskScale");
        private static readonly int MaskRotationID = Shader.PropertyToID("_MaskRotation");
        private static readonly int IntensityID = Shader.PropertyToID("_Intensity");
        private static readonly int LightenThresholdID = Shader.PropertyToID("_LightenThreshold");
        private static readonly int LightenIntensityID = Shader.PropertyToID("_LightenIntensity");
        private static readonly int DarkenThresholdID = Shader.PropertyToID("_DarkenThreshold");
        private static readonly int DarkenIntensityID = Shader.PropertyToID("_DarkenIntensity");
        private static readonly int BlendModeID = Shader.PropertyToID("_BlendMode");

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
            UpdateMaterial();
        }

        private void OnDisable()
        {
            if (material != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(material);
                }
                else
                {
                    DestroyImmediate(material);
                }
                material = null;
            }
        }

        private void Initialize()
        {
            if (image == null)
            {
                image = GetComponent<Image>();
            }

            if (material == null && image != null)
            {
                Shader shader = Shader.Find("UI/TwinkleMask");
                if (shader != null)
                {
                    material = new Material(shader);
                    image.material = material;
                }
                else
                {
                    Debug.LogError("UITwinkleMask: Could not find shader 'UI/TwinkleMask'");
                }
            }

            baseIntensity = intensity;
            baseRotation = maskRotation;
            baseScale = maskScale;
            currentOffset = Vector2.zero;
        }

        private void Update()
        {
            if (material == null)
            {
                Initialize();
            }

            UpdateAnimation();
            UpdateMaterial();
        }

        private void UpdateAnimation()
        {
            float time = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;

            // Animate offset
            if (animateOffset)
            {
                currentOffset += offsetSpeed * Time.deltaTime;
                currentOffset.x = Mathf.Repeat(currentOffset.x, 1f);
                currentOffset.y = Mathf.Repeat(currentOffset.y, 1f);
            }
            else
            {
                currentOffset = Vector2.zero;
            }

            // Animate rotation
            if (animateRotation)
            {
                maskRotation = baseRotation + (rotationSpeed * time);
                maskRotation = Mathf.Repeat(maskRotation, 360f);
            }
            else
            {
                maskRotation = baseRotation;
            }

            // Animate scale
            if (animateScale)
            {
                float scalePulse = Mathf.PingPong(time * scaleSpeed, 1f);
                float scaleMultiplier = Mathf.Lerp(scaleRange.x, scaleRange.y, scalePulse);
                maskScale = baseScale * scaleMultiplier;
            }
            else
            {
                maskScale = baseScale;
            }

            // Pulsate intensity
            if (pulsateIntensity)
            {
                float intensityPulse = Mathf.PingPong(time * intensityPulseSpeed, 1f);
                intensity = Mathf.Lerp(intensityRange.x, intensityRange.y, intensityPulse);
            }
            else
            {
                intensity = baseIntensity;
            }
        }

        private void UpdateMaterial()
        {
            if (material == null)
                return;

            // Update mask texture
            if (maskTexture != null)
            {
                material.SetTexture(MaskTexID, maskTexture);
            }

            // Update transform
            material.SetVector(MaskOffsetID, currentOffset);
            material.SetVector(MaskScaleID, maskScale);
            material.SetFloat(MaskRotationID, maskRotation);

            // Update effect parameters
            material.SetFloat(IntensityID, intensity);
            material.SetFloat(LightenThresholdID, lightenThreshold);
            material.SetFloat(LightenIntensityID, lightenIntensity);
            material.SetFloat(DarkenThresholdID, darkenThreshold);
            material.SetFloat(DarkenIntensityID, darkenIntensity);
            material.SetFloat(BlendModeID, (float)blendMode);

            // Update shader keywords for blend mode
            material.DisableKeyword("_BLENDMODE_ADD");
            material.DisableKeyword("_BLENDMODE_LIGHTEN");
            material.DisableKeyword("_BLENDMODE_SUBTRACT");
            material.DisableKeyword("_BLENDMODE_DARKEN");
            material.DisableKeyword("_BLENDMODE_BOTH");

            switch (blendMode)
            {
                case BlendMode.Add:
                    material.EnableKeyword("_BLENDMODE_ADD");
                    break;
                case BlendMode.Lighten:
                    material.EnableKeyword("_BLENDMODE_LIGHTEN");
                    break;
                case BlendMode.Subtract:
                    material.EnableKeyword("_BLENDMODE_SUBTRACT");
                    break;
                case BlendMode.Darken:
                    material.EnableKeyword("_BLENDMODE_DARKEN");
                    break;
                case BlendMode.Both:
                    material.EnableKeyword("_BLENDMODE_BOTH");
                    break;
            }
        }

        #region Public API

        /// <summary>
        /// Set the mask texture to use for the twinkle effect
        /// </summary>
        public void SetMaskTexture(Texture2D texture)
        {
            maskTexture = texture;
            UpdateMaterial();
        }

        /// <summary>
        /// Set the overall effect intensity
        /// </summary>
        public void SetIntensity(float value)
        {
            intensity = Mathf.Clamp(value, 0f, 2f);
            baseIntensity = intensity;
            UpdateMaterial();
        }

        /// <summary>
        /// Set the blend mode
        /// </summary>
        public void SetBlendMode(BlendMode mode)
        {
            blendMode = mode;
            UpdateMaterial();
        }

        /// <summary>
        /// Set thresholds for lighten/darken effects
        /// </summary>
        public void SetThresholds(float lighten, float darken)
        {
            lightenThreshold = Mathf.Clamp01(lighten);
            darkenThreshold = Mathf.Clamp01(darken);
            UpdateMaterial();
        }

        /// <summary>
        /// Set individual intensities for lighten and darken effects
        /// </summary>
        public void SetEffectIntensities(float lighten, float darken)
        {
            lightenIntensity = Mathf.Clamp(lighten, 0f, 2f);
            darkenIntensity = Mathf.Clamp(darken, 0f, 2f);
            UpdateMaterial();
        }

        /// <summary>
        /// Enable or disable automatic offset animation
        /// </summary>
        public void SetAnimateOffset(bool enabled, Vector2 speed)
        {
            animateOffset = enabled;
            offsetSpeed = speed;
        }

        /// <summary>
        /// Enable or disable automatic rotation animation
        /// </summary>
        public void SetAnimateRotation(bool enabled, float speed)
        {
            animateRotation = enabled;
            rotationSpeed = speed;
        }

        /// <summary>
        /// Enable or disable automatic scale animation
        /// </summary>
        public void SetAnimateScale(bool enabled, Vector2 range, float speed)
        {
            animateScale = enabled;
            scaleRange = range;
            scaleSpeed = speed;
        }

        /// <summary>
        /// Enable or disable intensity pulsation
        /// </summary>
        public void SetPulsateIntensity(bool enabled, Vector2 range, float speed)
        {
            pulsateIntensity = enabled;
            intensityRange = range;
            intensityPulseSpeed = speed;
        }

        #endregion

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null)
                    {
                        Initialize();
                        UpdateMaterial();
                    }
                };
            }
        }
#endif
    }
}
