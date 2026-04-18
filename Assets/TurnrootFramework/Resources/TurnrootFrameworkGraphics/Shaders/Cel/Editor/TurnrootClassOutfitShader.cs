#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

// Editor for Turnroot/Class Outfit Cel Shader — duplicated from TurnrootDefaultCharacterShader

namespace Turnroot
{
    public class TurnrootClassOutfitShader : ShaderGUI
    {
        // Foldout states (keep synced with other shader GUI)
        static bool showOutlines = true;
        static bool showMain = true;
        static bool showMainEmissive = false;
        static bool showMatcap = false;
        static bool showMatcapEmissive = false;
        static bool showFresnel = false;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            EditorGUILayout.Space();

            MaterialProperty Find(string n) => FindProperty(n, props, false);

            // Outlines
            showOutlines = EditorGUILayout.BeginFoldoutHeaderGroup(showOutlines, "Outlines");
            if (showOutlines)
            {
                materialEditor.ShaderProperty(Find("_use_outlines"), "Use Outlines");
                materialEditor.ShaderProperty(Find("_ASEOutlineWidth"), "Outline Width");
                materialEditor.ShaderProperty(Find("_ASEOutlineColor"), "Outline Color");
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Main (exposes Base + TintMask + Accent/Skin)
            showMain = EditorGUILayout.BeginFoldoutHeaderGroup(showMain, "Main");
            if (showMain)
            {
                // NOTE: Class Outfit shader uses _Base instead of _MainTex
                materialEditor.ShaderProperty(Find("_Base"), "Main Texture");
                materialEditor.ShaderProperty(Find("_NormalMap"), "Normal Map");

                // Tint / accent inputs specific to Class Outfit shader
                var tintProp = Find("_Tint_Mask");
                if (tintProp != null)
                {
                    materialEditor.ShaderProperty(tintProp, "Tint Mask");
                    materialEditor.ShaderProperty(Find("_Accent_Color_1"), "Accent Color 1");
                    materialEditor.ShaderProperty(Find("_Accent_Color_2"), "Accent Color 2");
                    materialEditor.ShaderProperty(Find("_Accent_Color_3"), "Accent Color 3");
                    materialEditor.ShaderProperty(Find("_Skin_Color"), "Skin Color");
                }

                // Expose optional MSE texture if present
                var mseProp = Find("_MSE");
                if (mseProp != null)
                {
                    materialEditor.ShaderProperty(mseProp, "MSE (Metal/Smooth/Emission) Map");
                }

                materialEditor.ShaderProperty(Find("_Cutoff"), "Mask Alpha Clip Cutoff");
                materialEditor.ShaderProperty(Find("_Cel_Shader_Offset"), "Cel Shader Offset");
                materialEditor.ShaderProperty(Find("_light"), "Light Color");
                materialEditor.ShaderProperty(Find("_use_light_tex"), "Use Light Texture");
                materialEditor.ShaderProperty(Find("_LightTex"), "Light Texture");
                materialEditor.ShaderProperty(Find("_dark"), "Dark Color");
                materialEditor.ShaderProperty(Find("_use_dark_tex"), "Use Dark Texture");
                materialEditor.ShaderProperty(Find("_DarkTex"), "Dark Texture");

                // Shadow & Highlight controls
                materialEditor.ShaderProperty(Find("_Shadow_Strength"), "Shadow Strength");

                // Replace toggle instead of enum
                MaterialProperty shadowReplaceProp = Find("_Shadow_Replace");
                bool shadowReplace = Mathf.Abs(shadowReplaceProp.floatValue) > 0.5f;
                EditorGUI.BeginChangeCheck();
                shadowReplace = EditorGUILayout.Toggle("Shadow Replace", shadowReplace);
                if (EditorGUI.EndChangeCheck())
                {
                    shadowReplaceProp.floatValue = shadowReplace ? 1f : 0f;
                }

                materialEditor.ShaderProperty(Find("_Shadow_Roughness"), "Shadow Roughness");
                materialEditor.ShaderProperty(Find("_Shadow_Offset"), "Shadow Offset");
                materialEditor.ShaderProperty(Find("_Shadow_Smoothness"), "Shadow Smoothness");

                EditorGUILayout.Space(6);

                materialEditor.ShaderProperty(Find("_use_shadow_noise"), "Use Shadow Noise");
                materialEditor.ShaderProperty(Find("_Shadow_Noise_Tex"), "Shadow Noise Texture");
                materialEditor.ShaderProperty(Find("_Shadow_Noise_Amount"), "Shadow Noise Amount");

                EditorGUILayout.Space(8);

                materialEditor.ShaderProperty(Find("_Highlight_Amount"), "Highlight Amount");

                // Replace toggle instead of enum
                MaterialProperty highlightReplaceProp = Find("_Highlight_Replace");
                bool highlightReplace = Mathf.Abs(highlightReplaceProp.floatValue) > 0.5f;
                EditorGUI.BeginChangeCheck();
                highlightReplace = EditorGUILayout.Toggle("Highlight Replace", highlightReplace);
                if (EditorGUI.EndChangeCheck())
                {
                    highlightReplaceProp.floatValue = highlightReplace ? 1f : 0f;
                }

                materialEditor.ShaderProperty(Find("_use_highlight_mask"), "Use Highlight Mask");
                materialEditor.ShaderProperty(
                    Find("_Highlight_Mask_Tex"),
                    "Highlight Mask Texture"
                );
                materialEditor.ShaderProperty(
                    Find("_Highlight_Mask_Amount"),
                    "Highlight Mask Amount"
                );
                materialEditor.ShaderProperty(
                    Find("_Highlight_Saturation"),
                    "Highlight Saturation"
                );

                materialEditor.ShaderProperty(Find("_Highlight_Offset"), "Highlight Offset");
                materialEditor.ShaderProperty(
                    Find("_Highlight_Smoothness"),
                    "Highlight Smoothness"
                );
                materialEditor.ShaderProperty(Find("_Highlight_Roughness"), "Highlight Roughness");

                materialEditor.ShaderProperty(Find("_Show_Masks"), "Show Masks (RGB S/M/H)");
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Main Emissive
            showMainEmissive = EditorGUILayout.BeginFoldoutHeaderGroup(
                showMainEmissive,
                "Main Emissive"
            );
            if (showMainEmissive)
            {
                materialEditor.ShaderProperty(Find("_use_main_emissive"), "Use Main Emissive");
                materialEditor.ShaderProperty(Find("_Main_Emissive_Tex"), "Main Emissive Texture");
                materialEditor.ShaderProperty(Find("_Main_Emissve_color"), "Main Emissive Color");
                materialEditor.ShaderProperty(Find("_Main_Emissve_power"), "Main Emissive Power");
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Matcap
            showMatcap = EditorGUILayout.BeginFoldoutHeaderGroup(showMatcap, "Matcap");
            if (showMatcap)
            {
                materialEditor.ShaderProperty(Find("_use_matcat"), "Use Mat Cap");
                materialEditor.ShaderProperty(Find("_matcap"), "Matcap Texture");
                materialEditor.ShaderProperty(Find("_special_buff_switch"), "Matcap Switch Mask");
                materialEditor.ShaderProperty(
                    Find("_special_buff_switch_edge_hardness"),
                    "Matcap Switch Edge Hardness"
                );
                materialEditor.ShaderProperty(
                    Find("_special_buff_dissolve"),
                    "Matcap Switch Dissolve"
                );
                materialEditor.ShaderProperty(
                    Find("_use_matcap_reflection"),
                    "Matcap Reflection Mode"
                );
                materialEditor.ShaderProperty(Find("_MatcapIntensity"), "Matcap Intensity");

                // ─── Toggle instead of float field ───
                MaterialProperty objSpaceProp = Find("_MatcapObjectSpace");
                bool objSpace = Mathf.Abs(objSpaceProp.floatValue) > 0.5f;
                EditorGUI.BeginChangeCheck();
                objSpace = EditorGUILayout.Toggle("Object-Space Highlight", objSpace);
                if (EditorGUI.EndChangeCheck())
                {
                    objSpaceProp.floatValue = objSpace ? 1f : 0f;
                }

                materialEditor.ShaderProperty(
                    Find("_use_matcap_animation"),
                    "Animate Matcap Texture"
                );
                materialEditor.ShaderProperty(
                    Find("_matcap_animation_speed"),
                    "Matcap Animation Speed"
                );
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Matcap Emissive
            showMatcapEmissive = EditorGUILayout.BeginFoldoutHeaderGroup(
                showMatcapEmissive,
                "Matcap Emissive"
            );
            if (showMatcapEmissive)
            {
                materialEditor.ShaderProperty(Find("_use_matcap_emissive"), "Use Matcap Emissive");
                materialEditor.ShaderProperty(
                    Find("_Matcap_Emissive_Tex"),
                    "Matcap Emissive Texture"
                );
                materialEditor.ShaderProperty(
                    Find("_Matcap_Emissve_color"),
                    "Matcap Emissive Color"
                );
                materialEditor.ShaderProperty(
                    Find("_Matcap_Emissve_power"),
                    "Matcap Emissive Power"
                );
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Fresnel
            showFresnel = EditorGUILayout.BeginFoldoutHeaderGroup(showFresnel, "Fresnel");
            if (showFresnel)
            {
                materialEditor.ShaderProperty(Find("_use_frensel"), "Use Fresnel");
                materialEditor.ShaderProperty(Find("_frensel_range"), "Fresnel Range");
                materialEditor.ShaderProperty(Find("_frensel_hard"), "Fresnel Hardness");
                materialEditor.ShaderProperty(Find("_frensel_power"), "Fresnel Power");
                materialEditor.ShaderProperty(Find("_frensel_color"), "Fresnel Color");
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(10);
        }
    }
}
#endif
