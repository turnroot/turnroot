using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Turnroot.Characters.CharacterClass
{
    /// <summary>
    /// Custom editor for CharacterClassData that provides a multi-select UI for allowed pronouns.
    /// </summary>
    [CustomEditor(typeof(CharacterClassData))]
    public class CharacterClassEditor : NaughtyAttributes.Editor.NaughtyInspector
    {
        private SerializedProperty allowedPronounKeysProp;
        private SerializedProperty pronounClassModelPrefabsProp;

        protected override void OnEnable()
        {
            base.OnEnable();
            allowedPronounKeysProp = serializedObject.FindProperty("allowedPronounKeys");
            pronounClassModelPrefabsProp = serializedObject.FindProperty(
                "Identity.PronounClassModelPrefabs"
            );
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            // Add custom UI for Pronouns multi-select at the end
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Allowed Pronouns", EditorStyles.boldLabel);
            var pronounKeys = Turnroot.Characters.Subclasses.Pronouns.GetAvailablePronounKeys();
            if (pronounKeys == null || pronounKeys.Length == 0)
            {
                EditorGUILayout.HelpBox("No pronoun sets are available.", MessageType.Warning);
            }
            else
            {
                // Read current (serialized) list into a HashSet for quick lookup
                var currentSet = new HashSet<string>();
                for (var i = 0; i < allowedPronounKeysProp.arraySize; i++)
                {
                    var e = allowedPronounKeysProp.GetArrayElementAtIndex(i);
                    var val = e.stringValue;
                    if (!string.IsNullOrEmpty(val))
                    {
                        currentSet.Add(val);
                    }
                }

                EditorGUI.indentLevel++;
                foreach (var key in pronounKeys)
                {
                    var has = currentSet.Contains(key);
                    var newHas = EditorGUILayout.ToggleLeft(key, has);
                    if (newHas && !has)
                    {
                        currentSet.Add(key);
                    }
                    else if (!newHas && has)
                    {
                        currentSet.Remove(key);
                    }
                }
                EditorGUI.indentLevel--;

                // Write back set to serialized property (keep order same as pronounKeys)
                allowedPronounKeysProp.ClearArray();
                var pIdx = 0;
                foreach (var key in pronounKeys)
                {
                    if (!currentSet.Contains(key))
                    {
                        continue;
                    }

                    allowedPronounKeysProp.InsertArrayElementAtIndex(pIdx);
                    allowedPronounKeysProp.GetArrayElementAtIndex(pIdx).stringValue = key;
                    pIdx++;
                }
            }

            // Pronoun-specific class model prefabs
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Pronoun-specific Class Models", EditorStyles.boldLabel);
            if (pronounClassModelPrefabsProp == null)
            {
                EditorGUILayout.HelpBox(
                    "Pronoun model array not found on this asset.",
                    MessageType.Warning
                );
            }
            else if (pronounKeys == null || pronounKeys.Length == 0)
            {
                EditorGUILayout.HelpBox("No pronoun keys available to assign.", MessageType.Info);
            }
            else
            {
                EditorGUI.indentLevel++;
                foreach (var key in pronounKeys)
                {
                    // Find existing array element for this key (if any)
                    var foundIndex = -1;
                    for (var i = 0; i < pronounClassModelPrefabsProp.arraySize; i++)
                    {
                        var el = pronounClassModelPrefabsProp.GetArrayElementAtIndex(i);
                        var keyProp = el.FindPropertyRelative("pronounKey");
                        if (
                            string.Equals(
                                keyProp.stringValue,
                                key,
                                System.StringComparison.OrdinalIgnoreCase
                            )
                        )
                        {
                            foundIndex = i;
                            break;
                        }
                    }

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(key, GUILayout.Width(80));

                    if (foundIndex >= 0)
                    {
                        var el = pronounClassModelPrefabsProp.GetArrayElementAtIndex(foundIndex);
                        var prefabProp = el.FindPropertyRelative("prefab");
                        EditorGUILayout.PropertyField(prefabProp, GUIContent.none);

                        if (GUILayout.Button("Remove", GUILayout.Width(70)))
                        {
                            pronounClassModelPrefabsProp.DeleteArrayElementAtIndex(foundIndex);
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("(no override)", GUILayout.MaxWidth(120));
                        if (GUILayout.Button("Add", GUILayout.Width(50)))
                        {
                            var insertIdx = pronounClassModelPrefabsProp.arraySize;
                            pronounClassModelPrefabsProp.InsertArrayElementAtIndex(insertIdx);
                            var newEl = pronounClassModelPrefabsProp.GetArrayElementAtIndex(
                                insertIdx
                            );
                            newEl.FindPropertyRelative("pronounKey").stringValue = key;
                            newEl.FindPropertyRelative("prefab").objectReferenceValue = null;
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
