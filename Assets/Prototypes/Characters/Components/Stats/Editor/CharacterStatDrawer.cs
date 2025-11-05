using UnityEditor;
using UnityEngine;

namespace Assets.Prototypes.Characters.Stats.Editor
{
    [CustomPropertyDrawer(typeof(BoundedCharacterStat))]
    public class CharacterStatDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _ = EditorGUI.BeginProperty(position, label, property);

            // Get the serialized fields
            var statTypeProp = property.FindPropertyRelative("_statType");
            var minProp = property.FindPropertyRelative("_min");
            var currentProp = property.FindPropertyRelative("_current");
            var maxProp = property.FindPropertyRelative("_max");
            var bonusProp = property.FindPropertyRelative("_bonus");

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float yOffset = position.y;

            // Draw stat type dropdown at the top
            Rect statTypeRect = new Rect(position.x, yOffset, position.width, lineHeight);
            _ = EditorGUI.PropertyField(statTypeRect, statTypeProp, new GUIContent("Stat Type"));
            yOffset += lineHeight + 2;

            // Calculate values
            float min = minProp.floatValue;
            float current = currentProp.floatValue;
            float max = maxProp.floatValue;
            float bonus = bonusProp.floatValue;
            float ratio = max == 0 ? 0 : (current + bonus) / max;

            // Color logic:
            // - Blue if bonus is active (bonus != 0)
            // - Red if ratio <= 25%
            // - Green otherwise
            Color barColor;
            if (bonus != 0)
            {
                barColor = new Color(0.2f, 0.5f, 1f); // Blue
            }
            else if (ratio <= 0.25f)
            {
                barColor = Color.red;
            }
            else
            {
                barColor = Color.green;
            }

            // Draw label (use stat name from enum)
            Rect labelRect = new Rect(position.x, yOffset, EditorGUIUtility.labelWidth, lineHeight);
            EditorGUI.LabelField(labelRect, label);

            // Draw progress bar
            Rect barRect = new Rect(
                position.x + EditorGUIUtility.labelWidth,
                yOffset,
                position.width - EditorGUIUtility.labelWidth,
                EditorGUIUtility.singleLineHeight
            );

            // Background
            EditorGUI.DrawRect(barRect, new Color(0.2f, 0.2f, 0.2f, 0.5f));

            // Fill
            Rect fillRect = new Rect(
                barRect.x,
                barRect.y,
                barRect.width * Mathf.Clamp01(ratio),
                barRect.height
            );
            EditorGUI.DrawRect(fillRect, barColor);

            // Text overlay showing current+bonus / max
            string valueText = $"{Mathf.RoundToInt(current + bonus)} / {Mathf.RoundToInt(max)}";
            EditorGUI.LabelField(
                barRect,
                valueText,
                new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = new GUIStyleState { textColor = Color.white },
                }
            );

            // Draw fields below the progress bar
            yOffset += lineHeight + 2;
            float fieldWidth = (position.width - EditorGUIUtility.labelWidth) / 4f;
            float xStart = position.x + EditorGUIUtility.labelWidth;

            // Min
            Rect minRect = new Rect(
                xStart,
                yOffset,
                fieldWidth - 2,
                EditorGUIUtility.singleLineHeight
            );
            EditorGUI.LabelField(
                new Rect(
                    position.x,
                    yOffset,
                    EditorGUIUtility.labelWidth,
                    EditorGUIUtility.singleLineHeight
                ),
                "Min/Cur/Max/Bonus"
            );
            minProp.floatValue = EditorGUI.FloatField(minRect, minProp.floatValue);

            // Current
            Rect currentRect = new Rect(
                xStart + fieldWidth,
                yOffset,
                fieldWidth - 2,
                EditorGUIUtility.singleLineHeight
            );
            currentProp.floatValue = EditorGUI.FloatField(currentRect, currentProp.floatValue);

            // Max
            Rect maxRect = new Rect(
                xStart + fieldWidth * 2,
                yOffset,
                fieldWidth - 2,
                EditorGUIUtility.singleLineHeight
            );
            maxProp.floatValue = EditorGUI.FloatField(maxRect, maxProp.floatValue);

            // Bonus
            Rect bonusRect = new Rect(
                xStart + fieldWidth * 3,
                yOffset,
                fieldWidth - 2,
                EditorGUIUtility.singleLineHeight
            );
            bonusProp.floatValue = EditorGUI.FloatField(bonusRect, bonusProp.floatValue);

            // Clamp current between min and max after editing
            currentProp.floatValue = Mathf.Clamp(
                currentProp.floatValue,
                minProp.floatValue,
                maxProp.floatValue
            );

            // Clamp bonus to prevent (current + bonus) from going below min
            // But allow bonus to be negative as long as (current + bonus) >= min
            float minAllowedBonus = minProp.floatValue - currentProp.floatValue;
            bonusProp.floatValue = Mathf.Max(bonusProp.floatValue, minAllowedBonus);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 3 + 6; // Stat type dropdown + progress bar + fields + padding
        }
    }
}
