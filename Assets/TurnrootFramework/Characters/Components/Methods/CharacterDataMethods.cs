using System;
using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters.CharacterClass;
using Turnroot.Characters.Components;
using Turnroot.Characters.Stats;
using Turnroot.Characters.Subclasses;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Characters
{
    public partial class CharacterData : ScriptableObject, IHasStats
    {
        public void InvalidatePortraitArrayCache() => InvalidatePortraitLookupCache();

        public void SetAvatarNameAndPronouns(string displayName, string fullName, Pronouns pronouns)
        {
            DisplayName = displayName;
            FullName = fullName;
            CharacterPronouns = pronouns;
        }

        public void SetAvatarAppearanceColors(Color skinColor, Color hairColor, Color eyeColor)
        {
            SkinColor = skinColor;
            _hairColor = hairColor;
            _eyeColor = eyeColor;
            AccentColor1 = hairColor;
            AccentColor2 = eyeColor;
        }

        /// Avatar-only
        public void SetAvatarHairPrefab(GameObject hairPrefab) => HairPrefab = hairPrefab;

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
            // this doesn't have any checks because it assumes that the caller has already verified that the enemy is generic
            var cls = ProgressionLadder.GetClassForTier(tier);
            return cls != null ? cls : StartingClass;
        }

        public List<LoadoutEntry> GetLoadoutForProgression(ProgressionLevel tier) =>
            ProgressionLadder.GetLoadoutForTier(tier);

        /// <summary>
        /// Helper used during instance initialization/persistence to choose an
        /// appropriate starting class.  This will return the ladder's Starter
        /// class when the option is active, falling back to the normal
        /// <see cref="StartingClass" /> if nothing is configured.
        /// </summary>
        public CharacterClassData GetPreferredStartingClass()
        {
            // comment on 97 could trickle here but I'd rather have a check at this point, so
            if (!IsUnique) // not unique is generic
            {
                var pref = GetClassFromProgression(ProgressionLevel.Starter);
                return pref != null ? pref : StartingClass;
            }
            return StartingClass;
        }

        private void EnsureDefaultStatsInitialized()
        {
            EnsureCharacterNames();
            EnsureStatLists();
            EnsureBoundedStats();
            EnsureUnboundedStats();
            EnsurePersonalGrowthRates();
        }

        private void EnsureCharacterNames()
        {
            if (string.IsNullOrWhiteSpace(DisplayName))
            {
                DisplayName = "New Unit";
            }

            if (string.IsNullOrWhiteSpace(FullName))
            {
                FullName = DisplayName;
            }
        }

        private void EnsureStatLists()
        {
            BoundedStats ??= new List<BoundedCharacterStat>();
            UnboundedStats ??= new List<CharacterStat>();
            PersonalGrowthRates ??= new List<UnboundedStatModifier>();
        }

        private void EnsureBoundedStats()
        {
            var gs = GameplayGeneralSettings.Instance;
            if (BoundedStats.Count == 0 && gs != null)
            {
                BoundedStats = gs.CreateDefaultBoundedStats();
            }
        }

        private void EnsureUnboundedStats()
        {
            var seenStats = new HashSet<UnboundedStatType>();
            UnboundedStats.RemoveAll(stat =>
                stat == null
                || !Enum.IsDefined(typeof(UnboundedStatType), stat.StatType)
                || !seenStats.Add(stat.StatType)
            );

            foreach (var statType in Enum.GetValues(typeof(UnboundedStatType)).Cast<UnboundedStatType>())
            {
                StatHelpers.GetOrCreateUnboundedStat(UnboundedStats, statType, FullName);
            }
        }

        private void EnsurePersonalGrowthRates()
        {
            var seenGrowthRates = new HashSet<UnboundedStatType>();
            bool healthGrowthSeen = false;
            PersonalGrowthRates.RemoveAll(rate =>
            {
                if (rate.isBounded)
                {
                    if (rate.boundedStatType != BoundedStatType.Health || healthGrowthSeen)
                    {
                        return true;
                    }

                    healthGrowthSeen = true;
                    return false;
                }

                return rate.unboundedStatType == UnboundedStatType.Movement
                    || !Enum.IsDefined(typeof(UnboundedStatType), rate.unboundedStatType)
                    || !seenGrowthRates.Add(rate.unboundedStatType);
            });

            foreach (var statType in Enum.GetValues(typeof(UnboundedStatType)).Cast<UnboundedStatType>())
            {
                if (statType == UnboundedStatType.Movement)
                {
                    continue;
                }

                if (!PersonalGrowthRates.Any(rate =>
                    !rate.isBounded && rate.unboundedStatType == statType))
                {
                    PersonalGrowthRates.Add(new UnboundedStatModifier(statType, 0f));
                }
            }

            if (!PersonalGrowthRates.Any(rate =>
                rate.isBounded && rate.boundedStatType == BoundedStatType.Health))
            {
                PersonalGrowthRates.Add(new UnboundedStatModifier(BoundedStatType.Health, 0f));
            }
        }

        private void OnEnable() => EnsureDefaultStatsInitialized();

        private void OnValidate()
        {
            EnsureDefaultStatsInitialized();
            _portraitArrayCache = null;
            SynchronizeExperienceRanks();
            WarnAboutUnconfiguredProgression();
        }

        private void SynchronizeExperienceRanks()
        {
            if (!Application.isEditor)
            {
                return;
            }

            ExperienceRanks ??= new List<ExperienceRank>();
            var gs = GameplayGeneralSettings.Instance;
            if (gs == null)
            {
                return;
            }

            var synchronizedRanks = new List<ExperienceRank>();
            foreach (var experienceType in gs.GetAllExperienceTypes())
            {
                if (experienceType == null)
                {
                    continue;
                }

                var existing = ExperienceRanks.Find(rank =>
                    rank != null && rank.ExperienceTypeId == experienceType.Name
                );
                synchronizedRanks.Add(existing ?? new ExperienceRank(experienceType.Name, "E"));
            }

            ExperienceRanks = synchronizedRanks;
        }

        private void WarnAboutUnconfiguredProgression()
        {
            if (IsUnique || Which.Equals(CharacterWhich.NPC))
            {
                return;
            }

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
