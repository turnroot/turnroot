using System;
using System.Collections.Generic;
using System.Linq;
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
            ValidateClassVisuals();
#endif
        }

        private OperationResult ValidateStatLists()
        {
            Stats ??= new ClassStats();
            Stats.StatMinimums ??= new List<UnboundedStatModifier>();
            Stats.StatCaps ??= new List<UnboundedStatModifier>();
            Stats.StatBonuses ??= new List<UnboundedStatModifier>();
            Stats.GrowthRateModifiers ??= new List<UnboundedStatModifier>();
            Stats.ClassChangeBonuses ??= new List<UnboundedStatModifier>();

            var classStatTypes = Enum
                .GetValues(typeof(UnboundedStatType))
                .Cast<UnboundedStatType>()
                .Where(statType => statType != UnboundedStatType.Movement);
            ValidateUnboundedStatList(
                Stats.StatMinimums,
                classStatTypes,
                nameof(Stats.StatMinimums),
                statType => new UnboundedStatModifier(statType, 0)
            );
            ValidateUnboundedStatList(
                Stats.StatCaps,
                classStatTypes,
                nameof(Stats.StatCaps),
                statType => new UnboundedStatModifier(statType, 0)
            );
            ValidateUnboundedStatList(
                Stats.StatBonuses,
                classStatTypes,
                nameof(Stats.StatBonuses),
                statType => new UnboundedStatModifier(statType, 0)
            );
            ValidateUnboundedStatList(
                Stats.GrowthRateModifiers,
                classStatTypes,
                nameof(Stats.GrowthRateModifiers),
                statType => new UnboundedStatModifier(statType, 0)
            );
            ValidateUnboundedStatList(
                Stats.ClassChangeBonuses,
                classStatTypes,
                nameof(Stats.ClassChangeBonuses),
                statType => new UnboundedStatModifier(statType, 0)
            );

            // Ensure HP growth modifier exists as a bounded entry
            if (
                !Stats.GrowthRateModifiers.Exists(g =>
                    g.isBounded && g.boundedStatType == BoundedStatType.Health
                )
            )
            {
                Stats.GrowthRateModifiers.Add(
                    new UnboundedStatModifier(BoundedStatType.Health, 0f)
                );
            }

            return OperationResult.Successful();
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

            var seen = new HashSet<UnboundedStatType>();
            list.RemoveAll(modifier =>
                !modifier.isBounded
                && (
                    !defaultList.Contains(modifier.unboundedStatType)
                    || !seen.Add(modifier.unboundedStatType)
                )
            );

            // count only non-bounded entries (HP or other future bounded growths are
            // stored alongside but shouldn't affect the unbounded stat count check)
            int unboundedCount = list.Count(m => !m.isBounded);

            if (unboundedCount == 0)
            {
                foreach (var statType in defaultList)
                {
                    list.Add(creator(statType));
                }
            }
            else if (unboundedCount != defaultList.Count)
            {
                // Add any missing stat types (preserves existing values)
                foreach (var statType in defaultList)
                {
                    if (!list.Exists(m => !m.isBounded && m.unboundedStatType == statType))
                    {
                        list.Add(creator(statType));
                    }
                }
            }

            // after validation, ensure HP growth entry exists regardless of count
            if (!list.Exists(g => g.isBounded && g.boundedStatType == BoundedStatType.Health))
            {
                list.Add(new UnboundedStatModifier(BoundedStatType.Health, 0f));
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
            EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    EditorUtility.SetDirty(this);
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
            var guids = AssetDatabase.FindAssets("t:CharacterClassData");
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var other = AssetDatabase.LoadAssetAtPath<CharacterClassData>(path);
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
                            var p = AssetDatabase.GUIDToAssetPath(gg);
                            var o = AssetDatabase.LoadAssetAtPath<CharacterClassData>(p);
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
                    EditorUtility.SetDirty(this);
                    $"{name}: ClassName '{original}' already exists. Renamed to '{candidate}' to ensure uniqueness.".LogWarning();
                    return;
                }
            }
        }
#endif
    }
}
