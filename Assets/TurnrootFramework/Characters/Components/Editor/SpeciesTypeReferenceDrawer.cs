#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Turnroot.Characters;
using Turnroot.Utilities;

namespace Turnroot.Editor.PropertyDrawers
{
    /// <summary>
    /// Custom property drawer for SpeciesType fields.
    /// Shows a dropdown of species types configured in GameplayGeneralSettings.
    /// </summary>
    [CustomPropertyDrawer(typeof(SpeciesType), true)]
    public class SpeciesTypeReferenceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Try to get the configured list from GameplayGeneralSettings
            var settings = GameSettingsLoader.LoadFirst<GameplayGeneralSettings>("GameSettings");
            SpeciesType[] options = null;
            if (
                settings != null
                && settings.SpeciesTypes != null
                && settings.SpeciesTypes.Length > 0
            )
            {
                options = settings.SpeciesTypes;
            }
            else
            {
                // Fallback: search the project for SpeciesType assets
                var guids = AssetDatabase.FindAssets("t:SpeciesType");
                var list = new List<SpeciesType>();
                foreach (var g in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(g);
                    var s = AssetDatabase.LoadAssetAtPath<SpeciesType>(path);
                    if (s != null)
                    {
                        list.Add(s);
                    }
                }
                options = list.ToArray();
            }

            // Build display names
            var names = new string[options.Length + 1];
            names[0] = "<None>";
            for (int i = 0; i < options.Length; i++)
            {
                names[i + 1] = options[i] == null ? "(null)" : options[i].name;
            }

            // Find current selection
            int currentIndex = 0;
            if (property.objectReferenceValue != null)
            {
                for (int i = 0; i < options.Length; i++)
                {
                    if (options[i] == property.objectReferenceValue)
                    {
                        currentIndex = i + 1;
                        break;
                    }
                }
            }

            // Draw popup
            int choice = EditorGUI.Popup(position, label.text, currentIndex, names);

            // Assign selection
            if (choice == 0)
            {
                property.objectReferenceValue = null;
            }
            else
            {
                property.objectReferenceValue = options[choice - 1];
            }

            EditorGUI.EndProperty();
        }
    }
}
#endif
