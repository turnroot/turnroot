using UnityEditor;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Shop
{
    [CustomPropertyDrawer(typeof(ShopItem))]
    public class ShopItemDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Safety: property can be null in some weird inspector states (e.g. missing serialized data).
            if (property == null)
            {
                return;
            }

            using (new EditorGUI.PropertyScope(position, label, property))
            {
                // Foldout label for the struct when displayed in an array
                position = EditorGUI.PrefixLabel(
                    position,
                    GUIUtility.GetControlID(FocusType.Passive),
                    label
                );

                EditorGUI.indentLevel++;

                var spacing = EditorGUIUtility.standardVerticalSpacing;

                void DrawProperty(ref Rect rect, SerializedProperty prop)
                {
                    if (prop == null)
                        return;

                    var height = EditorGUI.GetPropertyHeight(prop, true);
                    rect.height = height;
                    EditorGUI.PropertyField(rect, prop, true);
                    rect.y += height + spacing;
                }

                // Base fields
                DrawProperty(ref position, property.FindPropertyRelative("Item"));
                DrawProperty(ref position, property.FindPropertyRelative("CurrentStatus"));
                DrawProperty(ref position, property.FindPropertyRelative("MaxQuantity"));

                var restockAtIntervals = property.FindPropertyRelative("RestockAtIntervals");
                DrawProperty(ref position, restockAtIntervals);

                if (restockAtIntervals != null && restockAtIntervals.boolValue)
                {
                    DrawProperty(
                        ref position,
                        property.FindPropertyRelative("RestockIntervalDays")
                    );
                    DrawProperty(
                        ref position,
                        property.FindPropertyRelative("RestockQuantityPerDay")
                    );
                }

                var canGoOnSale = property.FindPropertyRelative("CanGoOnSale");
                DrawProperty(ref position, canGoOnSale);

                if (canGoOnSale != null && canGoOnSale.boolValue)
                {
                    DrawProperty(ref position, property.FindPropertyRelative("SalePrice"));
                    DrawProperty(ref position, property.FindPropertyRelative("SpecificSaleDays"));
                    DrawProperty(
                        ref position,
                        property.FindPropertyRelative("SaleChanceOnRandomDays")
                    );
                }

                var rareItem = property.FindPropertyRelative("RareItem");
                DrawProperty(ref position, rareItem);

                if (rareItem != null && rareItem.boolValue)
                {
                    DrawProperty(
                        ref position,
                        property.FindPropertyRelative("ChanceToAppearInShop")
                    );
                }

                EditorGUI.indentLevel--;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property == null)
            {
                return 0f;
            }

            var spacing = EditorGUIUtility.standardVerticalSpacing;

            float height = 0f;

            void AddHeight(SerializedProperty prop)
            {
                if (prop == null)
                    return;

                height += EditorGUI.GetPropertyHeight(prop, true) + spacing;
            }

            // Base fields
            AddHeight(property.FindPropertyRelative("Item"));
            AddHeight(property.FindPropertyRelative("CurrentStatus"));
            AddHeight(property.FindPropertyRelative("MaxQuantity"));
            AddHeight(property.FindPropertyRelative("RestockAtIntervals"));

            var restockAtIntervals = property.FindPropertyRelative("RestockAtIntervals");
            if (restockAtIntervals != null && restockAtIntervals.boolValue)
            {
                AddHeight(property.FindPropertyRelative("RestockIntervalDays"));
                AddHeight(property.FindPropertyRelative("RestockQuantityPerDay"));
            }

            AddHeight(property.FindPropertyRelative("CanGoOnSale"));

            var canGoOnSale = property.FindPropertyRelative("CanGoOnSale");
            if (canGoOnSale != null && canGoOnSale.boolValue)
            {
                AddHeight(property.FindPropertyRelative("SalePrice"));
                AddHeight(property.FindPropertyRelative("SpecificSaleDays"));
                AddHeight(property.FindPropertyRelative("SaleChanceOnRandomDays"));
            }

            AddHeight(property.FindPropertyRelative("RareItem"));

            var rareItem = property.FindPropertyRelative("RareItem");
            if (rareItem != null && rareItem.boolValue)
            {
                AddHeight(property.FindPropertyRelative("ChanceToAppearInShop"));
            }

            // Remove the trailing spacing for the last line (it was added unconditionally)
            if (height > 0f)
                height -= spacing;

            return height;
        }
    }
}
