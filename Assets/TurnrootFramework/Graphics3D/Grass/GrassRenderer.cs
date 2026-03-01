using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Renders GPU-instanced grass on any mesh using compute-shader frustum/distance culling
/// and Graphics.DrawMeshInstancedIndirect. Blade placement is computed once on the CPU
/// (weighted by triangle area, masked by an optional texture) and uploaded to the GPU.
/// Only the per-frame culling pass runs on the GPU each frame.
///
/// Setup:
///   1. Attach to a GameObject that has a MeshFilter with the surface mesh.
///   2. Assign the GrassCompute compute shader and Grass material.
///   3. (Optional) Assign a Read/Write enabled Texture2D as the mask.
///      White = full grass, Black = no grass, threshold is configurable.
///   4. (Optional) supply one or more materials for unmasked extras, and/or a
///      separate material plus mask texture for masked extras.  Unmasked materials
///      scatter uniformly at the given density; masked extras only appear where the
///      mask is white (threshold controlled via the component).  All extras use the
///      same wind animation as the blades.
///   5. Hit Play, or use the "Regenerate Grass" context menu in edit mode.
///
/// Timeline toggle: animate the grassEnabled bool via an Animation Track,
/// or call SetGrassEnabled(bool) from a SignalEmitter receiver.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[ExecuteAlways]
public class GrassRenderer : MonoBehaviour
{
    // ── References ──────────────────────────────────────────────────────────
    [Header("References")]
    public ComputeShader computeShader;
    public Material grassMaterial;

    [Tooltip(
        "Camera used for culling. Assign directly if your camera is not tagged 'MainCamera'. "
            + "Leave empty to fall back to Camera.main, then any active camera."
    )]
    public Camera targetCamera;

    // ── Mask ────────────────────────────────────────────────────────────────
    [Header("Mask  (texture must be Read/Write enabled in import settings)")]
    public Texture2D maskTexture;

    [Range(0f, 1f)]
    public float maskZero = 0f; // mask value at which blade height becomes zero

    // maskThreshold is no longer used; black areas will shrink blades to zero height,
    // white areas keep full height.  (Left for compatibility; can be ignored.)
    [HideInInspector]
    public float maskThreshold = 0.5f;

    // ── Density & size ───────────────────────────────────────────────────────
    [Header("Density")]
    [Range(1f, 300f)]
    public float density = 60f; // blades per world-space m²
    public int maxBlades = 1_000_000;

    [Header("Blade Size")]
    public float minHeight = 0.15f;
    public float maxHeight = 0.45f;
    public float minWidth = 0.02f;
    public float maxWidth = 0.05f;

    // ── Culling ──────────────────────────────────────────────────────────────
    [Header("Culling")]
    public float maxDistance = 50f;
    public float fadeStartDistance = 35f;

    // ── Runtime ──────────────────────────────────────────────────────────────
    [Header("Runtime")]
    public bool grassEnabled = true;
    public ShadowCastingMode shadowCasting = ShadowCastingMode.Off;

    [Header("Extras – GPU‑instanced (quads)")]
    [Tooltip("Materials for extras that ignore the mask; each material gets its own scatter.")]
    public List<Material> extraMaterials = new List<Material>();

    [Tooltip("Material applied to spawned planes that use the mask.")]
    public Material maskedExtraMaterial;

    [Tooltip("Mask texture for masked extras; white areas spawn quads at the given density.")]
    public Texture2D maskedExtraMask;

    [Range(0f, 1f)]
    public float maskedExtraThreshold = 0.5f;

    [Range(0f, 1f)]
    public float extraDensity = 0.01f; // blades per world-space m², same units as density

    [Tooltip(
        "Size of each quad in world units (width, height). Height is used for wind sway scale."
    )]
    public Vector2 extraPlaneSize = new Vector2(0.1f, 0.1f);

    [Header("Debug")]
    [Tooltip(
        "Bypass all compute culling — renders all blades every frame. "
            + "Use to confirm whether culling is the cause of missing grass. "
            + "Will be slow at high blade counts."
    )]
    public bool disableCulling = false;

    [Tooltip("Log visible blade count to console once per second.")]
    public bool logVisibleCount = false;

    // ── Private GPU resources ────────────────────────────────────────────────
    ComputeBuffer _allBladesBuffer; // all blades, set once
    ComputeBuffer _visibleBladesBuffer; // per-frame append output
    ComputeBuffer _indirectArgsBuffer; // DrawMeshInstancedIndirect args
    ComputeBuffer _readbackBuffer; // single uint for visible-count debug readback

    Mesh _bladeMesh;
    int _totalBladeCount;
    int _kernelCull;
    Bounds _drawBounds;
    float _logTimer;

    // quad mesh used for GPU-instanced extras and associated groups
    Mesh _planeMesh;

    class ExtraGroup
    {
        public Material material;
        public ComputeBuffer all;
        public ComputeBuffer visible;
        public ComputeBuffer args;
        public int totalCount;
    }

    List<ExtraGroup> _extraGroups;

    // Indirect draw args layout: [indexCount, instanceCount, startIndex, baseVertex, startInstance]
    readonly uint[] _args = new uint[5];
    readonly uint[] _readback = new uint[1];

    // ── Blade data struct (must match GrassCompute.compute BladeData) ────────
    struct BladeData
    {
        public Vector3 position;
        public Vector3 normal;
        public float height;
        public float width;
        public float phase;
        public float facingAngle;

        // 10 floats × 4 bytes = 40 bytes
        public const int Stride = 40;
    }

    // ────────────────────────────────────────────────────────────────────────
    // Unity lifecycle
    // ────────────────────────────────────────────────────────────────────────

    void OnEnable() => Init();

    void OnDisable() => ReleaseBuffers();

    void OnDestroy()
    {
        ReleaseBuffers();
        DestroyBladeMesh();
        DestroyPlaneMesh();
    }

    void LateUpdate()
    {
        if (!grassEnabled)
            return;
        if (_allBladesBuffer == null)
            return;
        if (computeShader == null || grassMaterial == null)
            return;
        if (_totalBladeCount == 0)
            return;

        Camera cam = GetRenderCamera();
        if (cam == null)
        {
            Debug.LogWarning(
                "[GrassRenderer] No camera found. Assign one to the Target Camera field.",
                this
            );
            return;
        }

        // regular grass
        RunCullPass(_allBladesBuffer, _visibleBladesBuffer, _totalBladeCount, cam);
        _indirectArgsBuffer.SetData(_args);
        ComputeBuffer.CopyCount(_visibleBladesBuffer, _indirectArgsBuffer, sizeof(uint));
        if (disableCulling)
            grassMaterial.SetBuffer("_VisibleBlades", _allBladesBuffer);
        DispatchDraw(_bladeMesh, grassMaterial, _visibleBladesBuffer, _indirectArgsBuffer, cam);

        // extras quads/materials
        if (_extraGroups != null)
        {
            foreach (var g in _extraGroups)
            {
                RunCullPass(g.all, g.visible, g.totalCount, cam);
                // reuse same args array
                g.args.SetData(_args);
                ComputeBuffer.CopyCount(g.visible, g.args, sizeof(uint));
                if (disableCulling)
                    g.material.SetBuffer("_VisibleBlades", g.all);
                DispatchDraw(_planeMesh, g.material, g.visible, g.args, cam);

                if (logVisibleCount)
                {
                    var readArgs = new uint[5];
                    g.args.GetData(readArgs);
                    Debug.Log(
                        $"[GrassRenderer] Visible extras '{g.material.name}': {readArgs[1]:N0} / {g.totalCount:N0}"
                    );
                }
            }
        }
    }

    // Returns the camera to use for culling in priority order:
    //   1. Explicitly assigned targetCamera field
    //   2. Camera.main (requires MainCamera tag — may be null)
    //   3. First enabled camera in the scene (tag-independent)
    Camera GetRenderCamera()
    {
        if (targetCamera != null && targetCamera.isActiveAndEnabled)
            return targetCamera;
        if (Camera.main != null)
            return Camera.main;
        // Fall back: find any active camera regardless of tag
        foreach (var c in Camera.allCameras)
            if (c.isActiveAndEnabled)
                return c;
        return null;
    }

    // ────────────────────────────────────────────────────────────────────────
    // Public API (Timeline / code)
    // ────────────────────────────────────────────────────────────────────────

    public void SetGrassEnabled(bool enabled) => grassEnabled = enabled;

    /// <summary>
    /// Calculate the world-space size and position of the surface mesh and
    /// update the grass material's _GroundTex_ST so that sampling is aligned
    /// to the mesh bounds.  After this call the UV coordinate used by the
    /// shader (worldPos.xz * ST.xy + ST.zw) will range from 0..1 across the
    /// mesh footprint, making it trivial to tile or offset the texture.
    ///
    /// This method is invoked automatically during Init(), but you can also
    /// call it manually whenever the mesh or transform changes (e.g. in the
    /// editor after scaling the object).
    /// </summary>
    [ContextMenu("Align Ground Texture")]
    public void AlignGroundTexture()
    {
        if (grassMaterial == null)
        {
            Debug.LogWarning(
                "[GrassRenderer] cannot align ground texture – grassMaterial is null."
            );
            return;
        }

        var mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
        {
            Debug.LogWarning("[GrassRenderer] no mesh filter found for texture alignment.");
            return;
        }

        Bounds localB = mf.sharedMesh.bounds;
        Vector3 worldMin = transform.TransformPoint(localB.min);
        Vector3 worldMax = transform.TransformPoint(localB.max);

        float width = Mathf.Abs(worldMax.x - worldMin.x);
        float depth = Mathf.Abs(worldMax.z - worldMin.z);

        // avoid divide-by-zero
        if (width <= 0f)
            width = 1f;
        if (depth <= 0f)
            depth = 1f;

        Vector2 scale = new Vector2(1f / width, 1f / depth);
        Vector2 offset = new Vector2(-worldMin.x * scale.x, -worldMin.z * scale.y);

        grassMaterial.SetVector("_GroundTex_ST", new Vector4(scale.x, scale.y, offset.x, offset.y));

        Debug.Log($"[GrassRenderer] ground tex ST set: scale={scale} offset={offset}");
    }

    // ────────────────────────────────────────────────────────────────────────
    // Initialisation
    // ────────────────────────────────────────────────────────────────────────

    void Init()
    {
        if (computeShader == null || grassMaterial == null)
            return;

        var mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
            return;

        ReleaseBuffers();
        DestroyBladeMesh();
        DestroyPlaneMesh();
        BuildBladeMesh();
        BuildPlaneMesh();
        BuildBladeData(mf.sharedMesh);
        BuildExtraGroups(mf.sharedMesh);

        // automatically align the ground texture to the mesh bounds if material present
        AlignGroundTexture();

        _kernelCull = computeShader.FindKernel("CullGrass");
    }

    // ── Build the procedural blade template mesh ─────────────────────────────
    // Vertices encode the blade shape as (side, t, 0):
    //   side = -0.5..0.5 (left/right edge), t = 0..1 (base→tip)
    // The vertex shader reads these and reconstructs world positions per instance.
    //
    //    4 (tip, centre)
    //   / \
    //  2   3  (mid)
    //  |   |
    //  0   1  (base)
    void BuildBladeMesh()
    {
        _bladeMesh = new Mesh { name = "GrassBladeProcedural" };

        var verts = new Vector3[5];
        var uvs = new Vector2[5];

        verts[0] = new Vector3(-0.5f, 0.00f, 0);
        uvs[0] = new Vector2(0.0f, 0.00f);
        verts[1] = new Vector3(0.5f, 0.00f, 0);
        uvs[1] = new Vector2(1.0f, 0.00f);
        verts[2] = new Vector3(-0.5f, 0.55f, 0);
        uvs[2] = new Vector2(0.0f, 0.55f);
        verts[3] = new Vector3(0.5f, 0.55f, 0);
        uvs[3] = new Vector2(1.0f, 0.55f);
        verts[4] = new Vector3(0.0f, 1.00f, 0);
        uvs[4] = new Vector2(0.5f, 1.00f);

        _bladeMesh.SetVertices(verts);
        _bladeMesh.SetUVs(0, uvs);
        _bladeMesh.SetTriangles(new[] { 0, 2, 1, 1, 2, 3, 2, 4, 3 }, 0);
        _bladeMesh.RecalculateNormals();
        // Artificially large bounds — Unity's per-mesh frustum cull must never fire;
        // our compute cull handles actual per-blade visibility.
        _bladeMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100_000f);
    }

    void BuildPlaneMesh()
    {
        if (_planeMesh != null)
            return;
        _planeMesh = new Mesh { name = "GrassExtraPlane" };
        // vertical unit quad: x = -0.5..0.5, y = 0..1, z = 0
        var verts = new Vector3[4]
        {
            new Vector3(-0.5f, 0, 0),
            new Vector3(0.5f, 0, 0),
            new Vector3(-0.5f, 1, 0),
            new Vector3(0.5f, 1, 0),
        };
        var uvs = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1),
        };
        _planeMesh.SetVertices(verts);
        _planeMesh.SetUVs(0, uvs);
        _planeMesh.SetTriangles(new[] { 0, 1, 2, 2, 1, 3 }, 0);
        _planeMesh.RecalculateNormals();
        _planeMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);
    }

    void DestroyPlaneMesh()
    {
        if (_planeMesh == null)
            return;
#if UNITY_EDITOR
        if (!Application.isPlaying)
            DestroyImmediate(_planeMesh);
        else
#endif
            Destroy(_planeMesh);
        _planeMesh = null;
    }

    void BuildExtraGroups(Mesh mesh)
    {
        // compute mesh CDF as before
        bool hasMask = maskedExtraMask != null && maskedExtraMask.isReadable;
        if (maskedExtraMask != null && !hasMask)
            Debug.LogWarning(
                "[GrassRenderer] Masked extra texture is not Read/Write enabled – mask will be ignored."
            );

        var verts = mesh.vertices;
        var tris = mesh.triangles;
        var normals =
            mesh.normals.Length == verts.Length
                ? mesh.normals
                : Enumerable.Repeat(Vector3.up, verts.Length).ToArray();
        var uvList = new List<Vector2>();
        mesh.GetUVs(0, uvList);
        var uvs = uvList.Count == verts.Length ? uvList.ToArray() : new Vector2[verts.Length];

        int triCount = tris.Length / 3;
        var areas = new float[triCount];
        float totalArea = 0f;
        for (int ti = 0; ti < triCount; ti++)
        {
            var w0 = transform.TransformPoint(verts[tris[ti * 3 + 0]]);
            var w1 = transform.TransformPoint(verts[tris[ti * 3 + 1]]);
            var w2 = transform.TransformPoint(verts[tris[ti * 3 + 2]]);
            float a = Vector3.Cross(w1 - w0, w2 - w0).magnitude * 0.5f;
            areas[ti] = a;
            totalArea += a;
        }
        if (totalArea <= 0f)
            return;
        var cdf = new float[triCount];
        float running = 0f;
        for (int ti = 0; ti < triCount; ti++)
        {
            running += areas[ti];
            cdf[ti] = running / totalArea;
        }

        int targetCount = Mathf.RoundToInt(totalArea * extraDensity);
        if (targetCount == 0)
            return;

        _extraGroups = new List<ExtraGroup>();

        // helper to scatter given count with optional mask predicate
        System.Func<int, Predicate<Vector2>, List<BladeData>> scatter = (count, maskPred) =>
        {
            var list = new List<BladeData>(count);
            var rng = new System.Random(12345);
            int attempts = 0,
                maxAttempts = count * 4;
            int rejects = 0;
            while (list.Count < count && attempts < maxAttempts)
            {
                attempts++;
                float r = (float)rng.NextDouble();
                int lo = 0,
                    hi = triCount - 1;
                while (lo < hi)
                {
                    int mid = (lo + hi) >> 1;
                    if (cdf[mid] < r)
                        lo = mid + 1;
                    else
                        hi = mid;
                }
                int ti = lo;
                int i0 = tris[ti * 3],
                    i1 = tris[ti * 3 + 1],
                    i2 = tris[ti * 3 + 2];
                float r1 = Mathf.Sqrt((float)rng.NextDouble());
                float r2 = (float)rng.NextDouble();
                float u = 1f - r1,
                    v = r1 * (1f - r2),
                    w = r1 * r2;

                Vector3 localPos = u * verts[i0] + v * verts[i1] + w * verts[i2];
                Vector3 pos = transform.TransformPoint(localPos);
                Vector3 normal = transform
                    .TransformDirection((u * normals[i0] + v * normals[i1] + w * normals[i2]))
                    .normalized;
                Vector2 uv = u * uvs[i0] + v * uvs[i1] + w * uvs[i2];

                if (maskPred != null && maskPred(uv))
                {
                    rejects++;
                    continue;
                }

                list.Add(
                    new BladeData
                    {
                        position = pos,
                        normal = normal,
                        height = extraPlaneSize.y,
                        width = extraPlaneSize.x,
                        phase = (float)(rng.NextDouble() * Mathf.PI * 2.0),
                        facingAngle = (float)(rng.NextDouble() * Mathf.PI * 2.0),
                    }
                );
            }
            if (rejects > 0)
                Debug.Log($"[GrassRenderer] Mask rejected {rejects} candidates.");
            return list;
        };

        // unmasked materials
        if (extraMaterials != null && extraMaterials.Count > 0)
        {
            int perMat = targetCount; // could distribute differently
            foreach (var mat in extraMaterials)
            {
                if (mat == null)
                    continue;
                if (!mat.enableInstancing)
                    Debug.LogWarning(
                        $"[GrassRenderer] extra material '{mat.name}' has GPU instancing disabled."
                    );
                var bladesList = scatter(perMat, null);
                CreateGroup(mat, bladesList);
            }
        }

        // masked material
        if (maskedExtraMaterial != null)
        {
            Predicate<Vector2> maskPred = null;
            if (hasMask)
            {
                maskPred = uv =>
                    maskedExtraMask.GetPixelBilinear(uv.x, uv.y).grayscale < maskedExtraThreshold;
                Debug.Log(
                    $"[GrassRenderer] Masked extras using '{maskedExtraMask.name}' threshold {maskedExtraThreshold}"
                );
            }
            var bladesList = scatter(targetCount, maskPred);
            CreateGroup(maskedExtraMaterial, bladesList);
            Debug.Log(
                $"[GrassRenderer] Masked extras generated {bladesList.Count} instances (density {extraDensity})."
            );
        }
    }

    void CreateGroup(Material mat, List<BladeData> blades)
    {
        var group = new ExtraGroup
        {
            material = mat,
            totalCount = blades.Count,
            all = new ComputeBuffer(blades.Count, BladeData.Stride),
            visible = new ComputeBuffer(blades.Count, BladeData.Stride, ComputeBufferType.Append),
            args = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments),
        };
        group.all.SetData(blades.ToArray());
        uint[] args = new uint[5];
        args[0] = (uint)_planeMesh.GetIndexCount(0);
        args[1] = 0;
        args[2] = (uint)_planeMesh.GetIndexStart(0);
        args[3] = (uint)_planeMesh.GetBaseVertex(0);
        args[4] = 0;
        group.args.SetData(args);
        _extraGroups.Add(group);
    }

    // ── Scatter blade positions over the mesh surface ────────────────────────
    // Uses area-weighted CDF sampling: every blade picks a triangle proportional
    // to its world-space area, then a random point on that triangle. This is
    // completely immune to non-uniform triangle density — finely subdivided regions
    // (coastlines, ridges) and coarse flat regions contribute equally per m².
    void BuildBladeData(Mesh mesh)
    {
        var verts = mesh.vertices;
        var tris = mesh.triangles;

        var normals = new Vector3[verts.Length];
        if (mesh.normals.Length == verts.Length)
            mesh.normals.CopyTo(normals, 0);
        else
            for (int i = 0; i < normals.Length; i++)
                normals[i] = Vector3.up;

        var uvList = new List<Vector2>();
        mesh.GetUVs(0, uvList);
        var uvs = uvList.Count == verts.Length ? uvList.ToArray() : new Vector2[verts.Length];

        bool hasMask = maskTexture != null && maskTexture.isReadable;
        if (maskTexture != null && !maskTexture.isReadable)
            Debug.LogWarning(
                "[GrassRenderer] Mask texture is not Read/Write enabled — mask will be ignored."
            );

        int triCount = tris.Length / 3;

        // ── Pass 1: compute world-space area of every triangle and build a CDF ──
        // We use world-space area so density is in blades-per-world-m² regardless
        // of the mesh's local scale or the object's Transform scale.
        var areas = new float[triCount];
        float totalArea = 0f;

        for (int ti = 0; ti < triCount; ti++)
        {
            int i0 = tris[ti * 3],
                i1 = tris[ti * 3 + 1],
                i2 = tris[ti * 3 + 2];
            Vector3 w0 = transform.TransformPoint(verts[i0]);
            Vector3 w1 = transform.TransformPoint(verts[i1]);
            Vector3 w2 = transform.TransformPoint(verts[i2]);
            areas[ti] = Vector3.Cross(w1 - w0, w2 - w0).magnitude * 0.5f;
            totalArea += areas[ti];
        }

        if (totalArea <= 0f)
        {
            Debug.LogWarning(
                $"[GrassRenderer] Mesh on {name} has zero world-space area — check Transform scale and mesh data."
            );
            return;
        }

        // CDF: cdf[i] = cumulative area up to (and including) triangle i, normalised 0..1
        var cdf = new float[triCount];
        float running = 0f;
        for (int ti = 0; ti < triCount; ti++)
        {
            running += areas[ti];
            cdf[ti] = running / totalArea;
        }

        // ── Pass 2: place blades ──────────────────────────────────────────────
        // Target blade count from area × density, clamped to maxBlades.
        int targetCount = Mathf.Min(Mathf.RoundToInt(totalArea * density), maxBlades);

        Debug.Log(
            $"[GrassRenderer] Mesh world area: {totalArea:F1} m²  →  targeting {targetCount:N0} blades at density {density} (max {maxBlades:N0})."
        );

        var blades = new List<BladeData>(targetCount);
        var boundsCalc = new Bounds();
        bool firstBlade = true;
        var rng = new System.Random(42);

        // Over-sample to account for mask rejection; stop when we hit targetCount.
        // Each rejected blade (failed mask test) draws another candidate.
        int maxAttempts = targetCount * 4; // safety ceiling
        int attempts = 0;

        while (blades.Count < targetCount && attempts < maxAttempts)
        {
            attempts++;

            // Binary search into CDF to pick a triangle weighted by area
            float r = (float)rng.NextDouble();
            int lo = 0,
                hi = triCount - 1;
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                if (cdf[mid] < r)
                    lo = mid + 1;
                else
                    hi = mid;
            }
            int ti = lo;

            int i0 = tris[ti * 3],
                i1 = tris[ti * 3 + 1],
                i2 = tris[ti * 3 + 2];

            // Uniform random point on the chosen triangle (Osada et al.)
            float r1 = Mathf.Sqrt((float)rng.NextDouble());
            float r2 = (float)rng.NextDouble();
            float u = 1f - r1;
            float v = r1 * (1f - r2);
            float w = r1 * r2;

            // Interpolate in local space, then transform to world space.
            // TransformPoint applies the full TRS hierarchy (position, rotation, scale)
            // so world-space positions are correct regardless of object scale or rotation.
            Vector3 localPos = u * verts[i0] + v * verts[i1] + w * verts[i2];
            Vector3 pos = transform.TransformPoint(localPos);
            Vector3 normal = transform
                .TransformDirection((u * normals[i0] + v * normals[i1] + w * normals[i2]))
                .normalized;
            Vector2 texUV = u * uvs[i0] + v * uvs[i1] + w * uvs[i2];

            // Sample mask value (1=full height, 0=zero height); default=1
            float maskVal = 1f;
            if (hasMask)
            {
                maskVal = maskTexture.GetPixelBilinear(texUV.x, texUV.y).grayscale;
                // remap according to zero threshold slider
                maskVal = Mathf.Clamp01((maskVal - maskZero) / Mathf.Max(1f - maskZero, 0.0001f));
            }

            // optionally skip entirely if maskVal is zero to avoid degenerate blades
            if (maskVal <= 0f)
                continue;

            float baseHeight = Mathf.Lerp(minHeight, maxHeight, (float)rng.NextDouble());

            blades.Add(
                new BladeData
                {
                    position = pos,
                    normal = normal,
                    height = baseHeight * maskVal,
                    width = Mathf.Lerp(minWidth, maxWidth, (float)rng.NextDouble()),
                    phase = (float)(rng.NextDouble() * Mathf.PI * 2.0),
                    facingAngle = (float)(rng.NextDouble() * Mathf.PI * 2.0),
                }
            );

            if (firstBlade)
            {
                boundsCalc = new Bounds(pos, Vector3.zero);
                firstBlade = false;
            }
            else
                boundsCalc.Encapsulate(pos);
        }

        _totalBladeCount = blades.Count;
        if (_totalBladeCount == 0)
        {
            Debug.LogWarning(
                $"[GrassRenderer] No blades placed on {name}. Check density and mask."
            );
            return;
        }

        Debug.Log(
            $"[GrassRenderer] Generated {_totalBladeCount:N0} blades on {name} "
                + $"| world bounds {boundsCalc.min:F1} → {boundsCalc.max:F1}"
        );

        boundsCalc.Expand(maxHeight * 4f);
        _drawBounds = boundsCalc;

        _allBladesBuffer = new ComputeBuffer(_totalBladeCount, BladeData.Stride);
        _visibleBladesBuffer = new ComputeBuffer(
            _totalBladeCount,
            BladeData.Stride,
            ComputeBufferType.Append
        );
        _indirectArgsBuffer = new ComputeBuffer(
            1,
            5 * sizeof(uint),
            ComputeBufferType.IndirectArguments
        );

        _allBladesBuffer.SetData(blades.ToArray());

        // masked extras handled separately from BuildExtraGroups

        // Pre-fill args with the (constant) index layout; instanceCount written by CopyCount each frame
        _args[0] = (uint)_bladeMesh.GetIndexCount(0);
        _args[1] = 0;
        _args[2] = (uint)_bladeMesh.GetIndexStart(0);
        _args[3] = (uint)_bladeMesh.GetBaseVertex(0);
        _args[4] = 0;
    }

    // ────────────────────────────────────────────────────────────────────────
    // Per-frame GPU work
    // ────────────────────────────────────────────────────────────────────────

    // generic culling helper for any compute-buffer pair
    void RunCullPass(ComputeBuffer all, ComputeBuffer visible, int totalCount, Camera cam)
    {
        if (disableCulling)
        {
            visible.SetCounterValue(0);
            // same bypass logic as before but only update args array; the caller will
            // copy to its own args buffer
            _args[1] = (uint)totalCount;
            // material binding left to caller
            if (logVisibleCount)
            {
                _logTimer += Time.deltaTime;
                if (_logTimer >= 1f)
                {
                    _logTimer = 0;
                    Debug.Log(
                        $"[GrassRenderer] Culling DISABLED — drawing all {totalCount:N0} instances."
                    );
                }
            }
            return;
        }

        visible.SetCounterValue(0);
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);
        var planeV4 = new Vector4[6];
        for (int i = 0; i < 6; i++)
            planeV4[i] = new Vector4(
                planes[i].normal.x,
                planes[i].normal.y,
                planes[i].normal.z,
                planes[i].distance
            );

        computeShader.SetInt("_TotalBladeCount", totalCount);
        computeShader.SetVectorArray("_FrustumPlanes", planeV4);
        computeShader.SetVector("_CameraPos", cam.transform.position);
        computeShader.SetFloat("_MaxDistance", maxDistance);
        computeShader.SetBuffer(_kernelCull, "_AllBlades", all);
        computeShader.SetBuffer(_kernelCull, "_VisibleBlades", visible);
        computeShader.Dispatch(_kernelCull, Mathf.CeilToInt(totalCount / 64f), 1, 1);

        // caller handles copying count to args
        if (logVisibleCount && totalCount == _totalBladeCount)
        {
            // we only log for the main grass set as before
            _logTimer += Time.deltaTime;
            if (_logTimer >= 1f)
            {
                _logTimer = 0;
                if (_readbackBuffer == null)
                    _readbackBuffer = new ComputeBuffer(
                        1,
                        sizeof(uint),
                        ComputeBufferType.IndirectArguments
                    );
                var readArgs = new uint[5];
                if (all == _allBladesBuffer)
                {
                    _indirectArgsBuffer.GetData(readArgs);
                    Debug.Log(
                        $"[GrassRenderer] Visible blades: {readArgs[1]:N0} / {_totalBladeCount:N0}  | cam pos: {cam.transform.position:F1}  maxDist: {maxDistance}"
                    );
                }
            }
        }
    }

    void DispatchDraw(
        Mesh mesh,
        Material mat,
        ComputeBuffer visible,
        ComputeBuffer args,
        Camera cam
    )
    {
        if (!disableCulling)
            mat.SetBuffer("_VisibleBlades", visible);
        mat.SetVector("_CameraPosition", cam.transform.position);
        mat.SetFloat("_MaxDistance", maxDistance);
        mat.SetFloat("_FadeStartDistance", fadeStartDistance);

        Graphics.DrawMeshInstancedIndirect(
            mesh,
            0,
            mat,
            _drawBounds,
            args,
            0,
            null,
            shadowCasting,
            true
        );
    }

    void DispatchDraw(Camera cam)
    {
        // When culling is disabled RunCullPass already set the buffer to _allBladesBuffer
        if (!disableCulling)
            grassMaterial.SetBuffer("_VisibleBlades", _visibleBladesBuffer);
        grassMaterial.SetVector("_CameraPosition", cam.transform.position);
        grassMaterial.SetFloat("_MaxDistance", maxDistance);
        grassMaterial.SetFloat("_FadeStartDistance", fadeStartDistance);

        Graphics.DrawMeshInstancedIndirect(
            _bladeMesh,
            0,
            grassMaterial,
            _drawBounds,
            _indirectArgsBuffer,
            0,
            null,
            shadowCasting,
            true
        ); // receiveShadows
    }

    // ────────────────────────────────────────────────────────────────────────
    // Cleanup
    // ────────────────────────────────────────────────────────────────────────

    void ReleaseBuffers()
    {
        _allBladesBuffer?.Release();
        _allBladesBuffer = null;
        _visibleBladesBuffer?.Release();
        _visibleBladesBuffer = null;
        _indirectArgsBuffer?.Release();
        _indirectArgsBuffer = null;
        _readbackBuffer?.Release();
        _readbackBuffer = null;

        if (_extraGroups != null)
        {
            foreach (var g in _extraGroups)
            {
                g.all?.Release();
                g.visible?.Release();
                g.args?.Release();
            }
            _extraGroups = null;
        }
    }

    void DestroyBladeMesh()
    {
        if (_bladeMesh == null)
            return;
#if UNITY_EDITOR
        if (!Application.isPlaying)
            DestroyImmediate(_bladeMesh);
        else
#endif
            Destroy(_bladeMesh);
        _bladeMesh = null;
    }

    // ────────────────────────────────────────────────────────────────────────
    // Editor helpers
    // ────────────────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    [ContextMenu("Regenerate Grass")]
    public void RegenerateGrass() => Init();

    void OnValidate()
    {
        // Clamp independent minimums
        minHeight = Mathf.Min(minHeight, maxHeight);
        minWidth = Mathf.Min(minWidth, maxWidth);

        // ensure distances are non-negative and fade never exceeds max
        maxDistance = Mathf.Max(0f, maxDistance);
        fadeStartDistance = Mathf.Clamp(fadeStartDistance, 0f, maxDistance);
        // masked extras threshold clamp
        maskedExtraThreshold = Mathf.Clamp01(maskedExtraThreshold);

#if UNITY_EDITOR
        if (maskedExtraMask != null && !maskedExtraMask.isReadable)
            Debug.LogWarning(
                "[GrassRenderer] maskedExtraMask is not Read/Write enabled – consider enabling read/write in import settings so the mask is used."
            );
#endif
    }
#endif
}
