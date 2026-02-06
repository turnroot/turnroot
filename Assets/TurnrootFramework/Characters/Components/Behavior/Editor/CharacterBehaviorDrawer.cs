using UnityEditor;
using UnityEngine;
using static Turnroot.Characters.Components.Behavior.CharacterBehavior;

namespace Turnroot.Characters.Components.Behavior
{
    /// <summary>
    /// Custom property drawer for CharacterBehavior with preset selection and value sliders.
    /// </summary>
    [CustomPropertyDrawer(typeof(CharacterBehavior))]
    public class CharacterBehaviorDrawer : PropertyDrawer
    {
        public string InfoBoxText = "";

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // Calculate rects
            float lineHeight = EditorGUIUtility.singleLineHeight + 2;
            float buttonWidth = 85f;
            float buttonSpacing = 4f;
            float totalButtonWidth = buttonWidth * 2 + buttonSpacing;
            float buttonStartX = position.x + position.width - totalButtonWidth;
            // Preset dropdown uses all space except for the two buttons
            Rect presetRect = new Rect(
                position.x,
                position.y,
                position.width - totalButtonWidth - 4f,
                lineHeight
            );
            float y = position.y + lineHeight + 2;

            // Draw Preset dropdown
            var presetProp = property.FindPropertyRelative("preset");
            EditorGUI.PropertyField(presetRect, presetProp);

            // Update InfoBoxText based on current preset
            InfoBoxText = GetPresetInfoBoxText(
                (CharacterBehaviorPresetEnum)presetProp.enumValueIndex
            );

            // Draw Apply Preset and Jiggle buttons on the same row (right side)
            Rect applyPresetRect = new Rect(buttonStartX, position.y, buttonWidth, lineHeight);
            Rect jiggleButtonRect = new Rect(
                buttonStartX + buttonWidth + buttonSpacing,
                position.y,
                buttonWidth,
                lineHeight
            );
            if (GUI.Button(applyPresetRect, "Apply Preset"))
            {
                ApplyPreset(property, presetProp);
                property.serializedObject.ApplyModifiedProperties();
            }
            if (GUI.Button(jiggleButtonRect, "Jiggle"))
            {
                // Apply random values to sliders
                var sliderProps = new[]
                {
                    "SoldierLoneWolf",
                    "MindlessCunning",
                    "SelfishSelfless",
                    "BrashWary",
                    "BloodthirstGreed",
                };
                foreach (var sliderProp in sliderProps)
                {
                    var prop = property.FindPropertyRelative(sliderProp);
                    if (prop != null)
                    {
                        var value = Random.value;
                        prop.floatValue += Mathf.Lerp(-0.05f, .05f, value);
                    }
                }
                property.serializedObject.ApplyModifiedProperties();
            }
            y = position.y + lineHeight + 2; // Move y down for subsequent controls

            // Draw an info box with preset details
            if (!string.IsNullOrEmpty(InfoBoxText))
            {
                Rect infoBoxRect = new Rect(position.x, y, position.width, lineHeight * 2);
                EditorGUI.HelpBox(infoBoxRect, InfoBoxText, MessageType.Info);
                y += lineHeight * 2 + 2;
            }

            // Draw sliders
            DrawSlider(property, ref y, position, "SoldierLoneWolf", "Soldier/Lone Wolf");
            DrawSlider(property, ref y, position, "MindlessCunning", "Mindless/Cunning");
            DrawSlider(property, ref y, position, "SelfishSelfless", "Selfish/Selfless");
            DrawSlider(property, ref y, position, "BrashWary", "Brash/Wary");
            DrawSlider(property, ref y, position, "BloodthirstGreed", "Bloodthirst/Greed");

            // Draw MovementDisabled checkbox
            var movementDisabledProp = property.FindPropertyRelative("MovementDisabled");
            if (movementDisabledProp != null)
            {
                Rect moveRect = new Rect(
                    position.x,
                    y,
                    position.width,
                    EditorGUIUtility.singleLineHeight
                );
                EditorGUI.PropertyField(
                    moveRect,
                    movementDisabledProp,
                    new GUIContent("Movement Disabled")
                );
                y += EditorGUIUtility.singleLineHeight + 2;
            }
            // Draw AttackDisabled checkbox
            var attackDisabledProp = property.FindPropertyRelative("AttackDisabled");
            if (attackDisabledProp != null)
            {
                Rect attackRect = new Rect(
                    position.x,
                    y,
                    position.width,
                    EditorGUIUtility.singleLineHeight
                );
                EditorGUI.PropertyField(
                    attackRect,
                    attackDisabledProp,
                    new GUIContent("Attack Disabled (will counterattack)")
                );
                y += EditorGUIUtility.singleLineHeight + 2;
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        private string GetPresetInfoBoxText(CharacterBehaviorPresetEnum presetEnum)
        {
            return presetEnum switch
            {
                CharacterBehaviorPresetEnum.MindlessBerserker =>
                    "Mindlessly attacks nearby enemies without regard for self-preservation",
                CharacterBehaviorPresetEnum.CunningAssassin =>
                    "Cautiously works alone to eliminate strategic enemies.",
                CharacterBehaviorPresetEnum.GreedyCoward =>
                    "Avoids combat, prefers looting and running away.",
                CharacterBehaviorPresetEnum.LoyalGuardian =>
                    "Stays near to allies and keeps them safe, even above their own safety",
                CharacterBehaviorPresetEnum.WaryProtector =>
                    "Prioritizes defense of allies while avoiding unnecessary risks",
                CharacterBehaviorPresetEnum.VengefulWarrior =>
                    "Generally strategic, flies into a murderous rampage when allies are killed",
                CharacterBehaviorPresetEnum.RecklessDuelist =>
                    "Seeks out enemies without considering themselves or allies",
                CharacterBehaviorPresetEnum.BalancedVeteran =>
                    "A well-rounded fighter who balances risks and support",
                _ => string.Empty,
            };
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // 1 line for preset+button
            float height = EditorGUIUtility.singleLineHeight + 2;
            // 2 lines for info box if shown
            var presetProp = property.FindPropertyRelative("preset");
            string infoBox = GetPresetInfoBoxText(
                (CharacterBehaviorPresetEnum)presetProp.enumValueIndex
            );
            if (!string.IsNullOrEmpty(infoBox))
                height += (EditorGUIUtility.singleLineHeight + 2) * 2;
            // 5 sliders
            height += (EditorGUIUtility.singleLineHeight + 2) * 5;
            // 2 checkboxes
            height += (EditorGUIUtility.singleLineHeight + 2) * 2;
            return height;
        }

        private void DrawSlider(
            SerializedProperty property,
            ref float y,
            Rect position,
            string field,
            string label
        )
        {
            var prop = property.FindPropertyRelative(field);
            if (prop != null)
            {
                Rect sliderRect = new Rect(
                    position.x,
                    y,
                    position.width,
                    EditorGUIUtility.singleLineHeight
                );

                // Color blend: #006D77 (0) to #7E9624 (1)
                Color color0 = new Color32(0x00, 0x6D, 0x77, 0xFF);
                Color color1 = new Color32(0x7E, 0x96, 0x24, 0xFF);
                float t = Mathf.Clamp01(prop.floatValue);
                Color blended = Color.Lerp(color0, color1, t);
                EditorGUI.DrawRect(sliderRect, blended);

                // Draw the slider on top
                EditorGUI.Slider(sliderRect, prop, 0f, 1f, label);
                y += EditorGUIUtility.singleLineHeight + 2;
            }
        }

        private void ApplyPreset(SerializedProperty property, SerializedProperty presetProp)
        {
            var presetEnum = (CharacterBehaviorPresetEnum)presetProp.enumValueIndex;
            CharacterBehavior presetValues = default;
            switch (presetEnum)
            {
                case CharacterBehaviorPresetEnum.MindlessBerserker:
                    presetValues = CharacterBehaviorPreset.MindlessBerserker;
                    break;
                case CharacterBehaviorPresetEnum.CunningAssassin:
                    presetValues = CharacterBehaviorPreset.CunningAssassin;
                    break;
                case CharacterBehaviorPresetEnum.GreedyCoward:
                    presetValues = CharacterBehaviorPreset.GreedyCoward;
                    break;
                case CharacterBehaviorPresetEnum.LoyalGuardian:
                    presetValues = CharacterBehaviorPreset.LoyalGuardian;
                    break;
                case CharacterBehaviorPresetEnum.WaryProtector:
                    presetValues = CharacterBehaviorPreset.WaryProtector;
                    break;
                case CharacterBehaviorPresetEnum.VengefulWarrior:
                    presetValues = CharacterBehaviorPreset.VengefulWarrior;
                    break;
                case CharacterBehaviorPresetEnum.RecklessDuelist:
                    presetValues = CharacterBehaviorPreset.RecklessDuelist;
                    break;
                case CharacterBehaviorPresetEnum.BalancedVeteran:
                    presetValues = CharacterBehaviorPreset.BalancedVeteran;
                    break;
                default:
                    return;
            }
            property.FindPropertyRelative("SoldierLoneWolf").floatValue =
                presetValues.SoldierLoneWolf;
            property.FindPropertyRelative("MindlessCunning").floatValue =
                presetValues.MindlessCunning;
            property.FindPropertyRelative("SelfishSelfless").floatValue =
                presetValues.SelfishSelfless;
            property.FindPropertyRelative("BrashWary").floatValue = presetValues.BrashWary;
            property.FindPropertyRelative("BloodthirstGreed").floatValue =
                presetValues.BloodthirstGreed;
        }
    }
}
