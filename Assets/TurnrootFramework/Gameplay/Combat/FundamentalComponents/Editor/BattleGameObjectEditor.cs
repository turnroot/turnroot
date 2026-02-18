using System;
using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Gameplay.Roster;
using UnityEditor;
using UnityEngine;

namespace Turnroot.Gameplay.Combat
{
    /// <summary>
    /// Custom editor for BattleGameObject with UI for managing battle conditions and required player units.
    /// </summary>
    [CustomEditor(typeof(BattleGameObject))]
    public class BattleGameObjectEditor : UnityEditor.Editor
    {
        private SerializedProperty _battleConditionsProp;
        private Dictionary<string, bool> _eventsFoldouts = new Dictionary<string, bool>();

        private SerializedProperty _requiredPlayerUnitsProp;

        private void OnEnable()
        {
            _battleConditionsProp = serializedObject.FindProperty("_battle_conditions");
            // try both common names to be robust in case of reformatting
            _battleConditionsProp ??= serializedObject.FindProperty("_battleConditions");
            _requiredPlayerUnitsProp = serializedObject.FindProperty("_requiredPlayerUnits");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw everything except the _battleConditions and _requiredPlayerUnits fields so we can provide a custom UI for them
            DrawPropertiesExcluding(serializedObject, "_battleConditions", "_requiredPlayerUnits");

            // --- Required Player Units custom UI ---
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Required Player Units", EditorStyles.boldLabel);

            // Try resolve gamewide persistent player roster characters
            var persistent = PersistentPlayerRoster.Instance;
            var rosterAsset = persistent?.PlayerRoster;
            var characterOptions = new CharacterData[0];
            var characterOptionNames = new string[0];

            if (rosterAsset != null)
            {
                var placements =
                    rosterAsset.characters ?? System.Array.Empty<Characters.Roster.UnitPlacement>();
                var chars = new List<CharacterData>();
                foreach (var p in placements)
                {
                    if (p?.CharacterData != null)
                    {
                        chars.Add(p.CharacterData);
                    }
                }
                characterOptions = chars.ToArray();
                characterOptionNames = characterOptions
                    .Select(c => c?.DisplayName ?? "(unknown)")
                    .ToArray();
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "No Gamewide persistent player roster found. Assign a PersistentPlayerRoster asset or add characters manually.",
                    MessageType.Warning
                );
            }

            if (_requiredPlayerUnitsProp != null)
            {
                // Ensure array size control
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add"))
                {
                    _requiredPlayerUnitsProp.arraySize++;
                    _requiredPlayerUnitsProp
                        .GetArrayElementAtIndex(_requiredPlayerUnitsProp.arraySize - 1)
                        .objectReferenceValue = null;
                }

                if (GUILayout.Button("Clear"))
                {
                    _requiredPlayerUnitsProp.ClearArray();
                }
                EditorGUILayout.EndHorizontal();

                // Draw each element as a popup sourced from roster characters (or object field fallback)
                for (int i = 0; i < _requiredPlayerUnitsProp.arraySize; i++)
                {
                    var elem = _requiredPlayerUnitsProp.GetArrayElementAtIndex(i);
                    EditorGUILayout.BeginHorizontal(GUI.skin.box);

                    var current = elem.objectReferenceValue as CharacterData;
                    int currentIndex = -1;
                    if (characterOptions.Length > 0 && current != null)
                    {
                        currentIndex = System.Array.IndexOf(characterOptions, current);
                    }

                    int selected = -1;
                    if (characterOptions.Length > 0)
                    {
                        var names = new string[characterOptionNames.Length + 1];
                        names[0] = "(None)";
                        for (int n = 0; n < characterOptionNames.Length; n++)
                        {
                            names[n + 1] = characterOptionNames[n];
                        }

                        selected = EditorGUILayout.Popup($"{i + 1}", currentIndex + 1, names) - 1;
                        if (selected != currentIndex)
                        {
                            elem.objectReferenceValue =
                                selected >= 0 ? characterOptions[selected] : null;
                        }
                    }
                    else
                    {
                        // fallback to object field when roster not available
                        elem.objectReferenceValue = (CharacterData)
                            EditorGUILayout.ObjectField(
                                $"{i + 1}",
                                elem.objectReferenceValue,
                                typeof(CharacterData),
                                false
                            );
                    }

                    if (GUILayout.Button("Remove", GUILayout.Width(80)))
                    {
                        _requiredPlayerUnitsProp.DeleteArrayElementAtIndex(i);
                        EditorGUILayout.EndHorizontal();
                        break; // stop to avoid enumerator invalidation
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Required Player Units property not found (ensure field exists and is serialized).",
                    MessageType.Warning
                );
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Battle Conditions", EditorStyles.boldLabel);

            if (_battleConditionsProp != null)
            {
                for (int i = 0; i < _battleConditionsProp.arraySize; i++)
                {
                    var element = _battleConditionsProp.GetArrayElementAtIndex(i);

                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    EditorGUILayout.BeginHorizontal();

                    var managed = element.managedReferenceValue;
                    string typeName =
                        managed != null ? PrettyNameForType(managed.GetType()) : "(null)";
                    EditorGUILayout.LabelField(typeName, EditorStyles.boldLabel);

                    if (GUILayout.Button("Remove", GUILayout.Width(80)))
                    {
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        _battleConditionsProp.DeleteArrayElementAtIndex(i);
                        // break so the array is re-drawn correctly
                        break;
                    }

                    EditorGUILayout.EndHorizontal();

                    var iterator = element.Copy();
                    var endProp = iterator.GetEndProperty(true);

                    // Move to first child
                    iterator.NextVisible(true);
                    while (!SerializedProperty.EqualContents(iterator, endProp))
                    {
                        EditorGUILayout.PropertyField(iterator, true);

                        if (!iterator.NextVisible(false))
                        {
                            break;
                        }
                    }

                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add Condition"))
                {
                    GenericMenu menu = new GenericMenu();
                    var types = GetAllDerivedTypes(typeof(BattleCondition));
                    foreach (var t in types)
                    {
                        menu.AddItem(
                            new GUIContent(PrettyNameForType(t)),
                            false,
                            () => AddConditionOfType(t)
                        );
                    }
                    menu.ShowAsContext();
                }

                if (GUILayout.Button("Clear All"))
                {
                    _battleConditionsProp.ClearArray();
                }

                EditorGUILayout.EndHorizontal();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void AddConditionOfType(Type t)
        {
            serializedObject.Update();

            int index = _battleConditionsProp.arraySize;
            _battleConditionsProp.arraySize++;
            var el = _battleConditionsProp.GetArrayElementAtIndex(index);

            object instance = null;
            try
            {
                instance = Activator.CreateInstance(t);
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogError($"Failed to create instance of {t.FullName}: {ex.Message}");
#endif
            }

            el.managedReferenceValue = instance;

            serializedObject.ApplyModifiedProperties();
        }

        private static IEnumerable<Type> GetAllDerivedTypes(Type baseType)
        {
            return AppDomain
                .CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try
                    {
                        return a.GetTypes();
                    }
                    catch
                    {
                        return new Type[0];
                    }
                })
                .Where(t => baseType.IsAssignableFrom(t) && !t.IsAbstract && t != baseType)
                .OrderBy(t => t.Name);
        }

        private static string PrettyNameForType(Type t)
        {
            var name = t.Name;
            if (name.EndsWith("BattleCondition", StringComparison.Ordinal))
            {
                name = name.Substring(0, name.Length - "BattleCondition".Length);
            }

            // Insert spaces before capital letters (except start)
            var pretty = System.Text.RegularExpressions.Regex.Replace(name, "(\\B[A-Z])", " $1");
            return pretty.Trim();
        }
    }
}
