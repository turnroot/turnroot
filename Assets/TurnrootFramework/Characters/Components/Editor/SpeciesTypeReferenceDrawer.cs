#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Turnroot.Characters;

namespace Turnroot.Editor.PropertyDrawers
{
    /// <summary>
    /// Custom property drawer for SpeciesType fields.
    /// Shows a dropdown of species types configured in GameplayGeneralSettings.
    /// </summary>
    [CustomPropertyDrawer(typeof(SpeciesType), true)]
    public class SpeciesTypeReferenceDrawer : PropertyDrawer
    {
        private static SpeciesType[] _cachedOptions;
        private static bool _optionsLoaded = false;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Always show all SpeciesType assets in the project as dropdown options
            if (!_optionsLoaded || _cachedOptions == null)
            {
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
                _cachedOptions = list.ToArray();
                _optionsLoaded = true;
            }
            SpeciesType[] options = _cachedOptions;

            // Build display names
            var names = new string[options.Length + 1];
            names[0] = "<None>";
            for (int i = 0; i < options.Length; i++)
            {
                if (options[i] == null)
                {
                    names[i + 1] = "(null)";
                }
                else
                {
                    // Prefer the Name property if set, otherwise fall back to asset name
                    names[i + 1] = !string.IsNullOrEmpty(options[i].Name)
                        ? options[i].Name
                        : options[i].name;
                }
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
