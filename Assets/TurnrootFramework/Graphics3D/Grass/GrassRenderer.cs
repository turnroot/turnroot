using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
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
    ///   4. Optionally assign grass mixin materials (scattered using the same mask).
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

        [Tooltip("Culling camera. Falls back to Camera.main, then any active camera.")]
        public UnityEngine.Camera targetCamera;

        // -- Mask ------------------------------------------------------------------
        [Header("Mask (texture must be Read/Write enabled in Import Settings)")]
        public Texture2D maskTexture;

        [Range(0f, 1f), Tooltip("Mask values at or below this floor are treated as zero height.")]
        public float maskFloor = 0f;

        // -- Density & Blade Size --------------------------------------------------
        [Header("Density")]
        [Range(1f, 400f)]
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

        // -- Grass Mixins ----------------------------------------------------------
        [Header("Grass Mixins - one scatter pass per material, follows grass mask")]
        [FormerlySerializedAs("extraMaterials")]
        public List<Material> grassMixinMaterials = new List<Material>();

        [Range(0f, 1f)]
        [FormerlySerializedAs("unmaskedExtraDensity")]
        public float grassMixinDensity = 0.01f; // quads per m�

        [FormerlySerializedAs("unmaskedExtraSize")]
        public Vector2 grassMixinSize = new Vector2(0.1f, 0.1f);

        [Min(0.1f)]
        [Tooltip("Hard cap for grass mixin width/height to prevent accidental giant quads.")]
        public float maxGrassMixinSize = 8f;

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
        private UnityEngine.Camera _resolvedCamera;

        // Indirect args layout: [indexCount, instanceCount, startIndex, baseVertex, startInstance]
        private readonly uint[] _args = new uint[5];

        // Implementation moved to partials (data, lifecycle, init, rendering, mesh builders, editor).
    }
}
