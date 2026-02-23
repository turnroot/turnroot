using System.Collections.Generic;
using Turnroot.Characters.CharacterClass;
using Turnroot.Characters.Components.Support;
using Turnroot.Characters.Stats;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Characters
{
    /// <summary>
    /// CharacterData methods for portrait defaults, skill management, and support relationships.
    /// </summary>
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

        public CharacterClassData GetClassFromProgression(ProgressionLevel tier)
        {
            if (UseClassProgressionLadder)
            {
                var cls = ProgressionLadder.GetClassForTier(tier);
                if (cls != null)
                {
                    return cls;
                }
            }
            return StartingClass;
        }

        public List<LoadoutEntry> GetLoadoutForProgression(ProgressionLevel tier) =>
            UseClassProgressionLadder ? ProgressionLadder.GetLoadoutForTier(tier) : null;

        /// <summary>
        /// Helper used during instance initialization/persistence to choose an
        /// appropriate starting class.  This will return the ladder's Starter
        /// class when the option is active, falling back to the normal
        /// <see cref="StartingClass" /> if nothing is configured.
        /// </summary>
        public CharacterClassData GetPreferredStartingClass()
        {
            var pref = GetClassFromProgression(ProgressionLevel.Starter);
            return pref != null ? pref : StartingClass;
        }

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
                var gs = GameplayGeneralSettings.Instance;
                if (gs != null)
                {
                    BoundedStats = gs.CreateDefaultBoundedStats();
                    UnboundedStats = gs.CreateDefaultUnboundedStats();
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
                    $"Removed invalid support relationship: {name} cannot have a support relationship with themselves ({r.Character?.name})".LogWarning();
                }
            }

            // Editor-time validation for rigging properties
            if (HasExtraBoneLayer)
            {
                if (AdditionalBonesMask == null)
                {
                    $"{name}: 'HasExtraBoneLayer' is true but 'AdditionalBonesMask' is not set. This may cause Animator layering misconfiguration.".LogWarning();
                }

                if (
                    (AdditionalBoneNames == null || AdditionalBoneNames.Length == 0)
                    && AdditionalBonesMask == null
                )
                {
                    $"{name}: No additional bone names or AvatarMask were provided for the extra bone layer. Add names or an AvatarMask for tooling/runtime mapping.".LogWarning();
                }
            }

            if (UseClassProgressionLadder && IsUnique)
            {
                UseClassProgressionLadder = false;
                $"{name}: Unique characters cannot use a class progression ladder; disabling the option.".LogWarning();
            }

            if (UseClassProgressionLadder)
            {
                if (
                    ProgressionLadder.Starter.Class == null
                    && ProgressionLadder.Base.Class == null
                    && ProgressionLadder.Advanced.Class == null
                    && ProgressionLadder.Master.Class == null
                    && ProgressionLadder.Expert.Class == null
                )
                {
                    $"{name}: progression ladder enabled but no classes are assigned".LogWarning();
                }
            }
        }
    }
}
