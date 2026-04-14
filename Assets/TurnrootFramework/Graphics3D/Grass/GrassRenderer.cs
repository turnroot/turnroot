using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
#endif

namespace Turnroot.Graphics3D
{
    /// <summary>
    /// GPU-instanced grass on any mesh using compute-shader frustum/distance culling
    /// and Graphics.DrawMeshInstancedIndirect.
    ///
    /// Setup:
    ///   1. Attach to a GameObject with a MeshFilter.
    ///   2. Assign GrassCompute and a grass Material.
    ///   3. Optionally assign a Read/Write Texture2D mask (white = full grass).
    ///   4. Optionally assign unmasked extra materials (scattered uniformly)
    ///      or a masked extra material with its own mask texture.
    ///   5. Hit Play, or use "Regenerate Grass" in the context menu.
    ///
    /// Toggle via grassEnabled, SetGrassEnabled(bool), or an Animation Track.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [ExecuteAlways]
    public partial class GrassRenderer : MonoBehaviour
    {
        // -- References ------------------------------------------------------------
        [Header("References")]
        public ComputeShader computeShader;
        public Material grassMaterial;

        [Tooltip("The ground mesh this grass sits on. Used by AlignGroundTexture to map _GroundTex over the full ground footprint.")]
        public MeshFilter groundSource;

        [Tooltip("Culling camera. Falls back to Camera.main, then any active camera.")]
        public UnityEngine.Camera targetCamera;

        // -- Mask ------------------------------------------------------------------
        [Header("Mask (texture must be Read/Write enabled in Import Settings)")]
        public Texture2D maskTexture;

        [Range(0f, 1f), Tooltip("Mask values at or below this floor are treated as zero height.")]
        public float maskFloor = 0f;

        // -- Density & Blade Size --------------------------------------------------
        [Header("Density")]
        [Range(1f, 350f)]
        public float density = 60f; // blades per world-space m�
        public int maxBlades = 1_000_000;

        [Header("Blade Size")]
        public float minHeight = 0.15f;
        public float maxHeight = 0.45f;
        public float minWidth = 0.02f;
        public float maxWidth = 0.05f;

        // -- Culling ---------------------------------------------------------------
        [Header("Culling")]
        public float maxDistance = 50f;
        public float fadeStartDistance = 35f;

        // -- Runtime ---------------------------------------------------------------
        [Header("Runtime")]
        public bool grassEnabled = true;
        public ShadowCastingMode shadowCasting = ShadowCastingMode.Off;

        // -- Unmasked Extras -------------------------------------------------------
        [Header("Unmasked Extras � one scatter pass per material, ignores mask")]
        public List<Material> extraMaterials = new List<Material>();

        [Range(0f, 1f)]
        public float unmaskedExtraDensity = 0.01f; // quads per m�
        public Vector2 unmaskedExtraSize = new Vector2(0.1f, 0.1f);

        // -- Masked Extras ---------------------------------------------------------
        [Header("Masked Extras � quads spawned where maskedExtraMask is white")]
        public Material maskedExtraMaterial;
        public Texture2D maskedExtraMask;

        [Range(0f, 1f)]
        public float maskedExtraThreshold = 0.5f;

        [Range(0f, 1f)]
        public float maskedExtraDensity = 0.01f; // quads per m�
        public Vector2 maskedExtraSize = new Vector2(0.1f, 0.1f);

        // -- Debug -----------------------------------------------------------------
        [Header("Debug")]
        [Tooltip("Bypass compute culling and render all blades every frame. Slow at high counts.")]
        public bool disableCulling = false;

        [Tooltip("Log visible blade count to console once per second.")]
        public bool logVisibleCount = false;

        // -- GPU resources ---------------------------------------------------------
        private ComputeBuffer _allBladesBuffer;
        private ComputeBuffer _visibleBladesBuffer;
        private ComputeBuffer _indirectArgsBuffer;
        private ComputeBuffer _readbackBuffer;

        private MeshRenderer _meshRenderer;
        private bool _meshRendererWasEnabled;
        private Mesh _bladeMesh;
        private Mesh _planeMesh;
        private int _totalBladeCount;
        private int _cullKernel;
        private Bounds _drawBounds;
        private float _logTimer;

        // Indirect args layout: [indexCount, instanceCount, startIndex, baseVertex, startInstance]
        private readonly uint[] _args = new uint[5];

        // Implementation moved to partials (data, lifecycle, init, rendering, mesh builders, editor).
    }
}
