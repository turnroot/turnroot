using System.Collections.Generic;
using UnityEditor;

namespace Turnroot.Characters.CharacterClass
{
    [CustomEditor(typeof(CharacterClassData))]
    public class CharacterClassEditor : NaughtyAttributes.Editor.NaughtyInspector
    {
        private SerializedProperty allowedPronounKeysProp;

        protected override void OnEnable()
        {
            base.OnEnable();
            allowedPronounKeysProp = serializedObject.FindProperty("allowedPronounKeys");
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

            serializedObject.ApplyModifiedProperties();
        }
    }
}
