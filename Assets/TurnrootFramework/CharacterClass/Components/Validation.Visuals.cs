using System;
using System.Collections.Generic;
using Turnroot.Characters.Stats;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Characters.CharacterClass
{
    public partial class CharacterClassData : ScriptableObject
    {
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

            // Required blendshape names must match CharacterModelBlendshapeSet.BlendshapeNames

            // Helper: validate a mesh for required blendshapes. Returns list of missing blendshape names (empty => ok)
            List<string> ValidateMeshBlendshapes(Mesh mesh, string source)
            {
                var missing = new List<string>();
                if (mesh == null)
                {
                    TurnrootLogger.Log(
                        $"{name}: {source} has no mesh assigned.",
                        TurnrootLogger.LogLevel.Error
                    );
                    return missing;
                }

                foreach (var b in RequiredBlendshapeNames)
                {
                    if (mesh.GetBlendShapeIndex(b) < 0)
                    {
                        missing.Add(b);
                    }
                }

                if (missing.Count > 0)
                {
                    TurnrootLogger.Log(
                        $"{name}: {source} is missing blendshapes: {string.Join(", ", missing)}",
                        TurnrootLogger.LogLevel.Error
                    );
                }
                return missing;
            }

            // Helper: check whether any material in the array exposes class texture properties
            bool MaterialsExposeClassTextures(Material[] mats)
            {
                if (mats == null)
                {
                    return false;
                }

                foreach (var mat in mats)
                {
                    if (mat == null)
                    {
                        continue;
                    }

                    if (
                        mat.HasProperty("_Base")
                        || mat.HasProperty("_MSE")
                        || mat.HasProperty("_Tint_Mask")
                    )
                    {
                        return true;
                    }
                }
                return false;
            }

            // Helper: detect explicit 'Hair' child or renderer whose name contains 'hair'
            bool PrefabContainsHairRenderer(GameObject prefab)
            {
                if (prefab == null)
                {
                    return false;
                }

                if (prefab.transform.Find("Hair") != null)
                {
                    return true;
                }

                var smrs =
                    prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    ?? new SkinnedMeshRenderer[0];
                return Array.Find(
                        smrs,
                        s =>
                            s != null
                            && (s.gameObject.name ?? string.Empty).IndexOf(
                                "hair",
                                StringComparison.OrdinalIgnoreCase
                            ) >= 0
                    ) != null;
            }

            // Validate prefab if assigned (prefab should contain a SkinnedMeshRenderer)
            if (Identity.ClassModelPrefab != null)
            {
                var prefab = Identity.ClassModelPrefab;
                var smrs = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if (smrs == null || smrs.Length == 0)
                {
                    TurnrootLogger.Log(
                        $"{name}: ClassModelPrefab '{prefab.name}' does not contain a SkinnedMeshRenderer. Clearing assignment.",
                        TurnrootLogger.LogLevel.Error
                    );
                    UnityEditor.Undo.RecordObject(this, "Clear invalid ClassModelPrefab");
                    Identity.ClassModelPrefab = null;
                    UnityEditor.EditorUtility.SetDirty(this);
                }
                else
                {
                    var missingAny = new List<string>();
                    foreach (var smr in smrs)
                    {
                        var missing = ValidateMeshBlendshapes(
                            smr.sharedMesh,
                            $"ClassModelPrefab '{prefab.name}' - {smr.gameObject.name}"
                        );
                        if (missing.Count > 0)
                        {
                            missingAny.AddRange(missing);
                        }
                    }
                    if (missingAny.Count > 0)
                    {
                        TurnrootLogger.Log(
                            $"{name}: ClassModelPrefab '{prefab.name}' is missing required blendshapes on submeshes: {string.Join(", ", missingAny)}. Clearing assignment.",
                            TurnrootLogger.LogLevel.Error
                        );
                        UnityEditor.Undo.RecordObject(this, "Clear invalid ClassModelPrefab");
                        Identity.ClassModelPrefab = null;
                        UnityEditor.EditorUtility.SetDirty(this);
                    }
                    else
                    {
                        // Enforce: class model prefabs must not include hair. Clear assignment if a 'Hair' renderer exists.
                        if (PrefabContainsHairRenderer(prefab))
                        {
                            TurnrootLogger.Log(
                                $"{name}: ClassModelPrefab '{prefab.name}' contains a 'Hair' renderer. Class models must not include hair; clearing assignment.",
                                TurnrootLogger.LogLevel.Error
                            );
                            UnityEditor.Undo.RecordObject(this, "Clear invalid ClassModelPrefab");
                            Identity.ClassModelPrefab = null;
                            UnityEditor.EditorUtility.SetDirty(this);
                        }
                    }
                }

                // Warn if none of the renderer materials expose class texture properties (_Base/_MSE/_Tint_Mask)
                bool classMatFound = false;
                var classSmrs =
                    prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    ?? new SkinnedMeshRenderer[0];
                foreach (var smr in classSmrs)
                {
                    var mats = smr.sharedMaterials ?? new Material[0];
                    if (MaterialsExposeClassTextures(mats))
                    {
                        classMatFound = true;
                        break;
                    }
                }
                if (!classMatFound)
                {
                    TurnrootLogger.Log(
                        $"{name}: ClassModelPrefab '{prefab.name}' contains no materials exposing class texture properties (_Base/_MSE/_Tint_Mask). Class textures will not be applied at runtime.",
                        TurnrootLogger.LogLevel.Warning
                    );
                    UnityEditor.EditorUtility.SetDirty(this);
                }
            }

            // Validate any pronoun-specific class model prefabs (same rules as ClassModelPrefab)
            if (Identity.PronounClassModelPrefabs != null)
            {
                foreach (var pp in Identity.PronounClassModelPrefabs)
                {
                    if (pp.prefab == null)
                    {
                        continue;
                    }

                    var prefab = pp.prefab;
                    var smrs = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                    if (smrs == null || smrs.Length == 0)
                    {
                        TurnrootLogger.Log(
                            $"{name}: PronounClassModelPrefabs entry for '{pp.pronounKey}' points to prefab '{prefab.name}' which does not contain a SkinnedMeshRenderer. Clearing that entry.",
                            TurnrootLogger.LogLevel.Error
                        );
                        UnityEditor.Undo.RecordObject(
                            this,
                            $"Clear invalid PronounClassModelPrefabs[{pp.pronounKey}]"
                        );
                        // Cannot clear struct array element automatically here; notify author and leave for manual fix in inspector
                        UnityEditor.EditorUtility.SetDirty(this);
                        continue;
                    }

                    var missingAny = new List<string>();
                    foreach (var smr in smrs)
                    {
                        var missing = ValidateMeshBlendshapes(
                            smr.sharedMesh,
                            $"PronounClassModelPrefabs '{pp.pronounKey}' -> '{prefab.name}' - {smr.gameObject.name}"
                        );
                        if (missing.Count > 0)
                        {
                            missingAny.AddRange(missing);
                        }
                    }

                    if (missingAny.Count > 0)
                    {
                        TurnrootLogger.Log(
                            $"{name}: PronounClassModelPrefabs entry for '{pp.pronounKey}' -> '{prefab.name}' is missing required blendshapes on submeshes: {string.Join(", ", missingAny)}. Clearing that entry.",
                            TurnrootLogger.LogLevel.Error
                        );
                        UnityEditor.Undo.RecordObject(
                            this,
                            $"Clear invalid PronounClassModelPrefabs[{pp.pronounKey}]"
                        );
                        // Cannot clear struct array element automatically here; notify author and leave for manual fix in inspector
                        UnityEditor.EditorUtility.SetDirty(this);
                    }

                    // Recommend: prefer a dedicated child renderer named 'Hair' for hair meshes so the runtime
                    // preserves hair materials and avoids relying on material-name heuristics.
                    if (PrefabContainsHairRenderer(prefab))
                    {
                        TurnrootLogger.Log(
                            $"{name}: PronounClassModelPrefabs entry for '{pp.pronounKey}' -> '{prefab.name}' contains a 'Hair' renderer. Pronoun-specific class models must not include hair.",
                            TurnrootLogger.LogLevel.Error
                        );
                        UnityEditor.Undo.RecordObject(
                            this,
                            $"PronounClassModelPrefabs contains invalid hair renderer [{pp.pronounKey}]"
                        );
                        // Notify author for manual fix in inspector (cannot auto-clear struct array element reliably)
                        UnityEditor.EditorUtility.SetDirty(this);

                        // Warn if none of the renderer materials expose class texture properties (_Base/_MSE/_Tint_Mask)
                        bool pronounHasClassMat = false;
                        foreach (var smr2 in smrs)
                        {
                            var mats2 = smr2.sharedMaterials ?? new Material[0];
                            if (MaterialsExposeClassTextures(mats2))
                            {
                                pronounHasClassMat = true;
                                break;
                            }
                        }
                        if (!pronounHasClassMat)
                        {
                            TurnrootLogger.Log(
                                $"{name}: PronounClassModelPrefabs entry for '{pp.pronounKey}' -> '{prefab.name}' contains no materials exposing class texture properties (_Base/_MSE/_Tint_Mask). Class textures will not be applied.",
                                TurnrootLogger.LogLevel.Warning
                            );
                            UnityEditor.EditorUtility.SetDirty(this);
                        }
                    }
                }
            }
        }
    }
}
