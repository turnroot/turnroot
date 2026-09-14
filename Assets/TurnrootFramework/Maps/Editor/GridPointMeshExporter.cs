#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Turnroot.Gameplay.Maps
{
    /// <summary>
    /// Exports map grid points as 3D mesh models with vertex colors.
    /// Supports cube or icosphere geometry and FBX/OBJ export formats.
    /// </summary>
    public static class GridPointMeshExporter
    {
        public enum MeshType
        {
            Cube,
            Icosphere,
        }

        public enum ExportFormat
        {
            OBJ,
            FBX,
            UnityMesh, // Creates temporary GameObjects in the scene for manual export
        }

        /// <summary>
        /// Exports all grid points from a MapGrid as 3D models with vertex colors.
        /// </summary>
        public static void ExportGridPoints(
            MapGrid grid,
            MeshType meshType,
            ExportFormat format,
            float meshScale = 0.8f,
            string fileName = null
        )
        {
            if (grid == null)
            {
                EditorUtility.DisplayDialog("Error", "No MapGrid selected.", "OK");
                return;
            }

            if (fileName == null)
            {
                fileName = $"{grid.MapName}_GridPoints_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
            }

            try
            {
                switch (format)
                {
                    case ExportFormat.OBJ:
                        ExportAsOBJ(grid, meshType, meshScale, fileName);
                        break;
                    case ExportFormat.FBX:
                        ExportAsTemporaryGameObjects(grid, meshType, meshScale, fileName);
                        EditorUtility.DisplayDialog(
                            "FBX Export",
                            "Temporary GameObjects created in the scene.\n\n"
                                + "To export as FBX:\n"
                                + "1. Select all objects in '"
                                + fileName
                                + "' parent\n"
                                + "2. Drag to your Assets folder\n"
                                + "3. Or use File > Export Model",
                            "OK"
                        );
                        break;
                    case ExportFormat.UnityMesh:
                        ExportAsTemporaryGameObjects(grid, meshType, meshScale, fileName);
                        EditorUtility.DisplayDialog(
                            "Grid Points Exported",
                            $"Created {fileName} container with mesh objects.\n\n"
                                + "These can be:\n"
                                + "- Exported to FBX via Blender\n"
                                + "- Manually exported via Unity's export tools\n"
                                + "- Kept in the scene for visualization",
                            "OK"
                        );
                        break;
                }
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog(
                    "Export Error",
                    $"Failed to export grid points:\n{ex.Message}",
                    "OK"
                );
                Debug.LogException(ex);
            }
        }

        private static void ExportAsOBJ(
            MapGrid grid,
            MeshType meshType,
            float meshScale,
            string fileName
        )
        {
            var meshData = GenerateCombinedMesh(grid, meshType, meshScale);

            if (meshData.mesh.vertexCount == 0)
            {
                EditorUtility.DisplayDialog("Error", "No grid points found to export.", "OK");
                return;
            }

            string exportPath = EditorUtility.SaveFilePanel(
                "Export Grid Points as OBJ",
                Application.persistentDataPath,
                fileName,
                "obj"
            );

            if (string.IsNullOrEmpty(exportPath))
                return;

            ExportMeshToOBJ(meshData.mesh, exportPath);

            EditorUtility.DisplayDialog(
                "Export Successful",
                $"Grid points exported to:\n{exportPath}\n\n"
                    + $"Vertices: {meshData.mesh.vertexCount}\n"
                    + $"Triangles: {meshData.mesh.triangles.Length / 3}",
                "OK"
            );

            // Open the file location
            EditorUtility.RevealInFinder(exportPath);
        }

        private static void ExportAsTemporaryGameObjects(
            MapGrid grid,
            MeshType meshType,
            float meshScale,
            string containerName
        )
        {
            // Create a parent container
            var containerGO = new GameObject(containerName);
            containerGO.transform.SetParent(grid.transform.parent);

            try
            {
                var gridPoints = GetAllGridPoints(grid);

                if (gridPoints.Count == 0)
                {
                    EditorUtility.DisplayDialog("Error", "No grid points found to export.", "OK");
                    return;
                }

                int count = 0;
                foreach (var point in gridPoints)
                {
                    if (point == null)
                        continue;

                    var mesh = CreateMesh(meshType, meshScale);
                    var color = GetPointColor(point);
                    ApplyVertexColors(mesh, color);

                    var meshObj = new GameObject($"Point_R{point.Row}_C{point.Col}");
                    meshObj.transform.SetParent(containerGO.transform);
                    meshObj.transform.position = point.gameObject.transform.position;

                    var meshFilter = meshObj.AddComponent<MeshFilter>();
                    meshFilter.mesh = mesh;

                    var meshRenderer = meshObj.AddComponent<MeshRenderer>();
                    meshRenderer.material = CreateVertexColorMaterial();

                    // Add collider for reference
                    meshObj.AddComponent<MeshCollider>();

                    count++;
                }

                Debug.Log($"Created {count} mesh objects in '{containerName}' for grid points.");
            }
            catch (Exception)
            {
                if (containerGO != null)
                {
                    UnityEngine.Object.DestroyImmediate(containerGO);
                }
                throw;
            }
        }

        private static (Mesh mesh, Color[] colors) GenerateCombinedMesh(
            MapGrid grid,
            MeshType meshType,
            float meshScale
        )
        {
            var gridPoints = GetAllGridPoints(grid);
            var combines = new List<CombineInstance>();
            var colors = new List<Color>();

            foreach (var point in gridPoints)
            {
                if (point == null)
                    continue;

                var mesh = CreateMesh(meshType, meshScale);
                var color = GetPointColor(point);
                ApplyVertexColors(mesh, color);

                var combine = new CombineInstance
                {
                    mesh = mesh,
                    transform = Matrix4x4.Translate(point.gameObject.transform.position),
                };
                combines.Add(combine);

                // Store color for each vertex in this mesh
                for (int i = 0; i < mesh.vertexCount; i++)
                {
                    colors.Add(color);
                }
            }

            var combinedMesh = new Mesh { name = "GridPointsMesh" };
            combinedMesh.CombineMeshes(combines.ToArray(), true, true);

            // Reapply vertex colors to combined mesh
            if (colors.Count == combinedMesh.vertexCount)
            {
                combinedMesh.colors = colors.ToArray();
            }

            return (combinedMesh, colors.ToArray());
        }

        private static List<MapGridPoint> GetAllGridPoints(MapGrid grid)
        {
            var points = new List<MapGridPoint>();

            for (int row = 0; row < grid.GridWidth; row++)
            {
                for (int col = 0; col < grid.GridHeight; col++)
                {
                    var point = grid.GetGridPoint(row, col);
                    if (point != null)
                    {
                        points.Add(point);
                    }
                }
            }

            return points;
        }

        private static Mesh CreateMesh(MeshType type, float scale)
        {
            return type switch
            {
                MeshType.Cube => CreateCube(scale),
                MeshType.Icosphere => CreateIcosphere(scale, 2),
                _ => CreateCube(scale),
            };
        }

        private static Mesh CreateCube(float scale)
        {
            var mesh = new Mesh { name = "GridPointCube" };

            float s = scale * 0.5f;
            var vertices = new Vector3[]
            {
                new(-s, -s, -s), // 0
                new(s, -s, -s), // 1
                new(s, s, -s), // 2
                new(-s, s, -s), // 3
                new(-s, -s, s), // 4
                new(s, -s, s), // 5
                new(s, s, s), // 6
                new(-s, s, s), // 7
            };

            var triangles = new int[]
            {
                // Front
                0,
                2,
                1,
                0,
                3,
                2,
                // Back
                4,
                5,
                6,
                4,
                6,
                7,
                // Left
                4,
                7,
                3,
                4,
                3,
                0,
                // Right
                1,
                2,
                6,
                1,
                6,
                5,
                // Bottom
                4,
                0,
                1,
                4,
                1,
                5,
                // Top
                3,
                7,
                6,
                3,
                6,
                2,
            };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        private static Mesh CreateIcosphere(float scale, int subdivisions)
        {
            var mesh = new Mesh { name = "GridPointIcosphere" };

            // Golden ratio
            float t = (1f + Mathf.Sqrt(5f)) / 2f;
            float s = scale / Mathf.Sqrt(1f + t * t);
            float ts = t * s;

            var vertices = new List<Vector3>
            {
                new(-s, ts, 0), // 0
                new(s, ts, 0), // 1
                new(-s, -ts, 0), // 2
                new(s, -ts, 0), // 3
                new(0, -s, ts), // 4
                new(0, s, ts), // 5
                new(0, -s, -ts), // 6
                new(0, s, -ts), // 7
                new(ts, 0, -s), // 8
                new(ts, 0, s), // 9
                new(-ts, 0, -s), // 10
                new(-ts, 0, s), // 11
            };

            var triangles = new List<int>
            {
                0,
                11,
                5,
                0,
                5,
                1,
                0,
                1,
                7,
                0,
                7,
                10,
                0,
                10,
                11,
                1,
                5,
                9,
                5,
                11,
                4,
                11,
                10,
                2,
                10,
                7,
                6,
                7,
                1,
                8,
                3,
                9,
                4,
                3,
                4,
                2,
                3,
                2,
                6,
                3,
                6,
                8,
                3,
                8,
                9,
                4,
                9,
                5,
                2,
                4,
                11,
                6,
                2,
                10,
                8,
                6,
                7,
                9,
                8,
                1,
            };

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        private static Color GetPointColor(MapGridPoint point)
        {
            var terrainType = point.GetCachedTerrainType();
            return terrainType?.EditorColor ?? Color.yellow;
        }

        private static void ApplyVertexColors(Mesh mesh, Color color)
        {
            var colors = new Color[mesh.vertexCount];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = color;
            }
            mesh.colors = colors;
        }

        private static Material CreateVertexColorMaterial()
        {
            // Use a standard shader that supports vertex colors
            var shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }
            return new Material(shader);
        }

        private static void ExportMeshToOBJ(Mesh mesh, string path)
        {
            using (var writer = new StreamWriter(path))
            {
                writer.WriteLine("# Grid Points Mesh");
                writer.WriteLine($"# Vertices: {mesh.vertexCount}");
                writer.WriteLine($"# Triangles: {mesh.triangles.Length / 3}");
                writer.WriteLine();

                var vertices = mesh.vertices;
                var normals = mesh.normals;
                var triangles = mesh.triangles;
                var colors = mesh.colors;

                // Write vertices with colors (OBJ doesn't officially support vertex colors,
                // but we can use the Wavefront extension format)
                for (int i = 0; i < vertices.Length; i++)
                {
                    var v = vertices[i];
                    var c = i < colors.Length ? colors[i] : Color.white;
                    writer.WriteLine($"v {v.x:F6} {v.y:F6} {v.z:F6} {c.r:F6} {c.g:F6} {c.b:F6}");
                }

                writer.WriteLine();

                // Write normals
                for (int i = 0; i < normals.Length; i++)
                {
                    var n = normals[i];
                    writer.WriteLine($"vn {n.x:F6} {n.y:F6} {n.z:F6}");
                }

                writer.WriteLine();

                // Write triangles
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int v0 = triangles[i] + 1;
                    int v1 = triangles[i + 1] + 1;
                    int v2 = triangles[i + 2] + 1;
                    writer.WriteLine($"f {v0}//{v0} {v1}//{v1} {v2}//{v2}");
                }
            }
        }
    }
}
#endif
