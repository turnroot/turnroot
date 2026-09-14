#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Turnroot.Gameplay.Maps
{
    /// <summary>
    /// Custom inspector for MapGrid with grid manipulation tools.
    /// </summary>
    [CustomEditor(typeof(MapGrid))]
    public class MapGridInspector : UnityEditor.Editor
    {
        private string _filterRow = string.Empty;
        private string _filterCol = string.Empty;

        // Export settings
        private GridPointMeshExporter.MeshType _exportMeshType = GridPointMeshExporter
            .MeshType
            .Cube;
        private GridPointMeshExporter.ExportFormat _exportFormat = GridPointMeshExporter
            .ExportFormat
            .UnityMesh;
        private float _exportMeshScale = 0.8f;
        private bool _showExportOptions = false;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw all serialized properties except the two raycast arrays.
            // Use a SerializedProperty iterator to ensure auto-property backing fields
            // (e.g. properties with [field: SerializeField]) are correctly displayed.
            var prop = serializedObject.GetIterator();
            bool enterChildren = true;
            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (
                    prop.name
                    is "_single3dHeightMeshRaycastPoints"
                        or "_single3dHeightMeshRaycastIndices"
                )
                {
                    continue;
                }

                EditorGUILayout.PropertyField(prop, true);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Filter Point", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical();
            _filterRow = EditorGUILayout.TextField("Row", _filterRow, new GUILayoutOption[] { });
            _filterCol = EditorGUILayout.TextField("Col", _filterCol, new GUILayoutOption[] { });
            EditorGUILayout.EndVertical();

            var mg = target as MapGrid;
            Vector3[] points = null;
            Vector2Int[] indices = null;
            if (mg != null)
            {
                var t = mg.GetType();
                var fiPoints = t.GetField(
                    "_single3dHeightMeshRaycastPoints",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                );
                var fiIndices = t.GetField(
                    "_single3dHeightMeshRaycastIndices",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                );
                points = fiPoints?.GetValue(mg) as Vector3[];
                indices = fiIndices?.GetValue(mg) as Vector2Int[];
            }

            bool hasFilter = !string.IsNullOrEmpty(_filterRow) || !string.IsNullOrEmpty(_filterCol);
            if (hasFilter && indices != null && points != null)
            {
                int parsedRow;
                int parsedCol;
                bool haveRow = int.TryParse(_filterRow, out parsedRow);
                bool haveCol = int.TryParse(_filterCol, out parsedCol);

                int found = -1;
                for (int i = 0; i < indices.Length; i++)
                {
                    bool rowMatches = !haveRow || indices[i].x == parsedRow;
                    bool colMatches = !haveCol || indices[i].y == parsedCol;
                    if (rowMatches && colMatches)
                    {
                        found = i;
                        break;
                    }
                }

                if (found >= 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Matched Raycast Point", EditorStyles.helpBox);

                    EditorGUILayout.LabelField("Element Number", found.ToString());
                    if (indices != null && found >= 0 && found < indices.Length)
                    {
                        var idx = indices[found];
                        EditorGUILayout.LabelField("Index (row,col)", $"{idx.x},{idx.y}");
                        string pointName = "(unknown)";
                        if (mg != null)
                        {
                            var mgp = mg.GetGridPoint(idx.x, idx.y);
                            if (mgp != null && mgp.gameObject != null)
                            {
                                pointName = mgp.gameObject.name;
                            }
                            else
                            {
                                pointName = $"Point_R{idx.x}_C{idx.y}";
                            }
                        }
                        EditorGUILayout.LabelField("Point Name", pointName);
                    }

                    var pointsPropEditable = serializedObject.FindProperty(
                        "_single3dHeightMeshRaycastPoints"
                    );
                    if (
                        pointsPropEditable != null
                        && found >= 0
                        && found < pointsPropEditable.arraySize
                    )
                    {
                        var elem = pointsPropEditable.GetArrayElementAtIndex(found);
                        EditorGUI.BeginChangeCheck();
                        Vector3 newVal = EditorGUILayout.Vector3Field("Point", elem.vector3Value);
                        if (EditorGUI.EndChangeCheck())
                        {
                            if (mg != null)
                            {
                                Undo.RecordObject(mg, "Edit Raycast Point");
                            }

                            elem.vector3Value = newVal;
                            serializedObject.ApplyModifiedProperties();
                            if (mg != null)
                            {
                                EditorUtility.SetDirty(mg);
                            }
                        }
                    }
                    else
                    {
                        EditorGUI.BeginChangeCheck();
                        Vector3 newVal = EditorGUILayout.Vector3Field("Point", points[found]);
                        if (EditorGUI.EndChangeCheck() && mg != null)
                        {
                            Undo.RecordObject(mg, "Edit Raycast Point");
                            points[found] = newVal;
                            var fiPoints = mg.GetType()
                                .GetField(
                                    "_single3dHeightMeshRaycastPoints",
                                    BindingFlags.Instance
                                        | BindingFlags.NonPublic
                                        | BindingFlags.Public
                                );
                            fiPoints?.SetValue(mg, points);
                            EditorUtility.SetDirty(mg);
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "No matching raycast point/index found.",
                        MessageType.Info
                    );
                }
            }
            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Create Grid Points"))
            {
                if (mg != null)
                {
                    Undo.RecordObject(mg, "Create Grid Points");
                    mg.CreateChildrenPoints();
                    EditorUtility.SetDirty(mg);
                }
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Row"))
            {
                if (mg != null)
                {
                    Undo.RecordObject(mg, "Add Row");
                    mg.AddRow();
                    EditorUtility.SetDirty(mg);
                }
            }

            if (GUILayout.Button("Add Column"))
            {
                if (mg != null)
                {
                    Undo.RecordObject(mg, "Add Column");
                    mg.AddColumn();
                    EditorUtility.SetDirty(mg);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Remove Row"))
            {
                if (mg != null)
                {
                    Undo.RecordObject(mg, "Remove Row");
                    mg.RemoveRow();
                    EditorUtility.SetDirty(mg);
                }
            }

            if (GUILayout.Button("Remove Column"))
            {
                if (mg != null)
                {
                    Undo.RecordObject(mg, "Remove Column");
                    mg.RemoveColumn();
                    EditorUtility.SetDirty(mg);
                }
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Connect to 3D Map Height"))
            {
                if (mg != null)
                {
                    Undo.RecordObject(mg, "Connect to 3D Map Height");
                    mg.ConnectTo3DMapObject();
                    EditorUtility.SetDirty(mg);
                }
            }

            if (GUILayout.Button("Remove Height Connection"))
            {
                if (mg != null)
                {
                    Undo.RecordObject(mg, "Remove Height Connection");
                    mg.RemoveHeightConnection();
                    EditorUtility.SetDirty(mg);
                }
            }

            // Render Map Images button (added so this appears in the custom inspector)
            if (GUILayout.Button("Render Map Images"))
            {
                if (mg != null)
                {
                    Undo.RecordObject(mg, "Render Map Images");
                    mg.RenderMapImages();
                    EditorUtility.SetDirty(mg);
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Export Grid Points", EditorStyles.boldLabel);

            _showExportOptions = EditorGUILayout.Foldout(_showExportOptions, "Export Settings");
            if (_showExportOptions)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                _exportMeshType = (GridPointMeshExporter.MeshType)
                    EditorGUILayout.EnumPopup("Mesh Shape", _exportMeshType);

                _exportFormat = (GridPointMeshExporter.ExportFormat)
                    EditorGUILayout.EnumPopup("Export Format", _exportFormat);

                _exportMeshScale = EditorGUILayout.Slider("Mesh Scale", _exportMeshScale, 0.1f, 5f);

                EditorGUILayout.HelpBox(
                    _exportFormat switch
                    {
                        GridPointMeshExporter.ExportFormat.OBJ =>
                            "Exports to OBJ format with vertex colors. Colors stored in extended OBJ format.",
                        GridPointMeshExporter.ExportFormat.FBX =>
                            "Creates temporary GameObjects in the scene that can be exported to FBX via File > Export Model or dragged to Assets folder.",
                        GridPointMeshExporter.ExportFormat.UnityMesh =>
                            "Creates temporary GameObjects in the scene for visualization and reference in Blender import workflow.",
                        _ => "Select an export format",
                    },
                    MessageType.Info
                );

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Export Grid Points as 3D Model", GUILayout.Height(30)))
            {
                if (mg != null)
                {
                    GridPointMeshExporter.ExportGridPoints(
                        mg,
                        _exportMeshType,
                        _exportFormat,
                        _exportMeshScale
                    );
                }
            }
        }
    }
}
#endif
