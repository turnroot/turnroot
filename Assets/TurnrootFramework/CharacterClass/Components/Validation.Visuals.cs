using System;
using System.Collections.Generic;
using Turnroot.Utilities;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Turnroot.Characters.CharacterClass
{
    public partial class CharacterClassData : ScriptableObject
    {
#if UNITY_EDITOR
        // Required blendshape names used by class visual validation
        private static readonly string[] RequiredBlendshapeNames = new[]
        {
            "ChestSize",
            "WaistSize",
            "HipSize",
            "ThighThickness",
            "ArmThickness",
            "NeckThickness",
        };

        private void ValidateClassVisuals()
        {
            if (Identity == null)
            {
                return;
            }

            ValidateDefaultClassModelPrefab();
            ValidatePronounClassModelPrefabs();
        }

        private List<string> ValidateMeshBlendshapes(Mesh mesh, string source)
        {
            var missing = new List<string>();
            if (mesh == null)
            {
                $"{name}: {source} has no mesh assigned.".LogError();
                return missing;
            }

            foreach (var blendshapeName in RequiredBlendshapeNames)
            {
                if (mesh.GetBlendShapeIndex(blendshapeName) < 0)
                {
                    missing.Add(blendshapeName);
                }
            }

            if (missing.Count > 0)
            {
                $"{name}: {source} is missing blendshapes: {string.Join(", ", missing)}".LogError();
            }
            return missing;
        }

        private static bool MaterialsExposeClassTextures(Material[] materials)
        {
            if (materials == null)
            {
                return false;
            }

            foreach (var material in materials)
            {
                if (
                    material != null
                    && (
                        material.HasProperty("_Base")
                        || material.HasProperty("_MSE")
                        || material.HasProperty("_Tint_Mask")
                    )
                )
                {
                    return true;
                }
            }
            return false;
        }

        private static bool PrefabContainsHairRenderer(GameObject prefab)
        {
            if (prefab == null)
            {
                return false;
            }

            if (prefab.transform.Find("Hair") != null)
            {
                return true;
            }

            foreach (
                var renderer in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true)
            )
            {
                if (
                    renderer != null
                    && renderer.gameObject.name.IndexOf(
                        "hair",
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0
                )
                {
                    return true;
                }
            }
            return false;
        }

        private void ClearDefaultClassModelPrefab(string undoName)
        {
            Undo.RecordObject(this, undoName);
            Identity.ClassModelPrefab = null;
            EditorUtility.SetDirty(this);
        }

        private void ValidateDefaultClassModelPrefab()
        {
            var prefab = Identity.ClassModelPrefab;
            if (prefab == null)
            {
                return;
            }

            var renderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                $"{name}: ClassModelPrefab '{prefab.name}' does not contain a SkinnedMeshRenderer. Clearing assignment.".LogError();
                ClearDefaultClassModelPrefab("Clear invalid ClassModelPrefab");
            }
            else
            {
                var missing = GetMissingBlendshapes(
                    renderers,
                    $"ClassModelPrefab '{prefab.name}'"
                );
                if (missing.Count > 0)
                {
                    $"{name}: ClassModelPrefab '{prefab.name}' is missing required blendshapes on submeshes: {string.Join(", ", missing)}. Clearing assignment.".LogError();
                    ClearDefaultClassModelPrefab("Clear invalid ClassModelPrefab");
                }
                else if (PrefabContainsHairRenderer(prefab))
                {
                    $"{name}: ClassModelPrefab '{prefab.name}' contains a 'Hair' renderer. Class models must not include hair; clearing assignment.".LogError();
                    ClearDefaultClassModelPrefab("Clear invalid ClassModelPrefab");
                }
            }

            if (!PrefabHasClassTextures(prefab))
            {
                $"{name}: ClassModelPrefab '{prefab.name}' contains no materials exposing class texture properties (_Base/_MSE/_Tint_Mask). Class textures will not be applied at runtime.".LogWarning();
                EditorUtility.SetDirty(this);
            }
        }

        private List<string> GetMissingBlendshapes(
            SkinnedMeshRenderer[] renderers,
            string source
        )
        {
            var missing = new List<string>();
            foreach (var renderer in renderers)
            {
                if (renderer != null)
                {
                    missing.AddRange(
                        ValidateMeshBlendshapes(
                            renderer.sharedMesh,
                            $"{source} - {renderer.gameObject.name}"
                        )
                    );
                }
            }
            return missing;
        }

        private static bool PrefabHasClassTextures(GameObject prefab)
        {
            if (prefab == null)
            {
                return false;
            }

            foreach (var renderer in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (MaterialsExposeClassTextures(renderer.sharedMaterials))
                {
                    return true;
                }
            }
            return false;
        }

        private void ValidatePronounClassModelPrefabs()
        {
            if (Identity.PronounClassModelPrefabs == null)
            {
                return;
            }

            foreach (var entry in Identity.PronounClassModelPrefabs)
            {
                var prefab = entry.prefab;
                if (prefab == null)
                {
                    continue;
                }

                var renderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if (renderers == null || renderers.Length == 0)
                {
                    $"{name}: PronounClassModelPrefabs entry for '{entry.pronounKey}' points to prefab '{prefab.name}' which does not contain a SkinnedMeshRenderer. Clearing that entry.".LogError();
                    Undo.RecordObject(
                        this,
                        $"Clear invalid PronounClassModelPrefabs[{entry.pronounKey}]"
                    );
                    EditorUtility.SetDirty(this);
                    continue;
                }

                var missing = GetMissingBlendshapes(
                    renderers,
                    $"PronounClassModelPrefabs '{entry.pronounKey}' -> '{prefab.name}'"
                );
                if (missing.Count > 0)
                {
                    $"{name}: PronounClassModelPrefabs entry for '{entry.pronounKey}' -> '{prefab.name}' is missing required blendshapes on submeshes: {string.Join(", ", missing)}. Clearing that entry.".LogError();
                    Undo.RecordObject(
                        this,
                        $"Clear invalid PronounClassModelPrefabs[{entry.pronounKey}]"
                    );
                    EditorUtility.SetDirty(this);
                }

                if (PrefabContainsHairRenderer(prefab))
                {
                    $"{name}: PronounClassModelPrefabs entry for '{entry.pronounKey}' -> '{prefab.name}' contains a 'Hair' renderer. Pronoun-specific class models must not include hair.".LogError();
                    Undo.RecordObject(
                        this,
                        $"PronounClassModelPrefabs contains invalid hair renderer [{entry.pronounKey}]"
                    );
                    EditorUtility.SetDirty(this);

                    if (!PrefabHasClassTextures(prefab))
                    {
                        $"{name}: PronounClassModelPrefabs entry for '{entry.pronounKey}' -> '{prefab.name}' contains no materials exposing class texture properties (_Base/_MSE/_Tint_Mask). Class textures will not be applied.".LogWarning();
                        EditorUtility.SetDirty(this);
                    }
                }
            }
        }
#endif
    }
}

