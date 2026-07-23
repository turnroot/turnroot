using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Turnroot.Graphics
{
    public class CelShaderPropertyMigrator : EditorWindow
    {
        private const string ShaderName = "Turnroot/Generic Cel Shader";

        // ── 1:1 renames ─────────────────────────────────────────────────────
        // Old property name -> new property name. Only entries here get copied
        // automatically; anything with an obvious required unit conversion is
        // handled as a special case further down instead (see MigrateSpecialCases).

        private static readonly Dictionary<string, string> FloatMap = new Dictionary<string, string>
        {
            { "_ASEOutlineWidth", "_OutlineWidth" },
            { "_use_outlines", "_UseOutlines" },
            { "_Shadow_Strength", "_ShadowStrength" },
            { "_Shadow_Replace", "_ShadowColorReplace" },
            { "_Shadow_Offset", "_ShadowOffset" },
            { "_Shadow_Smoothness", "_ShadowSoftness" },
            { "_use_shadow_noise", "_UseShadowNoise" },
            { "_Shadow_Noise_Amount", "_ShadowNoiseAmount" },
            { "_Highlight_Amount", "_HighlightStrength" },
            { "_Highlight_Replace", "_HighlightColorReplace" },
            { "_Highlight_Offset", "_HighlightOffset" },
            { "_Highlight_Smoothness", "_HighlightSoftness" },
            { "_use_highlight_mask", "_UseHighlightMask" },
            { "_Highlight_Mask_Amount", "_HighlightMaskAmount" },
            { "_Show_Masks", "_ShowCelMasks" },
            { "_use_main_emissive", "_UseEmission" },
            { "_use_matcat", "_UseMatcap" }, // old shader's (broken) keyword name; float name itself was fine
            { "_MatcapIntensity", "_MatcapIntensity" },
            { "_MatcapObjectSpace", "_MatcapObjectSpace" },
            { "_use_matcap_reflection", "_UseMatcapReflection" },
            { "_special_buff_switch_edge_hardness", "_MatcapMaskHardness" },
            { "_special_buff_dissolve", "_MatcapMaskDissolve" },
            { "_use_matcap_emissive", "_UseMatcapEmissive" },
            { "_Matcap_Emissve_power", "_MatcapEmissivePower" },
            { "_use_matcap_animation", "_UseMatcapAnimation" },
            { "_matcap_animation_speed", "_MatcapAnimationSpeed" },
            { "_use_frensel", "_UseFresnel" },
            { "_frensel_range", "_FresnelRange" },
            { "_frensel_hard", "_FresnelHardness" },
            { "_frensel_power", "_FresnelPower" },
            { "_NightTintIntensity", "_NightTintIntensity" },
            { "_Cutoff", "_Cutoff" },
        };

        private static readonly Dictionary<string, string> ColorMap = new Dictionary<string, string>
        {
            { "_ASEOutlineColor", "_OutlineColor" },
            { "_light", "_HighlightColor" },
            { "_dark", "_ShadowColor" },
            { "_BaseTint", "_BaseColor" },
            { "_NightTintColor", "_NightTintColor" },
            { "_Matcap_Emissve_color", "_MatcapEmissiveColor" },
            { "_frensel_color", "_FresnelColor" },
        };

        private static readonly Dictionary<string, string> TextureMap = new Dictionary<
            string,
            string
        >
        {
            { "_MainTex", "_BaseMap" },
            { "_NormalMap", "_BumpMap" },
            { "_matcap", "_MatcapTex" },
            { "_special_buff_switch", "_MatcapMaskTex" },
            { "_Main_Emissive_Tex", "_EmissionMap" },
            { "_Matcap_Emissive_Tex", "_MatcapEmissiveTex" },
            { "_Highlight_Mask_Tex", "_HighlightMaskTex" },
            { "_Shadow_Noise_Tex", "_ShadowNoiseTex" },
        };

        // Float properties that are also [Toggle(KEYWORD)] shader_feature switches
        // in the new shader — setting the float alone doesn't flip the compiled
        // variant, the keyword has to be enabled/disabled too.
        private static readonly Dictionary<string, string> ToggleKeywords = new Dictionary<
            string,
            string
        >
        {
            { "_UseOutlines", "_USE_OUTLINES_ON" },
            { "_UseHighlightMask", "_USE_HIGHLIGHT_MASK_ON" },
            { "_UseShadowNoise", "_USE_SHADOW_NOISE_ON" },
            { "_UseEmission", "_USE_EMISSION_ON" },
            { "_UseMatcap", "_USE_MATCAP_ON" },
            { "_UseMatcapReflection", "_USE_MATCAP_REFLECTION_ON" },
            { "_UseMatcapAnimation", "_USE_MATCAP_ANIMATION_ON" },
            { "_UseMatcapEmissive", "_USE_MATCAP_EMISSIVE_ON" },
            { "_UseFresnel", "_USE_FRESNEL_ON" },
        };

        // Old properties with no equivalent in the new shader at all — features
        // that were dropped or merged, not renamed. If a material has one of
        // these set to something other than its old default, flag it so you can
        // decide by hand whether it needs to be recreated another way.
        private static readonly Dictionary<string, float> DroppedFloatsWithOldDefault =
            new Dictionary<string, float>
            {
                { "_Cel_Shader_Offset", 0.64f }, // was declared but never actually read anywhere in the old shader
                { "_Shadow_Roughness", 0f }, // separate sharpness-exponent control, merged into Softness
                { "_Highlight_Roughness", 0f }, // same
                { "_Additional_Light_Falloff", 0f }, // stylized/physical blend removed — additional lights now always use correct physical falloff
                { "_use_light_tex", 0f }, // Light/Dark textures removed — solid Shadow/Highlight Color only now
                { "_use_dark_tex", 0f },
            };

        private static readonly string[] DroppedTextures = { "_LightTex", "_DarkTex" };

        // ── UI ──────────────────────────────────────────────────────────────

        [MenuItem("Tools/Turnroot/Migrate Cel Shader Materials")]
        public static void ShowWindow()
        {
            GetWindow<CelShaderPropertyMigrator>("Cel Shader Migrator");
        }

        private List<Material> _found = new List<Material>();
        private Vector2 _scroll;
        private bool _dryRun = true;

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Finds every Material using '"
                    + ShaderName
                    + "' in the project, reads "
                    + "any values still saved under the OLD property names, and copies them onto "
                    + "the new property names (including keyword sync for the On/Off toggles, and "
                    + "unit conversion for emission color/power and highlight saturation).\n\n"
                    + "Back up the project / make sure it's under version control first. Run with "
                    + "Dry Run checked, read the Console log, then uncheck it and run for real.",
                MessageType.Info
            );

            _dryRun = EditorGUILayout.ToggleLeft(
                "Dry Run (log only, don't modify materials)",
                _dryRun
            );

            if (GUILayout.Button("Find Materials"))
            {
                _found = FindMaterials();
                Debug.Log(
                    $"[CelShaderMigrator] Found {_found.Count} material(s) using '{ShaderName}'."
                );
            }

            EditorGUILayout.LabelField($"Materials found: {_found.Count}");
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(200));
            foreach (var mat in _found)
            {
                EditorGUILayout.ObjectField(mat, typeof(Material), false);
            }
            EditorGUILayout.EndScrollView();

            GUI.enabled = _found.Count > 0;
            if (GUILayout.Button(_dryRun ? "Preview Migration (Dry Run)" : "Migrate Properties"))
            {
                int totalChanged = 0;
                foreach (var mat in _found)
                {
                    totalChanged += MigrateMaterial(mat, _dryRun);
                }
                if (!_dryRun)
                {
                    AssetDatabase.SaveAssets();
                }
                string verb = _dryRun ? "Would update" : "Updated";
                EditorUtility.DisplayDialog(
                    "Migration " + (_dryRun ? "Preview" : "Complete"),
                    $"{verb} {totalChanged} propert{(totalChanged == 1 ? "y" : "ies")} across {_found.Count} material(s). See Console for details.",
                    "OK"
                );
            }
            GUI.enabled = true;
        }

        // ── Core ────────────────────────────────────────────────────────────

        private static List<Material> FindMaterials()
        {
            var result = new List<Material>();
            var guids = AssetDatabase.FindAssets("t:Material");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null && mat.shader != null && mat.shader.name == ShaderName)
                {
                    result.Add(mat);
                }
            }
            return result;
        }

        private static int MigrateMaterial(Material mat, bool dryRun)
        {
            int changed = 0;
            var so = new SerializedObject(mat);
            var savedProps = so.FindProperty("m_SavedProperties");
            if (savedProps == null)
            {
                Debug.LogWarning(
                    $"[CelShaderMigrator] Couldn't read serialized properties on '{mat.name}' — skipping. "
                        + "Unity's internal Material layout may have changed; this tool needs updating."
                );
                return 0;
            }

            var floats = savedProps.FindPropertyRelative("m_Floats");
            var colors = savedProps.FindPropertyRelative("m_Colors");
            var texEnvs = savedProps.FindPropertyRelative("m_TexEnvs");

            // Pull out raw old values we need, including the two special-case pairs.
            var oldFloats = ReadAll(floats, p => p.floatValue);
            var oldColors = ReadAll(colors, p => p.colorValue);

            // ── Simple 1:1 renames ──────────────────────────────────────────
            foreach (var kv in oldFloats)
            {
                if (FloatMap.TryGetValue(kv.Key, out string newName) && mat.HasProperty(newName))
                {
                    changed += ApplyFloat(mat, newName, kv.Value, dryRun, mat.name, kv.Key);
                }
            }
            foreach (var kv in oldColors)
            {
                if (ColorMap.TryGetValue(kv.Key, out string newName) && mat.HasProperty(newName))
                {
                    changed += ApplyColor(mat, newName, kv.Value, dryRun, mat.name, kv.Key);
                }
            }
            if (texEnvs != null)
            {
                for (int i = 0; i < texEnvs.arraySize; i++)
                {
                    var entry = texEnvs.GetArrayElementAtIndex(i);
                    var first = entry.FindPropertyRelative("first");
                    if (first == null)
                    {
                        continue;
                    }


                    string oldName = first.stringValue;
                    if (
                        !TextureMap.TryGetValue(oldName, out string newName)
                        || !mat.HasProperty(newName)
                    )
                    {
                        continue;
                    }


                    var second = entry.FindPropertyRelative("second");
                    var tex =
                        second.FindPropertyRelative("m_Texture").objectReferenceValue as Texture;
                    var scale = second.FindPropertyRelative("m_Scale").vector2Value;
                    var offset = second.FindPropertyRelative("m_Offset").vector2Value;

                    Debug.Log(
                        $"[CelShaderMigrator] {mat.name}: {oldName} -> {newName} = {(tex != null ? tex.name : "<none>")}, tiling {scale}, offset {offset}"
                    );
                    if (!dryRun)
                    {
                        mat.SetTexture(newName, tex);
                        mat.SetTextureScale(newName, scale);
                        mat.SetTextureOffset(newName, offset);
                    }
                    changed++;
                }
            }

            // ── Toggle keyword sync ─────────────────────────────────────────
            foreach (var kv in ToggleKeywords)
            {
                if (!mat.HasProperty(kv.Key))
                {
                    continue;
                }


                bool on = mat.GetFloat(kv.Key) > 0.5f;
                Debug.Log(
                    $"[CelShaderMigrator] {mat.name}: keyword {kv.Value} -> {(on ? "ON" : "OFF")} (from {kv.Key})"
                );
                if (!dryRun)
                {
                    if (on)
                    {
                        mat.EnableKeyword(kv.Value);
                    }
                    else
                    {
                        mat.DisableKeyword(kv.Value);
                    }

                }
            }

            // ── Special cases requiring a real conversion, not a straight copy ─
            changed += MigrateSpecialCases(mat, oldFloats, oldColors, dryRun);

            // ── Report dropped features that were actually in use ──────────
            ReportDropped(mat, oldFloats, texEnvs);

            if (changed > 0 && !dryRun)
            {
                EditorUtility.SetDirty(mat);
            }
            return changed;
        }

        private static int MigrateSpecialCases(
            Material mat,
            Dictionary<string, float> oldFloats,
            Dictionary<string, Color> oldColors,
            bool dryRun
        )
        {
            int changed = 0;

            // Emission: old shader multiplied a separate power scalar into an HDR
            // color at sample time (emis * color * power). New shader just does
            // emis * color, so power needs folding into the color up front.
            if (mat.HasProperty("_EmissionColor"))
            {
                bool hasColor = oldColors.TryGetValue("_Main_Emissve_color", out Color oldColor);
                bool hasPower = oldFloats.TryGetValue("_Main_Emissve_power", out float oldPower);
                if (hasColor || hasPower)
                {
                    Color c = hasColor ? oldColor : Color.white;
                    float p = hasPower ? oldPower : 1f;
                    Color newColor = new Color(c.r * p, c.g * p, c.b * p, c.a);
                    Debug.Log(
                        $"[CelShaderMigrator] {mat.name}: _Main_Emissve_color({c}) * _Main_Emissve_power({p}) -> _EmissionColor({newColor})"
                    );
                    if (!dryRun)
                    {
                        mat.SetColor("_EmissionColor", newColor);
                    }


                    changed++;
                }
            }

            // Highlight saturation: old formula used (1.0 + value) as its
            // multiplier (so 0 = neutral); new formula uses value directly as the
            // multiplier (so 1 = neutral). Straight copy would silently desaturate
            // every material that left this at its old default.
            if (
                mat.HasProperty("_HighlightSaturation")
                && oldFloats.TryGetValue("_Highlight_Saturation", out float oldSat)
            )
            {
                float newSat = oldSat + 1.0f;
                Debug.Log(
                    $"[CelShaderMigrator] {mat.name}: _Highlight_Saturation({oldSat}) -> _HighlightSaturation({newSat}) [+1.0 convention change]"
                );
                if (!dryRun)
                {
                    mat.SetFloat("_HighlightSaturation", newSat);
                }


                changed++;
            }

            return changed;
        }

        private static void ReportDropped(
            Material mat,
            Dictionary<string, float> oldFloats,
            SerializedProperty texEnvs
        )
        {
            foreach (var kv in DroppedFloatsWithOldDefault)
            {
                if (
                    oldFloats.TryGetValue(kv.Key, out float val)
                    && !Mathf.Approximately(val, kv.Value)
                )
                {
                    Debug.LogWarning(
                        $"[CelShaderMigrator] {mat.name}: '{kv.Key}' has no equivalent in the new shader "
                            + $"and was set to {val} (non-default). This feature was removed/merged — check the shader notes."
                    );
                }
            }
            if (texEnvs == null)
            {
                return;
            }


            for (int i = 0; i < texEnvs.arraySize; i++)
            {
                var entry = texEnvs.GetArrayElementAtIndex(i);
                var first = entry.FindPropertyRelative("first");
                if (first == null)
                {
                    continue;
                }


                string oldName = first.stringValue;
                if (Array.IndexOf(DroppedTextures, oldName) < 0)
                {
                    continue;
                }


                var tex =
                    entry
                        .FindPropertyRelative("second")
                        .FindPropertyRelative("m_Texture")
                        .objectReferenceValue as Texture;
                if (tex != null)
                {
                    Debug.LogWarning(
                        $"[CelShaderMigrator] {mat.name}: '{oldName}' had a texture assigned ({tex.name}) "
                            + "but Light/Dark textures were removed from the new shader (solid Shadow/Highlight Color only now)."
                    );
                }
            }
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private static Dictionary<string, T> ReadAll<T>(
            SerializedProperty arrayProp,
            Func<SerializedProperty, T> getValue
        )
        {
            var result = new Dictionary<string, T>();
            if (arrayProp == null)
            {
                return result;
            }


            for (int i = 0; i < arrayProp.arraySize; i++)
            {
                var entry = arrayProp.GetArrayElementAtIndex(i);
                var first = entry.FindPropertyRelative("first");
                var second = entry.FindPropertyRelative("second");
                if (first == null || second == null)
                {
                    continue;
                }


                result[first.stringValue] = getValue(second);
            }
            return result;
        }

        private static int ApplyFloat(
            Material mat,
            string newName,
            float value,
            bool dryRun,
            string matName,
            string oldName
        )
        {
            Debug.Log($"[CelShaderMigrator] {matName}: {oldName} -> {newName} = {value}");
            if (!dryRun)
            {
                mat.SetFloat(newName, value);
            }


            return 1;
        }

        private static int ApplyColor(
            Material mat,
            string newName,
            Color value,
            bool dryRun,
            string matName,
            string oldName
        )
        {
            Debug.Log($"[CelShaderMigrator] {matName}: {oldName} -> {newName} = {value}");
            if (!dryRun)
            {

                mat.SetColor(newName, value);
            }


            return 1;
        }
    }
}
