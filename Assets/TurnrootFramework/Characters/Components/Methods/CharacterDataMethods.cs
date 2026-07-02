using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters.CharacterClass;
using Turnroot.Characters.Stats;
using Turnroot.Characters.Subclasses;
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
        public void InvalidatePortraitArrayCache() => InvalidatePortraitLookupCache();

        public void SetAvatarNameAndPronouns(string displayName, string fullName, Pronouns pronouns)
        {
            DisplayName = displayName;
            FullName = fullName;
            CharacterPronouns = pronouns;
        }

        public void SaveDefaults()
        {
            TaggedLayerDefaults.Clear();
            if (Portraits != null)
            {
                CharacterHelpers.ForEachPortraitLayer(
                    Portraits.Select(p => p.Portrait),
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
                    Portraits.Select(p => p.Portrait),
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

        private void EnsureDefaultStatsInitialized()
        {
            BoundedStats ??= new List<BoundedCharacterStat>();
            UnboundedStats ??= new List<CharacterStat>();

            var gs = GameplayGeneralSettings.Instance;
            if (gs == null)
            {
                return;
            }

            if (BoundedStats.Count == 0)
            {
                BoundedStats = gs.CreateDefaultBoundedStats();
            }

            if (UnboundedStats.Count == 0)
            {
                UnboundedStats = gs.CreateDefaultUnboundedStats();
            }
        }

        private void OnEnable()
        {
            EnsureDefaultStatsInitialized();

            // Initialize personal growth rates if empty (set all unbounded stats + HP to 0% growth)
            PersonalGrowthRates ??= new List<UnboundedStatModifier>();
            if (PersonalGrowthRates.Count == 0)
            {
                if (UnboundedStats.Count > 0)
                {
                    foreach (var stat in UnboundedStats)
                    {
                        PersonalGrowthRates.Add(new UnboundedStatModifier(stat.StatType, 0f));
                    }
                }
                // always include an HP growth entry as a bounded stat
                PersonalGrowthRates.Add(new UnboundedStatModifier(BoundedStatType.Health, 0f));
            }
        }

        private void OnValidate()
        {
            EnsureDefaultStatsInitialized();

            // Reset cached portrait array so changes in the inspector are reflected
            _portraitArrayCache = null;

            // warn if asset contains duplicate unbounded stats (should be fixed manually)
            if (UnboundedStats != null)
            {
                var seen = new HashSet<UnboundedStatType>();
                foreach (var s in UnboundedStats)
                {
                    if (s == null)
                    {
                        continue;
                    }

                    if (seen.Contains(s.StatType))
                    {
                        $"CharacterData.OnValidate: duplicate unbounded stat {s.StatType} in {name}".LogWarning();
                        break;
                    }
                    seen.Add(s.StatType);
                }
            }
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

            // guarantee that growth rates list contains HP entry and remove movement entries
            if (PersonalGrowthRates != null)
            {
                // add HP entry if missing
                if (
                    !PersonalGrowthRates.Exists(g =>
                        g.isBounded && g.boundedStatType == BoundedStatType.Health
                    )
                )
                {
                    PersonalGrowthRates.Add(new UnboundedStatModifier(BoundedStatType.Health, 0f));
                }
                // remove any stray movement entries (they're not editable)
                PersonalGrowthRates.RemoveAll(g =>
                    !g.isBounded && g.unboundedStatType == UnboundedStatType.Movement
                );
            }

            // synchronize ExperienceRanks list with available types from settings
            if (Application.isEditor)
            {
                ExperienceRanks ??= new List<ExperienceRank>();

                var gs = GameplayGeneralSettings.Instance;
                if (gs != null)
                {
                    var types = gs.GetAllExperienceTypes();
                    var newList = new List<ExperienceRank>();
                    foreach (var et in types)
                    {
                        var existing = ExperienceRanks.Find(r => r.ExperienceTypeId == et.Name);
                        if (existing != null)
                        {
                            newList.Add(existing);
                        }
                        else
                        {
                            newList.Add(new ExperienceRank(et.Name, "E"));
                        }
                    }
                    ExperienceRanks = newList;
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
