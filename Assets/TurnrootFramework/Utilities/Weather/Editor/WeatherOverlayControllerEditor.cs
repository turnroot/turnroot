using UnityEditor;
using UnityEngine;

namespace Turnroot.Utilities.Weather.Editor
{
    [CustomEditor(typeof(WeatherOverlayController))]
    public class WeatherOverlayControllerEditor : UnityEditor.Editor
    {
        private static readonly string[] SectionTabs =
        {
            "Global",
            "Parallax",
            "Rain",
            "Drizzle",
            "Snow",
            "Ash",
            "Fade",
        };

        private int _selectedPresetIndex;
        private int _selectedSection;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawTopLevelSettings();
            EditorGUILayout.Space(8f);
            DrawActionButtons();
            EditorGUILayout.Space(8f);
            DrawPresetEditor();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawTopLevelSettings()
        {
            EditorGUILayout.LabelField("Weather Overlay Controller", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("OverlayRenderer"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SharedWeatherMaterial"));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("HideRendererWhenNoPreset")
            );
            EditorGUILayout.PropertyField(serializedObject.FindProperty("PreviewPreset"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("AutoPreviewInEditMode"));
        }

        private void DrawActionButtons()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply Preview Preset"))
                {
                    ForEachTarget(ctrl => ctrl.ApplyPreviewPreset());
                }

                if (GUILayout.Button("Rebuild Missing Presets"))
                {
                    ForEachTarget(ctrl => ctrl.RebuildMissingPresets());
                }

                if (GUILayout.Button("Clear Overlay"))
                {
                    ForEachTarget(ctrl => ctrl.ClearOverlay());
                }
            }
        }

        private void DrawPresetEditor()
        {
            SerializedProperty presetsProp = serializedObject.FindProperty("Presets");
            if (presetsProp == null)
            {
                return;
            }

            if (presetsProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox(
                    "No presets found. Click Rebuild Missing Presets.",
                    MessageType.Info
                );
                return;
            }

            _selectedPresetIndex = Mathf.Clamp(_selectedPresetIndex, 0, presetsProp.arraySize - 1);
            string[] labels = BuildPresetLabels(presetsProp);
            _selectedPresetIndex = EditorGUILayout.Popup("Preset", _selectedPresetIndex, labels);

            SerializedProperty presetProp = presetsProp.GetArrayElementAtIndex(
                _selectedPresetIndex
            );
            if (presetProp == null)
            {
                return;
            }

            SerializedProperty weatherTypeProp = presetProp.FindPropertyRelative("WeatherType");
            EditorGUILayout.PropertyField(weatherTypeProp);

            _selectedSection = GUILayout.Toolbar(_selectedSection, SectionTabs);
            EditorGUILayout.Space(6f);

            switch (_selectedSection)
            {
                case 0:
                    DrawGlobalSection(presetProp);
                    break;
                case 1:
                    DrawParallaxSection(presetProp);
                    break;
                case 2:
                    DrawRainSection(presetProp);
                    break;
                case 3:
                    DrawDrizzleSection(presetProp);
                    break;
                case 4:
                    DrawSnowSection(presetProp);
                    break;
                case 5:
                    DrawAshSection(presetProp);
                    break;
                case 6:
                    DrawFadeSection(presetProp);
                    break;
            }
        }

        private static string[] BuildPresetLabels(SerializedProperty presetsProp)
        {
            int size = presetsProp.arraySize;
            string[] labels = new string[size];

            for (int i = 0; i < size; i++)
            {
                SerializedProperty p = presetsProp.GetArrayElementAtIndex(i);
                SerializedProperty weatherTypeProp = p.FindPropertyRelative("WeatherType");
                string weather =
                    weatherTypeProp != null
                        ? weatherTypeProp.enumDisplayNames[weatherTypeProp.enumValueIndex]
                        : $"Preset {i + 1}";
                labels[i] = weather;
            }

            return labels;
        }

        private static void DrawGlobalSection(SerializedProperty preset)
        {
            DrawRelative(preset, "GlobalOpacity");
            DrawRelative(preset, "Brightness");
            DrawRelative(preset, "Contrast");
            DrawRelative(preset, "WorldForwardXZ");
            DrawRelative(preset, "GlobalWindAngle");
        }

        private static void DrawParallaxSection(SerializedProperty preset)
        {
            DrawRelative(preset, "ParallaxEnabled");
            DrawRelative(preset, "ParallaxAmount");
            DrawRelative(preset, "ParallaxYawAmount");
            DrawRelative(preset, "ParallaxPitchAmount");
            DrawRelative(preset, "ParallaxRain");
            DrawRelative(preset, "ParallaxDrizzle");
            DrawRelative(preset, "ParallaxSnow");
            DrawRelative(preset, "ParallaxAsh");

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Layer Parallax", EditorStyles.boldLabel);
            DrawRelative(preset, "LayerBackParallax");
            DrawRelative(preset, "LayerMidParallax");
            DrawRelative(preset, "LayerForeParallax");

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Layer Density", EditorStyles.boldLabel);
            DrawRelative(preset, "LayerBackDensity");
            DrawRelative(preset, "LayerMidDensity");
            DrawRelative(preset, "LayerForeDensity");

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Layer Size", EditorStyles.boldLabel);
            DrawRelative(preset, "LayerBackSize");
            DrawRelative(preset, "LayerMidSize");
            DrawRelative(preset, "LayerForeSize");
        }

        private static void DrawRainSection(SerializedProperty preset)
        {
            DrawRelative(preset, "RainEnabled");
            DrawRelative(preset, "RainIntensity");
            DrawRelative(preset, "RainOpacity");
            DrawRelative(preset, "RainColor");
            DrawRelative(preset, "RainDensity");
            DrawRelative(preset, "RainSpeed");
            DrawRelative(preset, "RainWidth");
            DrawRelative(preset, "RainLength");
            DrawRelative(preset, "RainWidthRandomness");
            DrawRelative(preset, "RainLengthRandomness");
            DrawRelative(preset, "RainStreakTiling");
            DrawRelative(preset, "RainFlatBody");
            DrawRelative(preset, "RainFallAngle");
            DrawRelative(preset, "RainCameraYawInfluence");
            DrawRelative(preset, "RainJitter");
            DrawRelative(preset, "RainSpawn");
            DrawRelative(preset, "RainSoftness");
        }

        private static void DrawDrizzleSection(SerializedProperty preset)
        {
            DrawRelative(preset, "DrizzleEnabled");
            DrawRelative(preset, "DrizzleIntensity");
            DrawRelative(preset, "DrizzleOpacity");
            DrawRelative(preset, "DrizzleColor");
            DrawRelative(preset, "DrizzleDensity");
            DrawRelative(preset, "DrizzleSpeed");
            DrawRelative(preset, "DrizzleWidth");
            DrawRelative(preset, "DrizzleLength");
            DrawRelative(preset, "DrizzleWidthRandomness");
            DrawRelative(preset, "DrizzleLengthRandomness");
            DrawRelative(preset, "DrizzleStreakTiling");
            DrawRelative(preset, "DrizzleFlatBody");
            DrawRelative(preset, "DrizzleFallAngle");
            DrawRelative(preset, "DrizzleCameraYawInfluence");
            DrawRelative(preset, "DrizzleJitter");
            DrawRelative(preset, "DrizzleSpawn");
            DrawRelative(preset, "DrizzleSoftness");
        }

        private static void DrawSnowSection(SerializedProperty preset)
        {
            DrawRelative(preset, "SnowEnabled");
            DrawRelative(preset, "SnowIntensity");
            DrawRelative(preset, "SnowOpacity");
            DrawRelative(preset, "SnowColor");
            DrawRelative(preset, "SnowDensity");
            DrawRelative(preset, "SnowSpeed");
            DrawRelative(preset, "SnowSize");
            DrawRelative(preset, "SnowSizeRandomness");
            DrawRelative(preset, "SnowDriftAmount");
            DrawRelative(preset, "SnowDriftSpeed");
            DrawRelative(preset, "SnowFallAngle");
            DrawRelative(preset, "SnowCameraYawInfluence");
            DrawRelative(preset, "SnowSpawn");
            DrawRelative(preset, "SnowDotEdgeSoftness");
        }

        private static void DrawAshSection(SerializedProperty preset)
        {
            DrawRelative(preset, "AshEnabled");
            DrawRelative(preset, "AshIntensity");
            DrawRelative(preset, "AshOpacity");
            DrawRelative(preset, "AshColor");
            DrawRelative(preset, "AshDensity");
            DrawRelative(preset, "AshSpeed");
            DrawRelative(preset, "AshSize");
            DrawRelative(preset, "AshSizeRandomness");
            DrawRelative(preset, "AshDriftAmount");
            DrawRelative(preset, "AshDriftSpeed");
            DrawRelative(preset, "AshFallAngle");
            DrawRelative(preset, "AshCameraYawInfluence");
            DrawRelative(preset, "AshSpawn");
            DrawRelative(preset, "AshDotEdgeSoftness");
        }

        private static void DrawFadeSection(SerializedProperty preset)
        {
            DrawRelative(preset, "VerticalFadeTop");
            DrawRelative(preset, "VerticalFadeBottom");
            DrawRelative(preset, "HorizontalFadeLeft");
            DrawRelative(preset, "HorizontalFadeRight");
        }

        private static void DrawRelative(SerializedProperty parent, string fieldName)
        {
            SerializedProperty prop = parent.FindPropertyRelative(fieldName);
            if (prop != null)
            {
                EditorGUILayout.PropertyField(prop);
            }
        }

        private void ForEachTarget(System.Action<WeatherOverlayController> action)
        {
            serializedObject.ApplyModifiedProperties();

            foreach (Object targetObj in targets)
            {
                if (targetObj is not WeatherOverlayController controller)
                {
                    continue;
                }

                Undo.RecordObject(controller, "Weather Overlay Controller Action");
                action(controller);
                EditorUtility.SetDirty(controller);
            }

            serializedObject.Update();
        }
    }
}
