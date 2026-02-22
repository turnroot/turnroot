using System;
using System.Collections.Generic;
using Turnroot.Characters.Stats;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
#if UNITY_EDITOR
            ForceInspectorRefresh();
            EnsureUniqueClassName();
#endif

            // Validate class visuals (mesh/prefab contain required blendshapes)
#if UNITY_EDITOR
            this.ValidateClassVisuals();
#endif
        }

        private OperationResult ValidateStatLists()
        {
            var gs = GameplayGeneralSettings.Instance;
            if (gs == null)
            {
                return OperationResult.Failure(
                    $"{name}: Cannot validate stat lists - GameplayGeneralSettings not found in GameSettings."
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
                    gs.GetDefaultBoundedStatTypes(),
                    name,
                    (statType) => new StatModifier(statType, 0)
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
                    gs.GetDefaultUnboundedStatTypes(),
                    name,
                    (statType) => new UnboundedStatModifier(statType, 0)
                );
            }
            return OperationResult.Successful();
        }

        private void ValidateBoundedStatList(
            List<StatModifier> list,
            IEnumerable<BoundedStatType> defaults,
            string listName,
            Func<BoundedStatType, StatModifier> creator
        )
        {
            var defaultList = defaults is ICollection<BoundedStatType> c
                ? c
                : new List<BoundedStatType>(defaults);

            if (list.Count == 0)
            {
                foreach (var statType in defaultList)
                {
                    list.Add(creator(statType));
                }
            }
            else if (list.Count != defaultList.Count)
            {
                $"{name}: {listName} count ({list.Count}) doesn't match project default stat count ({defaultList.Count}). This may cause issues.".LogWarning();
            }
        }

        private void ValidateUnboundedStatList(
            List<UnboundedStatModifier> list,
            IEnumerable<UnboundedStatType> defaults,
            string listName,
            Func<UnboundedStatType, UnboundedStatModifier> creator
        )
        {
            var defaultList = defaults is ICollection<UnboundedStatType> c
                ? c
                : new List<UnboundedStatType>(defaults);

            if (list.Count == 0)
            {
                foreach (var statType in defaultList)
                {
                    list.Add(creator(statType));
                }
            }
            else if (list.Count != defaultList.Count)
            {
                $"{name}: {listName} count ({list.Count}) doesn't match project default stat count ({defaultList.Count}). This may cause issues.".LogWarning();
            }
        }

        private void ValidatePromotionPaths()
        {
            if (Requirements.PromotionPaths == null || Requirements.PromotionPaths.Count == 0)
            {
                return;
            }

            // Warn if project is configured for requirement-based selection but promotion paths were set
            if (
                GetProjectClassSelectionMode()
                == GameplayGeneralSettings.ClassSelectionMode.RequirementBased
            )
            {
                $"{name}: PromotionPaths are configured but project ClassSelection mode is RequirementBased — promotion paths will be ignored at runtime.".LogWarning();
            }

            if (Requirements.PromotionPaths.Contains(this))
            {
                $"{name}: Class cannot have itself in its promotion paths. This creates a cycle.".LogWarning();
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
                    $"{name}: Detected circular promotion path with {promotion.Identity.ClassName}. This may cause issues.".LogWarning();
                }
            }
        }

#if UNITY_EDITOR
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
#endif

#if UNITY_EDITOR
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
                    $"{name}: ClassName '{original}' already exists. Renamed to '{candidate}' to ensure uniqueness.".LogWarning();
                    return;
                }
            }
        }
#endif
    }
}
