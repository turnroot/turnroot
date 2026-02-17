using System;
using System.Collections.Generic;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Characters.CharacterClass
{
    public partial class CharacterClassData : ScriptableObject
    {
        /// <summary>
        /// Validate class data when modified in editor.
        /// </summary>
        private void OnValidate()
        {
            // Update cached mode so ShowIf can see changes
            _cachedClassSelectionMode = GetProjectClassSelectionMode();

            ValidateStatLists();
            ValidatePromotionPaths();
            ForceInspectorRefresh();
            EnsureUniqueClassName();

            // Validate class visuals (mesh/prefab contain required blendshapes)
            ValidateClassVisuals();
        }

        private OperationResult ValidateStatLists()
        {
            var defaultStats = DefaultCharacterStats.Instance;
            if (defaultStats == null)
            {
                return OperationResult.Failure(
                    $"{name}: Cannot validate stat lists - DefaultCharacterStats not found in GameSettings."
                );
            }

            // Define all bounded stat lists to validate in one place
            var boundedStatLists = new[]
            {
                (list: Stats.StatMinimums, name: nameof(Stats.StatMinimums)),
                (list: Stats.StatCaps, name: nameof(Stats.StatCaps)),
                (list: Stats.StatBonuses, name: nameof(Stats.StatBonuses)),
                (list: Stats.ClassChangeBonuses, name: nameof(Stats.ClassChangeBonuses)),
            };

            // Validate all bounded lists with single loop
            foreach (var (list, name) in boundedStatLists)
            {
                ValidateBoundedStatList(
                    list,
                    defaultStats.DefaultBoundedStats,
                    name,
                    (stat) => new StatModifier(stat.StatType, 0)
                );
            }

            // Define all unbounded stat lists
            var unboundedStatLists = new[]
            {
                (list: Stats.UnboundedStatMinimums, name: nameof(Stats.UnboundedStatMinimums)),
                (list: Stats.UnboundedStatCaps, name: nameof(Stats.UnboundedStatCaps)),
                (list: Stats.UnboundedStatBonuses, name: nameof(Stats.UnboundedStatBonuses)),
                (list: Stats.GrowthRateModifiers, name: nameof(Stats.GrowthRateModifiers)),
                (
                    list: Stats.UnboundedClassChangeBonuses,
                    name: nameof(Stats.UnboundedClassChangeBonuses)
                ),
            };

            // Validate all unbounded lists
            foreach (var (list, name) in unboundedStatLists)
            {
                ValidateUnboundedStatList(
                    list,
                    defaultStats.DefaultUnboundedStats,
                    name,
                    (stat) => new UnboundedStatModifier(stat.StatType, 0)
                );
            }
            return OperationResult.Successful();
        }

        private void ValidateBoundedStatList(
            List<StatModifier> list,
            List<DefaultCharacterStats.DefaultBoundedStat> defaults,
            string listName,
            Func<DefaultCharacterStats.DefaultBoundedStat, StatModifier> creator
        )
        {
            if (list.Count == 0)
            {
                foreach (var stat in defaults)
                {
                    list.Add(creator(stat));
                }
            }
            else if (list.Count != defaults.Count)
            {
                TurnrootLogger.Log(
                    $"{name}: {listName} count ({list.Count}) doesn't match DefaultCharacterStats count ({defaults.Count}). This may cause issues.",
                    TurnrootLogger.LogLevel.Warning
                );
            }
        }

        private void ValidateUnboundedStatList(
            List<UnboundedStatModifier> list,
            List<DefaultCharacterStats.DefaultUnboundedStat> defaults,
            string listName,
            Func<DefaultCharacterStats.DefaultUnboundedStat, UnboundedStatModifier> creator
        )
        {
            if (list.Count == 0)
            {
                foreach (var stat in defaults)
                {
                    list.Add(creator(stat));
                }
            }
            else if (list.Count != defaults.Count)
            {
                TurnrootLogger.Log(
                    $"{name}: {listName} count ({list.Count}) doesn't match DefaultCharacterStats count ({defaults.Count}). This may cause issues.",
                    TurnrootLogger.LogLevel.Warning
                );
            }
        }

        private void ValidatePromotionPaths()
        {
            if (Requirements.PromotionPaths == null || Requirements.PromotionPaths.Count == 0)
            {
                return;
            }

            if (Requirements.PromotionPaths.Contains(this))
            {
                TurnrootLogger.Log(
                    $"{name}: Class cannot have itself in its promotion paths. This creates a cycle.",
                    TurnrootLogger.LogLevel.Warning
                );
            }

            // Check for simple 2-step cycles (A -> B -> A)
            foreach (var promotion in Requirements.PromotionPaths)
            {
                if (
                    promotion != null
                    && promotion.Requirements.PromotionPaths != null
                    && promotion.Requirements.PromotionPaths.Contains(this)
                )
                {
                    TurnrootLogger.Log(
                        $"{name}: Detected circular promotion path with {promotion.Identity.ClassName}. This may cause issues.",
                        TurnrootLogger.LogLevel.Warning
                    );
                }
            }
        }

        private void ForceInspectorRefresh()
        {
            // Force inspector refresh when validating to update ShowIf conditions
            // This ensures promotion/requirement fields show/hide correctly based on GameplayGeneralSettings
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    UnityEditor.EditorUtility.SetDirty(this);
                }
            };
        }

        private void EnsureUniqueClassName()
        {
            if (string.IsNullOrWhiteSpace(Identity?.ClassName))
            {
                return;
            }

            var original = Identity.ClassName.Trim();

            // Search project for other CharacterClassData assets
            var guids = UnityEditor.AssetDatabase.FindAssets("t:CharacterClassData");
            foreach (var g in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                var other = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterClassData>(path);
                if (other == null || other == this)
                {
                    continue;
                }

                var otherName = other.Identity.ClassName;
                if (string.IsNullOrWhiteSpace(otherName))
                {
                    continue;
                }

                if (string.Equals(otherName.Trim(), original, StringComparison.OrdinalIgnoreCase))
                {
                    // Found a conflict. Find a unique candidate by appending an incrementing suffix.
                    int suffix = 1;
                    string candidate;
                    bool exists;
                    do
                    {
                        candidate = $"{original} ({suffix})";
                        exists = false;
                        foreach (var gg in guids)
                        {
                            var p = UnityEditor.AssetDatabase.GUIDToAssetPath(gg);
                            var o = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterClassData>(
                                p
                            );
                            if (o == null)
                            {
                                continue;
                            }

                            var n = o.Identity.ClassName;
                            if (
                                string.Equals(
                                    n?.Trim(),
                                    candidate,
                                    StringComparison.OrdinalIgnoreCase
                                )
                            )
                            {
                                exists = true;
                                break;
                            }
                        }
                        suffix++;
                    } while (exists);

                    Identity.ClassName = candidate;
                    UnityEditor.EditorUtility.SetDirty(this);
                    TurnrootLogger.Log(
                        $"{name}: ClassName '{original}' already exists. Renamed to '{candidate}' to ensure uniqueness.",
                        TurnrootLogger.LogLevel.Warning
                    );
                    return;
                }
            }
        }

        private void ValidateClassVisuals()
        {
            if (Identity == null)
            {
                return;
            }

            // Required blendshape names must match CharacterModelBlendshapeSet.BlendshapeNames
            var required = new string[]
            {
                "ChestSize",
                "WaistSize",
                "HipSize",
                "ThighThickness",
                "ArmThickness",
                "NeckThickness",
            };

            // Helper to validate a mesh for required blendshapes. Returns list of missing blendshape names (empty => ok)
            List<string> ValidateMesh(Mesh mesh, string source)
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

                foreach (var b in required)
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
                        var missing = ValidateMesh(
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
                        if (
                            prefab != null
                            && (
                                prefab.transform.Find("Hair") != null
                                || System.Array.Find(
                                    prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true),
                                    s =>
                                        s != null
                                        && (s.gameObject.name ?? string.Empty).IndexOf(
                                            "hair",
                                            System.StringComparison.OrdinalIgnoreCase
                                        ) >= 0
                                ) != null
                            )
                        )
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
                    foreach (var mat in mats)
                    {
                        if (
                            mat != null
                            && (
                                mat.HasProperty("_Base")
                                || mat.HasProperty("_MSE")
                                || mat.HasProperty("_Tint_Mask")
                            )
                        )
                        {
                            classMatFound = true;
                            break;
                        }
                    }
                    if (classMatFound)
                        break;
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
                        var missing = ValidateMesh(
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
                    if (
                        prefab != null
                        && (
                            prefab.transform.Find("Hair") != null
                            || System.Array.Find(
                                prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true),
                                s =>
                                    s != null
                                    && (s.gameObject.name ?? string.Empty).IndexOf(
                                        "hair",
                                        System.StringComparison.OrdinalIgnoreCase
                                    ) >= 0
                            ) != null
                        )
                    )
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
                            foreach (var mat2 in mats2)
                            {
                                if (
                                    mat2 != null
                                    && (
                                        mat2.HasProperty("_Base")
                                        || mat2.HasProperty("_MSE")
                                        || mat2.HasProperty("_Tint_Mask")
                                    )
                                )
                                {
                                    pronounHasClassMat = true;
                                    break;
                                }
                            }
                            if (pronounHasClassMat)
                                break;
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

            // Hat-specific fields removed — hats should be part of the ClassModelPrefab. ClassModelPrefab and PronounClassModelPrefabs are validated above and must not contain hair.
        }

        // Class/hat/hair rules simplified: class assets must not contain hair (unit hair comes from CharacterData.HairPrefab).
        // Detection uses explicit 'Hair' child checks or renderer name checks.
    }
}
