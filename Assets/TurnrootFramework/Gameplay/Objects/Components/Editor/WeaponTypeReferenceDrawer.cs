using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Turnroot.Gameplay.Objects.Components;
using Turnroot.Utilities;

namespace Turnroot.Editor.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(WeaponType), true)]
    public class WeaponTypeReferenceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Try to get the configured list from GameplayGeneralSettings
            var settings = GameSettingsLoader.LoadFirst<GameplayGeneralSettings>("GameSettings");
            WeaponType[] options = null;
            if (settings != null && settings.WeaponTypes != null && settings.WeaponTypes.Length > 0)
                options = settings.WeaponTypes;
            else
            {
                // Fallback: search the project for WeaponType assets
                var guids = AssetDatabase.FindAssets("t:WeaponType");
                var list = new List<WeaponType>();
                foreach (var g in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(g);
                    var w = AssetDatabase.LoadAssetAtPath<WeaponType>(path);
                    if (w != null) list.Add(w);
                }
                options = list.ToArray();
            }

            // Build display names
            var names = new string[options.Length + 1];
            names[0] = "<None>";
            for (int i = 0; i < options.Length; i++) names[i + 1] = options[i] == null ? "(null)" : options[i].name;

            // Find current selection
            int currentIndex = 0;
            if (property.objectReferenceValue != null)
            {
                for (int i = 0; i < options.Length; i++) if (options[i] == property.objectReferenceValue) { currentIndex = i + 1; break; }
            }

            // Draw popup
            int choice = EditorGUI.Popup(position, label.text, currentIndex, names);

            // Assign selection
            if (choice == 0) property.objectReferenceValue = null;
            else property.objectReferenceValue = options[choice - 1];

            EditorGUI.EndProperty();
        }
    }
}
