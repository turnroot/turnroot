using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
public class GrassRenderer : MonoBehaviour
{
    // ── References ────────────────────────────────────────────────────────────
    [Header("References")]
    public ComputeShader computeShader;
    public Material grassMaterial;

    [Tooltip("Culling camera. Falls back to Camera.main, then any active camera.")]
    public Camera targetCamera;

    // ── Mask ──────────────────────────────────────────────────────────────────
    [Header("Mask (texture must be Read/Write enabled in Import Settings)")]
    public Texture2D maskTexture;

    [Range(0f, 1f), Tooltip("Mask values at or below this floor are treated as zero height.")]
    public float maskFloor = 0f;

    // ── Density & Blade Size ──────────────────────────────────────────────────
    [Header("Density")]
    [Range(1f, 300f)]
    public float density = 60f; // blades per world-space m²
    public int maxBlades = 1_000_000;

    [Header("Blade Size")]
    public float minHeight = 0.15f;
    public float maxHeight = 0.45f;
    public float minWidth = 0.02f;
    public float maxWidth = 0.05f;

    // ── Culling ───────────────────────────────────────────────────────────────
    [Header("Culling")]
    public float maxDistance = 50f;
    public float fadeStartDistance = 35f;

    // ── Runtime ───────────────────────────────────────────────────────────────
    [Header("Runtime")]
    public bool grassEnabled = true;
    public ShadowCastingMode shadowCasting = ShadowCastingMode.Off;

    // ── Unmasked Extras ───────────────────────────────────────────────────────
    [Header("Unmasked Extras — one scatter pass per material, ignores mask")]
    public List<Material> extraMaterials = new List<Material>();

    [Range(0f, 1f)]
    public float unmaskedExtraDensity = 0.01f; // quads per m²
    public Vector2 unmaskedExtraSize = new Vector2(0.1f, 0.1f);

    // ── Masked Extras ─────────────────────────────────────────────────────────
    [Header("Masked Extras — quads spawned where maskedExtraMask is white")]
    public Material maskedExtraMaterial;
    public Texture2D maskedExtraMask;

    [Range(0f, 1f)]
    public float maskedExtraThreshold = 0.5f;

    [Range(0f, 1f)]
    public float maskedExtraDensity = 0.01f; // quads per m²
    public Vector2 maskedExtraSize = new Vector2(0.1f, 0.1f);

    // ── Debug ─────────────────────────────────────────────────────────────────
    [Header("Debug")]
    [Tooltip("Bypass compute culling and render all blades every frame. Slow at high counts.")]
    public bool disableCulling = false;

    [Tooltip("Log visible blade count to console once per second.")]
    public bool logVisibleCount = false;

    // ── GPU resources ─────────────────────────────────────────────────────────
    private ComputeBuffer _allBladesBuffer;
    private ComputeBuffer _visibleBladesBuffer;
    private ComputeBuffer _indirectArgsBuffer;
    private ComputeBuffer _readbackBuffer;

    private Mesh _bladeMesh;
    private Mesh _planeMesh;
    private int _totalBladeCount;
    private int _cullKernel;
    private Bounds _drawBounds;
    private float _logTimer;

    // Indirect args layout: [indexCount, instanceCount, startIndex, baseVertex, startInstance]
    private readonly uint[] _args = new uint[5];

    // Must match BladeData in GrassCompute.compute (10 floats = 40 bytes)
    private struct BladeData
    {
        public Vector3 position;
        public Vector3 normal;
        public float height;
        public float width;
        public float phase;
        public float facingAngle;
        public const int Stride = 40;
    }

    private class ExtraGroup
    {
        public Material material;
        public ComputeBuffer all;
        public ComputeBuffer visible;
        public ComputeBuffer args;
        public int totalCount;
    }

    private List<ExtraGroup> _extraGroups;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void OnEnable() => Init();

    private void OnDisable() => ReleaseBuffers();

    private void OnDestroy()
    {
        ReleaseBuffers();
        DestroyMesh(ref _bladeMesh);
        DestroyMesh(ref _planeMesh);
    }

    private void LateUpdate()
    {
        if (
            !grassEnabled
            || _allBladesBuffer == null
            || computeShader == null
            || grassMaterial == null
            || _totalBladeCount == 0
        )
        {
            return;
        }

        Camera cam = GetRenderCamera();
        if (cam == null)
        {
            return;
        }

        DrawGroup(
            _allBladesBuffer,
            _visibleBladesBuffer,
            _indirectArgsBuffer,
            _totalBladeCount,
            _bladeMesh,
            grassMaterial,
            cam
        );

        if (_extraGroups != null)
        {
            foreach (var g in _extraGroups)
            {
                DrawGroup(g.all, g.visible, g.args, g.totalCount, _planeMesh, g.material, cam);
            }
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────
    public void SetGrassEnabled(bool enabled) => grassEnabled = enabled;

    /// <summary>
    /// Sets _GroundTex_ST so world-space UVs span 0–1 over the mesh footprint.
    /// Called automatically on Init; call manually after rescaling.
    /// </summary>
    [ContextMenu("Align Ground Texture")]
    public void AlignGroundTexture()
    {
        if (grassMaterial == null)
        {
            return;
        }

        var mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
        {
            return;
        }

        Bounds local = mf.sharedMesh.bounds;
        Vector3 worldMin = transform.TransformPoint(local.min);
        Vector3 worldMax = transform.TransformPoint(local.max);

        float w = Mathf.Max(Mathf.Abs(worldMax.x - worldMin.x), 0.001f);
        float d = Mathf.Max(Mathf.Abs(worldMax.z - worldMin.z), 0.001f);

        Vector2 scale = new Vector2(1f / w, 1f / d);
        Vector2 offset = new Vector2(-worldMin.x * scale.x, -worldMin.z * scale.y);
        grassMaterial.SetVector("_GroundTex_ST", new Vector4(scale.x, scale.y, offset.x, offset.y));
    }

    // ── Camera resolution ─────────────────────────────────────────────────────
    private Camera GetRenderCamera()
    {
        if (targetCamera != null && targetCamera.isActiveAndEnabled)
        {
            return targetCamera;
        }

        if (Camera.main != null)
        {
            return Camera.main;
        }

        foreach (var c in Camera.allCameras)
        {
            if (c.isActiveAndEnabled)
            {
                return c;
            }
        }

        return null;
    }

    // ── Init ──────────────────────────────────────────────────────────────────
    private void Init()
    {
        if (computeShader == null || grassMaterial == null)
        {
            return;
        }

        var mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
        {
            return;
        }

        ReleaseBuffers();
        DestroyMesh(ref _bladeMesh);
        DestroyMesh(ref _planeMesh);

        BuildBladeMesh();
        BuildPlaneMesh();
        BuildBladeData(mf.sharedMesh);
        BuildExtraGroups(mf.sharedMesh);
        AlignGroundTexture();

        _cullKernel = computeShader.FindKernel("CullGrass");
    }

    // ── Mesh construction ─────────────────────────────────────────────────────
    // Blade: 5-vertex tapered quad. vertex.x = side (–0.5..0.5), vertex.y = t (0..1).
    // The shader reconstructs world positions per instance from BladeData.
    //     [4] tip
    //    [2][3] mid
    //    [0][1] base
    private void BuildBladeMesh()
    {
        _bladeMesh = new Mesh { name = "GrassBlade" };
        _bladeMesh.SetVertices(
            new Vector3[]
            {
                new(-0.5f, 0.00f, 0),
                new(0.5f, 0.00f, 0),
                new(-0.5f, 0.55f, 0),
                new(0.5f, 0.55f, 0),
                new(0.0f, 1.00f, 0),
            }
        );
        _bladeMesh.SetUVs(
            0,
            new Vector2[]
            {
                new(0f, 0.00f),
                new(1f, 0.00f),
                new(0f, 0.55f),
                new(1f, 0.55f),
                new(0.5f, 1f),
            }
        );
        _bladeMesh.SetTriangles(new[] { 0, 2, 1, 1, 2, 3, 2, 4, 3 }, 0);
        _bladeMesh.RecalculateNormals();
        // Oversized bounds prevent Unity's per-mesh frustum cull from firing;
        // per-blade visibility is handled by the compute shader.
        _bladeMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100_000f);
    }

    // Extra: vertical unit quad, x = –0.5..0.5, y = 0..1.
    private void BuildPlaneMesh()
    {
        _planeMesh = new Mesh { name = "GrassExtraPlane" };
        _planeMesh.SetVertices(
            new Vector3[] { new(-0.5f, 0, 0), new(0.5f, 0, 0), new(-0.5f, 1, 0), new(0.5f, 1, 0) }
        );
        _planeMesh.SetUVs(0, new Vector2[] { new(0, 0), new(1, 0), new(0, 1), new(1, 1) });
        _planeMesh.SetTriangles(new[] { 0, 1, 2, 2, 1, 3 }, 0);
        _planeMesh.RecalculateNormals();
        _planeMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100_000f);
    }

    // ── Blade placement ───────────────────────────────────────────────────────
    // Area-weighted CDF sampling: each blade picks a triangle proportional to
    // world-space area, then a uniform random point on that triangle.
    // Density is blades/m² regardless of mesh subdivision or object scale.
    private void BuildBladeData(Mesh mesh)
    {
        var (verts, tris, normals, uvs) = GetMeshArrays(mesh);
        var (cdf, totalArea) = BuildAreaCDF(verts, tris);
        if (totalArea <= 0f)
        {
            return;
        }

        bool hasMask = maskTexture != null && maskTexture.isReadable;
        int triCount = tris.Length / 3;
        int targetCount = Mathf.Min(Mathf.RoundToInt(totalArea * density), maxBlades);
        if (targetCount == 0)
        {
            return;
        }

        var blades = new List<BladeData>(targetCount);
        var bounds = new Bounds();
        bool first = true;
        var rng = new System.Random(42);

        for (int attempt = 0; blades.Count < targetCount && attempt < targetCount * 4; attempt++)
        {
            SampleTriangle(
                rng,
                cdf,
                triCount,
                tris,
                verts,
                normals,
                uvs,
                out Vector3 pos,
                out Vector3 normal,
                out Vector2 uv
            );

            float maskVal = 1f;
            if (hasMask)
            {
                float raw = maskTexture.GetPixelBilinear(uv.x, uv.y).grayscale;
                maskVal = Mathf.Clamp01((raw - maskFloor) / Mathf.Max(1f - maskFloor, 0.0001f));
            }
            if (maskVal <= 0f)
            {
                continue;
            }

            if (first)
            {
                bounds = new Bounds(pos, Vector3.zero);
                first = false;
            }
            else
            {
                bounds.Encapsulate(pos);
            }

            blades.Add(
                new BladeData
                {
                    position = pos,
                    normal = normal,
                    height = Mathf.Lerp(minHeight, maxHeight, (float)rng.NextDouble()) * maskVal,
                    width = Mathf.Lerp(minWidth, maxWidth, (float)rng.NextDouble()),
                    phase = (float)(rng.NextDouble() * Math.PI * 2.0),
                    facingAngle = (float)(rng.NextDouble() * Math.PI * 2.0),
                }
            );
        }

        _totalBladeCount = blades.Count;
        if (_totalBladeCount == 0)
        {
            return;
        }

        bounds.Expand(maxHeight * 4f);
        _drawBounds = bounds;

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

        _args[0] = (uint)_bladeMesh.GetIndexCount(0);
        _args[1] = 0;
        _args[2] = (uint)_bladeMesh.GetIndexStart(0);
        _args[3] = (uint)_bladeMesh.GetBaseVertex(0);
        _args[4] = 0;
    }

    // ── Extra group placement ─────────────────────────────────────────────────
    private void BuildExtraGroups(Mesh mesh)
    {
        _extraGroups = new List<ExtraGroup>();

        bool hasUnmasked =
            extraMaterials != null && extraMaterials.Count > 0 && unmaskedExtraDensity > 0f;
        bool hasMasked = maskedExtraMaterial != null && maskedExtraDensity > 0f;
        if (!hasUnmasked && !hasMasked)
        {
            return;
        }

        var (verts, tris, normals, uvs) = GetMeshArrays(mesh);
        var (cdf, totalArea) = BuildAreaCDF(verts, tris);
        if (totalArea <= 0f)
        {
            return;
        }

        int triCount = tris.Length / 3;

        if (hasUnmasked)
        {
            bool hasGrassMask = maskTexture != null && maskTexture.isReadable;
            Predicate<Vector2> reject = hasGrassMask
                ? uv => maskTexture.GetPixelBilinear(uv.x, uv.y).grayscale < maskFloor
                : (Predicate<Vector2>)null;

            int poolCount = Mathf.RoundToInt(
                totalArea * unmaskedExtraDensity * extraMaterials.Count
            );
            var pool = ScatterBlades(
                poolCount,
                cdf,
                triCount,
                tris,
                verts,
                normals,
                uvs,
                reject,
                unmaskedExtraSize
            );

            var rng2 = new System.Random(98765);
            var bins = new List<BladeData>[extraMaterials.Count];
            for (int i = 0; i < bins.Length; i++)
            {
                bins[i] = new List<BladeData>();
            }

            foreach (var b in pool)
            {
                bins[rng2.Next(bins.Length)].Add(b);
            }

            for (int i = 0; i < extraMaterials.Count; i++)
            {
                if (extraMaterials[i] != null && bins[i].Count > 0)
                {
                    CreateExtraGroup(extraMaterials[i], bins[i]);
                }
            }
        }

        if (hasMasked)
        {
            bool hasMaskTex = maskedExtraMask != null && maskedExtraMask.isReadable;
            Predicate<Vector2> reject = hasMaskTex
                ? uv =>
                    maskedExtraMask.GetPixelBilinear(uv.x, uv.y).grayscale < maskedExtraThreshold
                : (Predicate<Vector2>)null;

            int count = Mathf.RoundToInt(totalArea * maskedExtraDensity);
            var blades = ScatterBlades(
                count,
                cdf,
                triCount,
                tris,
                verts,
                normals,
                uvs,
                reject,
                maskedExtraSize
            );
            if (blades.Count > 0)
            {
                CreateExtraGroup(maskedExtraMaterial, blades);
            }
        }
    }

    // Scatter `count` blades; skip candidates where `reject(uv)` returns true.
    private List<BladeData> ScatterBlades(
        int count,
        float[] cdf,
        int triCount,
        int[] tris,
        Vector3[] verts,
        Vector3[] normals,
        Vector2[] uvs,
        Predicate<Vector2> reject,
        Vector2 planeSize
    )
    {
        var list = new List<BladeData>(Mathf.Max(count, 0));
        if (count <= 0)
        {
            return list;
        }

        var rng = new System.Random(12345);
        for (int attempt = 0; list.Count < count && attempt < count * 4; attempt++)
        {
            SampleTriangle(
                rng,
                cdf,
                triCount,
                tris,
                verts,
                normals,
                uvs,
                out Vector3 pos,
                out Vector3 normal,
                out Vector2 uv
            );
            if (reject != null && reject(uv))
            {
                continue;
            }

            list.Add(
                new BladeData
                {
                    position = pos,
                    normal = normal,
                    height = planeSize.y,
                    width = planeSize.x,
                    phase = (float)(rng.NextDouble() * Math.PI * 2.0),
                    facingAngle = (float)(rng.NextDouble() * Math.PI * 2.0),
                }
            );
        }
        return list;
    }

    private void CreateExtraGroup(Material mat, List<BladeData> blades)
    {
        if (blades == null || blades.Count == 0)
        {
            return;
        }

        var g = new ExtraGroup
        {
            material = mat,
            totalCount = blades.Count,
            all = new ComputeBuffer(blades.Count, BladeData.Stride),
            visible = new ComputeBuffer(blades.Count, BladeData.Stride, ComputeBufferType.Append),
            args = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments),
        };
        g.all.SetData(blades.ToArray());
        g.args.SetData(
            new uint[]
            {
                (uint)_planeMesh.GetIndexCount(0),
                0,
                (uint)_planeMesh.GetIndexStart(0),
                (uint)_planeMesh.GetBaseVertex(0),
                0,
            }
        );
        _extraGroups.Add(g);
    }

    // ── Mesh data helpers ─────────────────────────────────────────────────────
    private (Vector3[] verts, int[] tris, Vector3[] normals, Vector2[] uvs) GetMeshArrays(Mesh mesh)
    {
        var verts = mesh.vertices;
        var tris = mesh.triangles;
        var normals =
            mesh.normals.Length == verts.Length
                ? mesh.normals
                : Enumerable.Repeat(Vector3.up, verts.Length).ToArray();
        var uvList = new List<Vector2>();
        mesh.GetUVs(0, uvList);
        var uvs = uvList.Count == verts.Length ? uvList.ToArray() : new Vector2[verts.Length];
        return (verts, tris, normals, uvs);
    }

    // Normalised cumulative area distribution over mesh triangles.
    private (float[] cdf, float totalArea) BuildAreaCDF(Vector3[] verts, int[] tris)
    {
        int triCount = tris.Length / 3;
        var areas = new float[triCount];
        float totalArea = 0f;
        for (int ti = 0; ti < triCount; ti++)
        {
            Vector3 w0 = transform.TransformPoint(verts[tris[ti * 3]]);
            Vector3 w1 = transform.TransformPoint(verts[tris[ti * 3 + 1]]);
            Vector3 w2 = transform.TransformPoint(verts[tris[ti * 3 + 2]]);
            areas[ti] = Vector3.Cross(w1 - w0, w2 - w0).magnitude * 0.5f;
            totalArea += areas[ti];
        }
        var cdf = new float[triCount];
        float running = 0f;
        float invTotal = totalArea > 0f ? 1f / totalArea : 0f;
        for (int ti = 0; ti < triCount; ti++)
        {
            running += areas[ti];
            cdf[ti] = running * invTotal;
        }
        return (cdf, totalArea);
    }

    // Returns a uniformly random world-space point on a triangle chosen by area-weighted CDF.
    private void SampleTriangle(
        System.Random rng,
        float[] cdf,
        int triCount,
        int[] tris,
        Vector3[] verts,
        Vector3[] normals,
        Vector2[] uvs,
        out Vector3 pos,
        out Vector3 normal,
        out Vector2 uv
    )
    {
        float r = (float)rng.NextDouble();
        int lo = 0,
            hi = triCount - 1;
        while (lo < hi)
        {
            int mid = (lo + hi) >> 1;
            if (cdf[mid] < r)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid;
            }
        }

        int i0 = tris[lo * 3],
            i1 = tris[lo * 3 + 1],
            i2 = tris[lo * 3 + 2];
        float r1 = Mathf.Sqrt((float)rng.NextDouble()),
            r2 = (float)rng.NextDouble();
        float u = 1f - r1,
            v = r1 * (1f - r2),
            w = r1 * r2;

        pos = transform.TransformPoint(u * verts[i0] + v * verts[i1] + w * verts[i2]);
        normal = transform
            .TransformDirection(u * normals[i0] + v * normals[i1] + w * normals[i2])
            .normalized;
        uv = u * uvs[i0] + v * uvs[i1] + w * uvs[i2];
    }

    // ── Per-frame GPU work ────────────────────────────────────────────────────
    private void DrawGroup(
        ComputeBuffer all,
        ComputeBuffer visible,
        ComputeBuffer argsBuffer,
        int totalCount,
        Mesh mesh,
        Material mat,
        Camera cam
    )
    {
        if (disableCulling)
        {
            // Render directly from the full blade buffer; write count into args slot 1.
            mat.SetBuffer("_VisibleBlades", all);
            argsBuffer.SetData(
                new uint[]
                {
                    (uint)mesh.GetIndexCount(0),
                    (uint)totalCount,
                    (uint)mesh.GetIndexStart(0),
                    (uint)mesh.GetBaseVertex(0),
                    0,
                }
            );
        }
        else
        {
            visible.SetCounterValue(0);
            DispatchCull(all, visible, totalCount, cam);
            // Slots 0,2,3,4 are constant; CopyCount writes the visible count into slot 1.
            _args[0] = (uint)mesh.GetIndexCount(0);
            _args[1] = 0;
            _args[2] = (uint)mesh.GetIndexStart(0);
            _args[3] = (uint)mesh.GetBaseVertex(0);
            _args[4] = 0;
            argsBuffer.SetData(_args);
            ComputeBuffer.CopyCount(visible, argsBuffer, sizeof(uint));
            mat.SetBuffer("_VisibleBlades", visible);
        }

        mat.SetVector("_CameraPosition", cam.transform.position);
        mat.SetFloat("_MaxDistance", maxDistance);
        mat.SetFloat("_FadeStartDistance", fadeStartDistance);
        Graphics.DrawMeshInstancedIndirect(
            mesh,
            0,
            mat,
            _drawBounds,
            argsBuffer,
            0,
            null,
            shadowCasting,
            true
        );
    }

    private void DispatchCull(ComputeBuffer all, ComputeBuffer visible, int totalCount, Camera cam)
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);
        var planeV4 = new Vector4[6];
        for (int i = 0; i < 6; i++)
        {
            planeV4[i] = new Vector4(
                planes[i].normal.x,
                planes[i].normal.y,
                planes[i].normal.z,
                planes[i].distance
            );
        }

        computeShader.SetInt("_TotalBladeCount", totalCount);
        computeShader.SetVectorArray("_FrustumPlanes", planeV4);
        computeShader.SetVector("_CameraPos", cam.transform.position);
        computeShader.SetFloat("_MaxDistance", maxDistance);
        computeShader.SetBuffer(_cullKernel, "_AllBlades", all);
        computeShader.SetBuffer(_cullKernel, "_VisibleBlades", visible);
        computeShader.Dispatch(_cullKernel, Mathf.CeilToInt(totalCount / 64f), 1, 1);
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────
    private void ReleaseBuffers()
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

    private void DestroyMesh(ref Mesh mesh)
    {
        if (mesh == null)
        {
            return;
        }
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyImmediate(mesh);
        }
        else
#endif
            Destroy(mesh);
        mesh = null;
    }

    // ── Editor ────────────────────────────────────────────────────────────────
#if UNITY_EDITOR
    [ContextMenu("Regenerate Grass")]
    public void RegenerateGrass() => Init();

    private void OnValidate()
    {
        minHeight = Mathf.Min(minHeight, maxHeight);
        minWidth = Mathf.Min(minWidth, maxWidth);
        maxDistance = Mathf.Max(0f, maxDistance);
        fadeStartDistance = Mathf.Clamp(fadeStartDistance, 0f, maxDistance);
        unmaskedExtraDensity = Mathf.Clamp01(unmaskedExtraDensity);
        maskedExtraDensity = Mathf.Clamp01(maskedExtraDensity);
        maskedExtraThreshold = Mathf.Clamp01(maskedExtraThreshold);
        unmaskedExtraSize = Vector2.Max(unmaskedExtraSize, Vector2.zero);
        maskedExtraSize = Vector2.Max(maskedExtraSize, Vector2.zero);
    }
#endif
}
