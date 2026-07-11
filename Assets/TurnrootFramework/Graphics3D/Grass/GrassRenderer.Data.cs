using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Turnroot.Graphics3D
{
    public partial class GrassRenderer
    {
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

        // ── Blade placement ───────────────────────────────────────────────────────
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

            for (
                int attempt = 0;
                blades.Count < targetCount && attempt < targetCount * 4;
                attempt++
            )
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
                        height =
                            Mathf.Lerp(minHeight, maxHeight, (float)rng.NextDouble()) * maskVal,
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

            // configure indirect args for drawing (instance count will be filled in later)
            SetArgsFromMesh(_bladeMesh);
            SetInstanceCount(0);
        }

        // ── Extra group placement ─────────────────────────────────────────────────
        private void BuildExtraGroups(Mesh mesh)
        {
            _extraGroups = new List<ExtraGroup>();

            bool hasMixins =
                grassMixinMaterials != null
                && grassMixinMaterials.Count > 0
                && grassMixinDensity > 0f;
            if (!hasMixins)
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

            bool hasGrassMask = maskTexture != null && maskTexture.isReadable;
            Predicate<Vector2> reject = hasGrassMask
                ? uv =>
                {
                    float raw = maskTexture.GetPixelBilinear(uv.x, uv.y).grayscale;
                    float maskVal = Mathf.Clamp01(
                        (raw - maskFloor) / Mathf.Max(1f - maskFloor, 0.0001f)
                    );
                    return maskVal <= 0f;
                }
                : (Predicate<Vector2>)null;

            int poolCount = Mathf.RoundToInt(
                totalArea * grassMixinDensity * grassMixinMaterials.Count
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
                grassMixinSize
            );

            var rng2 = new System.Random(98765);
            var bins = new List<BladeData>[grassMixinMaterials.Count];
            for (int i = 0; i < bins.Length; i++)
            {
                bins[i] = new List<BladeData>();
            }

            foreach (var b in pool)
            {
                bins[rng2.Next(bins.Length)].Add(b);
            }

            for (int i = 0; i < grassMixinMaterials.Count; i++)
            {
                if (grassMixinMaterials[i] != null && bins[i].Count > 0)
                {
                    CreateExtraGroup(grassMixinMaterials[i], bins[i]);
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

                float clampedMaxMixinSize = Mathf.Max(0.1f, maxGrassMixinSize);
                Vector2 clampedPlaneSize = new Vector2(
                    Mathf.Clamp(planeSize.x, 0f, clampedMaxMixinSize),
                    Mathf.Clamp(planeSize.y, 0f, clampedMaxMixinSize)
                );

                list.Add(
                    new BladeData
                    {
                        position = pos,
                        normal = normal,
                        height = clampedPlaneSize.y,
                        width = clampedPlaneSize.x,
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
                visible = new ComputeBuffer(
                    blades.Count,
                    BladeData.Stride,
                    ComputeBufferType.Append
                ),
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
        private (Vector3[] verts, int[] tris, Vector3[] normals, Vector2[] uvs) GetMeshArrays(
            Mesh mesh
        )
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
    }
}
