#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapGrid))]
public class MapGridInspector : Editor
{
    private string _filterRow = string.Empty;
    private string _filterCol = string.Empty;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw everything except the two raycast arrays so we can place our filter UI above them
        DrawPropertiesExcluding(
            serializedObject,
            "_single3dHeightMeshRaycastPoints",
            "_single3dHeightMeshRaycastIndices"
        );

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
                            pointName = mgp.gameObject.name;
                        else
                            pointName = $"Point_R{idx.x}_C{idx.y}";
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
                            Undo.RecordObject(mg, "Edit Raycast Point");
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
                                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                            );
                        fiPoints?.SetValue(mg, points);
                        EditorUtility.SetDirty(mg);
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No matching raycast point/index found.", MessageType.Info);
            }
        }
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
