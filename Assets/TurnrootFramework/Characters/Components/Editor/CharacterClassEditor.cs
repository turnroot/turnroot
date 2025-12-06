using System.Collections.Generic;
using Turnroot.Characters.CharacterClass;
using Turnroot.CommonAncestors;
using Turnroot.Gameplay.Objects.Components;
using Turnroot.Utilities;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CharacterClassData))]
public class CharacterClassEditor : UnityEditor.Editor
{
    private SerializedProperty weaponProficienciesProp;
    private SerializedProperty allowedPronounKeysProp;
    private SerializedProperty cachedClassSelectionModeProp;

    private void OnEnable()
    {
        weaponProficienciesProp = serializedObject.FindProperty("weaponProficiencies");
        allowedPronounKeysProp = serializedObject.FindProperty("allowedPronounKeys");
        cachedClassSelectionModeProp = serializedObject.FindProperty("_cachedClassSelectionMode");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Determine which fields to exclude based on class selection mode
        var excludedProps = new List<string> { "weaponProficiencies", "allowedPronounKeys" };

        // Get the cached mode to determine which fields to show
        var mode = (GameplayGeneralSettings.ClassSelectionMode)
            cachedClassSelectionModeProp.enumValueIndex;

        if (mode == GameplayGeneralSettings.ClassSelectionMode.PromotionBased)
        {
            // Hide requirement-based fields
            excludedProps.Add("experienceRequirements");
            excludedProps.Add("selectionMinimumLevel");
        }
        else // RequirementBased
        {
            // Hide promotion-based fields
            excludedProps.Add("promotionPaths");
            excludedProps.Add("requiredLevelToChange");
        }

        // Draw everything except excluded properties
        DrawPropertiesExcluding(serializedObject, excludedProps.ToArray());

        // Pronouns multi-select
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
            for (int i = 0; i < allowedPronounKeysProp.arraySize; i++)
            {
                var e = allowedPronounKeysProp.GetArrayElementAtIndex(i);
                var val = e.stringValue;
                if (!string.IsNullOrEmpty(val))
                    currentSet.Add(val);
            }

            EditorGUI.indentLevel++;
            foreach (var key in pronounKeys)
            {
                bool has = currentSet.Contains(key);
                bool newHas = EditorGUILayout.ToggleLeft(key, has);
                if (newHas && !has)
                    currentSet.Add(key);
                else if (!newHas && has)
                    currentSet.Remove(key);
            }
            EditorGUI.indentLevel--;

            // Write back set to serialized property (keep order same as pronounKeys)
            allowedPronounKeysProp.ClearArray();
            int pIdx = 0;
            foreach (var key in pronounKeys)
            {
                if (!currentSet.Contains(key))
                    continue;
                allowedPronounKeysProp.InsertArrayElementAtIndex(pIdx);
                allowedPronounKeysProp.GetArrayElementAtIndex(pIdx).stringValue = key;
                pIdx++;
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Weapon Proficiencies", EditorStyles.boldLabel);

        var settings = GameSettingsLoader.LoadFirst<GameplayGeneralSettings>("GameSettings");
        WeaponType[] options = settings != null ? settings.WeaponTypes : null;
        if (options == null || options.Length == 0)
        {
            EditorGUILayout.HelpBox(
                "No Weapon Types configured in GameplayGeneralSettings — populate the Weapon Types list to edit class proficiencies.",
                MessageType.Warning
            );
            // still draw the default list for inspection
            EditorGUILayout.PropertyField(weaponProficienciesProp, true);
        }
        else
        {
            // Read current list into a dictionary for quick lookup
            var map = new Dictionary<WeaponType, string>();
            for (int i = 0; i < weaponProficienciesProp.arraySize; i++)
            {
                var element = weaponProficienciesProp.GetArrayElementAtIndex(i);
                var wtProp = element.FindPropertyRelative("weaponType");
                var rankProp = element.FindPropertyRelative("rank._value");
                var wt = wtProp.objectReferenceValue as WeaponType;
                var rank =
                    (rankProp != null && !string.IsNullOrEmpty(rankProp.stringValue))
                        ? rankProp.stringValue
                        : LeveledLetteredField.E;
                if (wt != null && !map.ContainsKey(wt))
                    map[wt] = rank;
            }

            // Show checkboxes for each configured weapon type
            EditorGUI.indentLevel++;
            foreach (var opt in options)
            {
                if (opt == null)
                    continue;
                bool has = map.ContainsKey(opt);
                bool newHas = EditorGUILayout.ToggleLeft(opt.name, has);
                if (newHas && !has)
                {
                    // add with default rank E
                    map[opt] = LeveledLetteredField.E;
                }
                else if (!newHas && has)
                {
                    map.Remove(opt);
                }

                // If present, allow editing the rank
                if (map.ContainsKey(opt))
                {
                    var current = map[opt];
                    string[] rankOptions = new[]
                    {
                        LeveledLetteredField.S,
                        LeveledLetteredField.A,
                        LeveledLetteredField.B,
                        LeveledLetteredField.C,
                        LeveledLetteredField.D,
                        LeveledLetteredField.E,
                    };
                    int currentIdx = System.Array.IndexOf(rankOptions, current);
                    if (currentIdx < 0)
                        currentIdx = rankOptions.Length - 1; // default to E
                    int newIdx = EditorGUILayout.Popup("  Rank", currentIdx, rankOptions);
                    var newRank = rankOptions[newIdx];
                    if (newRank != current)
                        map[opt] = newRank;
                }
            }
            EditorGUI.indentLevel--;

            // Write back to the serialized list
            weaponProficienciesProp.ClearArray();
            int idx = 0;
            foreach (var kv in map)
            {
                weaponProficienciesProp.InsertArrayElementAtIndex(idx);
                var el = weaponProficienciesProp.GetArrayElementAtIndex(idx);
                el.FindPropertyRelative("weaponType").objectReferenceValue = kv.Key;
                var vProp = el.FindPropertyRelative("rank._value");
                if (vProp != null)
                    vProp.stringValue = kv.Value;
                idx++;
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
