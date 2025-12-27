using System;
using System.Collections.Generic;
using System.Linq;
using Turnroot.Gameplay.Combat;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BattleGameObject))]
public class BattleGameObjectEditor : Editor
{
    private SerializedProperty _battleConditionsProp;
    private Dictionary<string, bool> _eventsFoldouts = new Dictionary<string, bool>();

    private void OnEnable()
    {
        _battleConditionsProp = serializedObject.FindProperty("_battleConditions");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw everything except the _battleConditions field so we can provide a custom UI for it
        DrawPropertiesExcluding(serializedObject, "_battleConditions");

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
                string typeName = managed != null ? PrettyNameForType(managed.GetType()) : "(null)";
                EditorGUILayout.LabelField(typeName, EditorStyles.boldLabel);

                if (GUILayout.Button("Remove", GUILayout.Width(80)))
                {
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
