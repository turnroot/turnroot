using UnityEditor;
using UnityEngine;

namespace Assets.Prototypes.Gameplay.Combat.FundamentalComponents.Editor
{
    [CustomPropertyDrawer(typeof(ExperienceType))]
    public class ExperienceTypeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _ = EditorGUI.BeginProperty(position, label, property);

            float yOffset = position.y;

            // Get properties
            var nameProp = property.FindPropertyRelative("_name");
            var idProp = property.FindPropertyRelative("_id");
            var enabledProp = property.FindPropertyRelative("_enabled");
            var hasWeaponTypeProp = property.FindPropertyRelative("_hasWeaponType");
            var weaponTypeProp = property.FindPropertyRelative("_associatedWeaponType");

            // Draw Name
            if (nameProp != null)
            {
                Rect rect = new Rect(position.x, yOffset, position.width, 16);
                EditorGUI.BeginChangeCheck();
                _ = EditorGUI.PropertyField(rect, nameProp, new GUIContent("Name"));
                if (EditorGUI.EndChangeCheck() && idProp != null)
                {
                    // Auto-generate ID from name
                    string name = nameProp.stringValue;
                    idProp.stringValue = string.IsNullOrEmpty(name)
                        ? ""
                        : name.ToLower().Replace(" ", "");
                }
                yOffset += 18;
            }

            // Draw Enabled
            if (enabledProp != null)
            {
                Rect rect = new Rect(position.x, yOffset, position.width, 16);
                _ = EditorGUI.PropertyField(rect, enabledProp, new GUIContent("Enabled"));
                yOffset += 18;
            }

            // Draw HasWeaponType
            if (hasWeaponTypeProp != null)
            {
                Rect rect = new Rect(position.x, yOffset, position.width, 16);
                _ = EditorGUI.PropertyField(rect, hasWeaponTypeProp, new GUIContent("Has Weapon Type"));
                yOffset += 18;
            }

            // Draw WeaponType only if HasWeaponType is true
            if (hasWeaponTypeProp != null && hasWeaponTypeProp.boolValue && weaponTypeProp != null)
            {
                Rect rect = new Rect(position.x, yOffset, position.width, 16);
                _ = EditorGUI.PropertyField(rect, weaponTypeProp, new GUIContent("Weapon Type"), true);
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var hasWeaponTypeProp = property.FindPropertyRelative("_hasWeaponType");
            var weaponTypeProp = property.FindPropertyRelative("_associatedWeaponType");

            float height = 18 * 4; // Name, ID, Enabled, HasWeaponType

            // Add height for WeaponType if HasWeaponType is true
            if (hasWeaponTypeProp != null && hasWeaponTypeProp.boolValue && weaponTypeProp != null)
            {
                height += EditorGUI.GetPropertyHeight(weaponTypeProp, true) + 2;
            }

            return height;
        }
    }
}
