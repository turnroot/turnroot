using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters.CharacterClass;
using Turnroot.Characters.Components.Support;
using Turnroot.Characters.Stats;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Characters
{
    public partial class CharacterData : ScriptableObject, IHasStats
    {
        public void InvalidatePortraitArrayCache() => _portraitArrayCache = null;

        public void SaveDefaults()
        {
            TaggedLayerDefaults.Clear();
            if (Portraits != null)
            {
                CharacterHelpers.ForEachPortraitLayer(
                    Portraits,
                    layer =>
                    {
                        if (!string.IsNullOrEmpty(layer.Tag))
                        {
                            TaggedLayerDefaults[layer.Tag] = new TaggedLayerDefault
                            {
                                Tag = layer.Tag,
                                Sprite = layer.Sprite,
                                Offset = layer.Offset,
                                Scale = layer.Scale,
                                Tint = layer.Tint,
                            };
                        }
                    }
                );
            }
        }

        public void LoadDefaults()
        {
            if (Portraits != null)
            {
                CharacterHelpers.ForEachPortraitLayer(
                    Portraits,
                    layer =>
                    {
                        if (
                            !string.IsNullOrEmpty(layer.Tag)
                            && TaggedLayerDefaults.TryGetValue(layer.Tag, out var def)
                        )
                        {
                            layer.Sprite = def.Sprite;
                            layer.Offset = def.Offset;
                            layer.Scale = def.Scale;
                            layer.Tint = def.Tint;
                        }
                    }
                );
            }
            InvalidatePortraitArrayCache();
        }

        // Helper methods to get stats by type
        public BoundedCharacterStat GetBoundedStat(BoundedStatType type) =>
            StatHelpers.GetBoundedStat(BoundedStats, type);

        public CharacterStat GetUnboundedStat(UnboundedStatType type) =>
            StatHelpers.GetUnboundedStat(UnboundedStats, type);

        public ExperienceRank GetExperienceRank(string experienceTypeId) =>
            ExperienceRanks?.Find(e => e.ExperienceTypeId == experienceTypeId);

        /* ----------------------------- Core Functions ----------------------------- */

        private void OnEnable()
        {
            // Load settings from centralized cache
            var settings = CharacterSettings.PrototypeSettings;
            if (settings == null)
            {
                return;
            }

            // Initialize stats from defaults if stats are empty
            if (BoundedStats.Count == 0 && UnboundedStats.Count == 0)
            {
                var defaultStats = CharacterSettings.DefaultStats;
                if (defaultStats != null)
                {
                    BoundedStats = defaultStats.CreateBoundedStats();
                    UnboundedStats = defaultStats.CreateUnboundedStats();
                }
            }

            // Initialize personal growth rates if empty (set all unbounded stats to 0% growth)
            if (PersonalGrowthRates.Count == 0 && UnboundedStats.Count > 0)
            {
                foreach (var stat in UnboundedStats)
                {
                    PersonalGrowthRates.Add(new UnboundedStatModifier(stat.StatType, 0f));
                }
            }
        }

        private void OnValidate()
        {
            // Reset cached portrait array so changes in the inspector are reflected
            _portraitArrayCache = null;
            // Ensure that the character's name is not empty
            if (string.IsNullOrWhiteSpace(DisplayName))
            {
                DisplayName = "New Unit";
            }

            // Ensure that the full name is not empty
            if (string.IsNullOrWhiteSpace(FullName))
            {
                FullName = DisplayName;
            }

            // Validate support relationships - remove any that reference this character
            if (SupportRelationships != null)
            {
                var removed = SupportRelationship.SanitizeForCharacter(this, SupportRelationships);
                foreach (var r in removed)
                {
                    TurnrootLogger.Log(
                        $"Removed invalid support relationship: {name} cannot have a support relationship with themselves ({r.Character?.name})",
                        TurnrootLogger.LogLevel.Warning
                    );
                }
            }

            // Editor-time validation for rigging properties
            if (HasExtraBoneLayer)
            {
                if (AdditionalBonesMask == null)
                {
                    TurnrootLogger.Log(
                        $"{name}: 'HasExtraBoneLayer' is true but 'AdditionalBonesMask' is not set. This may cause Animator layering misconfiguration.",
                        TurnrootLogger.LogLevel.Warning
                    );
                }

                if (
                    (AdditionalBoneNames == null || AdditionalBoneNames.Length == 0)
                    && AdditionalBonesMask == null
                )
                {
                    TurnrootLogger.Log(
                        $"{name}: No additional bone names or AvatarMask were provided for the extra bone layer. Add names or an AvatarMask for tooling/runtime mapping.",
                        TurnrootLogger.LogLevel.Warning
                    );
                }
            }

#if UNITY_EDITOR
            // Validate that assigned prefabs contain a SkinnedMeshRenderer; if not, clear the assignment and log an error.
            ValidatePrefabBlendshapes(HeadAndHandsPrefab, nameof(HeadAndHandsPrefab));
            ValidatePrefabBlendshapes(HairPrefab, nameof(HairPrefab));
            ValidatePrefabBlendshapes(NonBattleOutfitPrefab, nameof(NonBattleOutfitPrefab));

            // Warn if the character defines blendshapes but has no non-battle outfit prefab assigned.
            if (Blendshapes.BlendshapeNames != null && Blendshapes.BlendshapeNames.Length > 0)
            {
                if (NonBattleOutfitPrefab == null)
                {
                    TurnrootLogger.Log(
                        $"{name}: Character has blendshapes defined but no NonBattleOutfitPrefab assigned. Blendshapes are applied only to outfit meshes. Assign a NonBattleOutfitPrefab or ensure class outfits are present.",
                        TurnrootLogger.LogLevel.Warning
                    );
                }
            }

            // Run stats-specific validation defined in another partial file (if present)
            ValidateStats();
#endif
        }

#if UNITY_EDITOR
        private void ValidatePrefabBlendshapes(GameObject prefab, string propertyName)
        {
            if (prefab == null)
            {
                return;
            }

            var smr = prefab.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (smr == null)
            {
                Debug.LogError(
                    $"{name}: Assigned {propertyName} prefab '{prefab.name}' does not contain a SkinnedMeshRenderer. Clearing assignment."
                );
                if (propertyName == nameof(HeadAndHandsPrefab))
                {
                    HeadAndHandsPrefab = null;
                }
                else if (propertyName == nameof(HairPrefab))
                {
                    HairPrefab = null;
                }
                else if (propertyName == nameof(NonBattleOutfitPrefab))
                {
                    NonBattleOutfitPrefab = null;
                }
                return;
            }

            var mesh = smr.sharedMesh;
            if (mesh == null)
            {
                Debug.LogError(
                    $"{name}: Assigned {propertyName} prefab '{prefab.name}' has no sharedMesh. Clearing assignment."
                );
                if (propertyName == nameof(HeadAndHandsPrefab))
                {
                    HeadAndHandsPrefab = null;
                }
                else if (propertyName == nameof(HairPrefab))
                {
                    HairPrefab = null;
                }
                else if (propertyName == nameof(NonBattleOutfitPrefab))
                {
                    NonBattleOutfitPrefab = null;
                }
                return;
            }

            // Only enforce required blendshapes on the non-battle outfit prefab.
            if (propertyName == nameof(NonBattleOutfitPrefab))
            {
                var required = Blendshapes.BlendshapeNames;
                var missingAny = new List<string>();
                var smrs = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if (smrs == null || smrs.Length == 0)
                {
                    Debug.LogError(
                        $"{name}: Assigned NonBattleOutfitPrefab '{prefab.name}' contains no SkinnedMeshRenderer. Clearing assignment."
                    );
                    NonBattleOutfitPrefab = null;
                    return;
                }

                foreach (var childSmr in smrs)
                {
                    var m = childSmr.sharedMesh;
                    if (m == null)
                    {
                        Debug.LogError(
                            $"{name}: NonBattleOutfitPrefab '{prefab.name}' contains a SkinnedMeshRenderer with no sharedMesh. Clearing assignment."
                        );
                        NonBattleOutfitPrefab = null;
                        return;
                    }

                    if (required != null)
                    {
                        var missing = new List<string>();
                        foreach (var n in required)
                        {
                            if (m.GetBlendShapeIndex(n) < 0)
                            {
                                missing.Add(n);
                            }
                        }
                        if (missing.Count > 0)
                        {
                            missingAny.AddRange(
                                missing.Select(x => $"{childSmr.gameObject.name}:{x}")
                            );
                        }
                    }
                }

                if (missingAny.Count > 0)
                {
                    Debug.LogError(
                        $"{name}: Assigned {propertyName} prefab '{prefab.name}' is missing blendshapes on submeshes: {string.Join(", ", missingAny)}. Clearing assignment."
                    );
                    NonBattleOutfitPrefab = null;
                }
            }
        }
#endif
    }
}
