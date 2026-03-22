using System.Collections.Generic;
using Turnroot.Gameplay.Objects;
using Turnroot.Gameplay.Objects.Components;
using UnityEditor;
using UnityEngine;

namespace Turnroot.Editor.PropertyDrawers
{
    /// <summary>
    /// Custom property drawer for WeaponType that displays a dropdown of all WeaponType assets in the project.
    /// </summary>
    [CustomPropertyDrawer(typeof(WeaponType), true)]
    public class WeaponTypeReferenceDrawer : PropertyDrawer
    {
        private static WeaponType[] _cachedOptions;
        private static bool _optionsLoaded = false;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Try to get the configured list from GameplayGeneralSettings
            // Always show all WeaponType assets in the project as dropdown options
            if (!_optionsLoaded || _cachedOptions == null)
            {
                var guids = AssetDatabase.FindAssets("t:WeaponType");
                var list = new List<WeaponType>();
                foreach (var g in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(g);
                    var w = AssetDatabase.LoadAssetAtPath<WeaponType>(path);
                    if (w != null)
                    {
                        list.Add(w);
                    }
                }
                _cachedOptions = list.ToArray();
                _optionsLoaded = true;
            }
            var options = new List<WeaponType>(_cachedOptions);

            // If this property is on an ObjectItem, filter by subtype.
            // For Weapon subtype: non-magic only.
            // For Magic subtype: magic-only.
            if (property.serializedObject.targetObject is ObjectItem objectItem)
            {
                if (objectItem.IsWeaponSubtype())
                {
                    options.RemoveAll(w => w == null || w.IsMagic);
                }
                else if (objectItem.IsMagicSubtype())
                {
                    options.RemoveAll(w => w == null || !w.IsMagic);
                }
            }

            // Build display names
            var names = new string[options.Count + 1];
            names[0] = "<None>";
            for (int i = 0; i < options.Count; i++)
            {
                string display = null;
                var w = options[i];
                if (w == null)
                {
                    display = "(null)";
                }
                else if (!string.IsNullOrEmpty(w.Name))
                {
                    display = w.Name;
                }
                else if (!string.IsNullOrEmpty(w.name))
                {
                    display = w.name;
                }
                else if (!string.IsNullOrEmpty(w.Id))
                {
                    display = w.Id;
                }
                else
                {
                    display = "(unnamed)";
                }
                names[i + 1] = display;
            }

            // Find current selection
            int currentIndex = 0;
            if (property.objectReferenceValue != null)
            {
                for (int i = 0; i < options.Count; i++)
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
