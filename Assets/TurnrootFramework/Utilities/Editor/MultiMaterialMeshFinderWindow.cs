using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Turnroot.Utilities.Editor
{
    /// <summary>
    /// Tools -> Turnroot -> Multi-Material Mesh Finder
    /// Scans project assets for meshes that use 2 or more materials.
    /// </summary>
    public class MultiMaterialMeshFinderWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private List<MeshAssetInfo> _meshesWithMultipleMaterials = new List<MeshAssetInfo>();
        private bool _isScanning;
        private GUIStyle _headerStyle;
        private GUIStyle _itemStyle;
        private bool _stylesInitialized;

        [MenuItem("Tools/Turnroot/Multi-Material Mesh Finder")]
        public static void ShowWindow()
        {
            var window = GetWindow<MultiMaterialMeshFinderWindow>("Multi-Material Mesh Finder");
            window.minSize = new Vector2(400f, 300f);
        }

        private void InitStyles()
        {
            if (_stylesInitialized)
                return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                margin = new RectOffset(0, 0, 10, 5)
            };

            _itemStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true
            };

            _stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitStyles();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Multi-Material Mesh Finder", _headerStyle);
            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "This tool scans all mesh assets in your project and lists any that use 2 or more materials.",
                MessageType.Info
            );

            EditorGUILayout.Space(10);

            // Scan button
            GUI.enabled = !_isScanning;
            if (GUILayout.Button(_isScanning ? "Scanning..." : "Scan Project", GUILayout.Height(30)))
            {
                ScanProject();
            }
            GUI.enabled = true;

            EditorGUILayout.Space(10);

            // Results
            if (_meshesWithMultipleMaterials.Count > 0)
            {
                EditorGUILayout.LabelField(
                    $"Found {_meshesWithMultipleMaterials.Count} mesh(es) with multiple materials:",
                    EditorStyles.boldLabel
                );

                EditorGUILayout.Space(5);

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

                foreach (var meshInfo in _meshesWithMultipleMaterials)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    EditorGUILayout.BeginHorizontal();

                    // Asset name as clickable label
                    if (GUILayout.Button(meshInfo.Name, EditorStyles.linkLabel))
                    {
                        EditorGUIUtility.PingObject(meshInfo.Asset);
                        Selection.activeObject = meshInfo.Asset;
                    }

                    GUILayout.FlexibleSpace();

                    // Material count badge
                    var oldColor = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(1f, 0.7f, 0.3f);
                    GUILayout.Label(
                        $"{meshInfo.MaterialCount} materials",
                        EditorStyles.helpBox,
                        GUILayout.Width(100)
                    );
                    GUI.backgroundColor = oldColor;

                    EditorGUILayout.EndHorizontal();

                    // Path
                    EditorGUILayout.LabelField(meshInfo.Path, EditorStyles.miniLabel);

                    // Submesh details
                    if (meshInfo.SubmeshCount > 1)
                    {
                        EditorGUILayout.LabelField(
                            $"Submeshes: {meshInfo.SubmeshCount}",
                            EditorStyles.miniLabel
                        );
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }

                EditorGUILayout.EndScrollView();
            }
            else if (!_isScanning)
            {
                EditorGUILayout.HelpBox("Click 'Scan Project' to search for meshes with multiple materials.", MessageType.None);
            }
        }

        private void ScanProject()
        {
            _isScanning = true;
            _meshesWithMultipleMaterials.Clear();

            try
            {
                // Find all mesh assets
                string[] allAssetGuids = AssetDatabase.FindAssets("t:Mesh");
                int totalAssets = allAssetGuids.Length;

                for (int i = 0; i < totalAssets; i++)
                {
                    string guid = allAssetGuids[i];
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                    // Update progress bar
                    EditorUtility.DisplayProgressBar(
                        "Scanning Meshes",
                        $"Checking {assetPath}",
                        (float)i / totalAssets
                    );

                    // Load the mesh
                    Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
                    if (mesh != null)
                    {
                        int submeshCount = mesh.subMeshCount;

                        // Check if mesh has 2 or more submeshes (which typically means 2+ materials)
                        if (submeshCount >= 2)
                        {
                            _meshesWithMultipleMaterials.Add(new MeshAssetInfo
                            {
                                Name = mesh.name,
                                Path = assetPath,
                                Asset = mesh,
                                MaterialCount = submeshCount,
                                SubmeshCount = submeshCount
                            });
                        }
                    }
                }

                // Also scan prefabs and GameObjects for MeshRenderer/SkinnedMeshRenderer
                ScanPrefabsForMultiMaterialMeshes();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                _isScanning = false;
            }

            // Sort by material count (descending)
            _meshesWithMultipleMaterials = _meshesWithMultipleMaterials
                .OrderByDescending(m => m.MaterialCount)
                .ThenBy(m => m.Name)
                .ToList();

            Repaint();
        }

        private void ScanPrefabsForMultiMaterialMeshes()
        {
            // Find all prefabs
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            int totalPrefabs = prefabGuids.Length;

            for (int i = 0; i < totalPrefabs; i++)
            {
                string guid = prefabGuids[i];
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                EditorUtility.DisplayProgressBar(
                    "Scanning Prefabs",
                    $"Checking {assetPath}",
                    (float)i / totalPrefabs
                );

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab != null)
                {
                    // Check MeshRenderer components
                    MeshRenderer[] meshRenderers = prefab.GetComponentsInChildren<MeshRenderer>(true);
                    foreach (var renderer in meshRenderers)
                    {
                        if (renderer.sharedMaterials.Length >= 2)
                        {
                            var existingEntry = _meshesWithMultipleMaterials.FirstOrDefault(
                                m => m.Asset == renderer.gameObject
                            );

                            if (existingEntry == null)
                            {
                                _meshesWithMultipleMaterials.Add(new MeshAssetInfo
                                {
                                    Name = $"{prefab.name}/{renderer.gameObject.name}",
                                    Path = assetPath,
                                    Asset = renderer.gameObject,
                                    MaterialCount = renderer.sharedMaterials.Length,
                                    SubmeshCount = renderer.sharedMaterials.Length
                                });
                            }
                        }
                    }

                    // Check SkinnedMeshRenderer components
                    SkinnedMeshRenderer[] skinnedRenderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                    foreach (var renderer in skinnedRenderers)
                    {
                        if (renderer.sharedMaterials.Length >= 2)
                        {
                            var existingEntry = _meshesWithMultipleMaterials.FirstOrDefault(
                                m => m.Asset == renderer.gameObject
                            );

                            if (existingEntry == null)
                            {
                                _meshesWithMultipleMaterials.Add(new MeshAssetInfo
                                {
                                    Name = $"{prefab.name}/{renderer.gameObject.name}",
                                    Path = assetPath,
                                    Asset = renderer.gameObject,
                                    MaterialCount = renderer.sharedMaterials.Length,
                                    SubmeshCount = renderer.sharedMaterials.Length
                                });
                            }
                        }
                    }
                }
            }
        }

        private class MeshAssetInfo
        {
            public string Name;
            public string Path;
            public Object Asset;
            public int MaterialCount;
            public int SubmeshCount;
        }
    }
}
