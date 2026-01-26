using System.Collections.Generic;
using Turnroot.Characters.Stats;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.Characters
{
    [CreateAssetMenu(
        fileName = "DefaultCharacterStats",
        menuName = "Turnroot/Characters/Default Character Stats"
    )]
    public class DefaultCharacterStats : SingletonScriptableObject<DefaultCharacterStats>
    {
        [System.Serializable]
        public class DefaultBoundedStat
        {
            public BoundedStatType StatType;
            public float Max = 100;
            public float Current = 100;
            public float Min = 0;
        }

        [System.Serializable]
        public class DefaultUnboundedStat
        {
            public UnboundedStatType StatType;
            public float Current = 10;
        }

        [SerializeField]
        private List<DefaultBoundedStat> _defaultBoundedStats = new()
        {
            new DefaultBoundedStat
            {
                StatType = BoundedStatType.Health,
                Max = 100,
                Current = 100,
                Min = 0,
            },
        };

        [SerializeField]
        private List<DefaultUnboundedStat> _defaultUnboundedStats = new()
        {
            new DefaultUnboundedStat { StatType = UnboundedStatType.Strength, Current = 10 },
        };

        public List<DefaultBoundedStat> DefaultBoundedStats => _defaultBoundedStats;
        public List<DefaultUnboundedStat> DefaultUnboundedStats => _defaultUnboundedStats;

#if UNITY_EDITOR
        private void OnValidate() => AutoPopulateMissingStats();

        private void AutoPopulateMissingStats()
        {
            bool changed = false;

            // Check for missing bounded stats
            var existingBounded = new HashSet<BoundedStatType>(
                _defaultBoundedStats.ConvertAll(s => s.StatType)
            );
            foreach (BoundedStatType type in System.Enum.GetValues(typeof(BoundedStatType)))
            {
                if (!existingBounded.Contains(type))
                {
                    var (max, current, min) = GetDefaultValuesForBoundedStat(type);
                    _defaultBoundedStats.Add(
                        new DefaultBoundedStat
                        {
                            StatType = type,
                            Max = max,
                            Current = current,
                            Min = min,
                        }
                    );
                    changed = true;
                }
            }

            // Check for missing unbounded stats
            var existingUnbounded = new HashSet<UnboundedStatType>(
                _defaultUnboundedStats.ConvertAll(s => s.StatType)
            );
            foreach (UnboundedStatType type in System.Enum.GetValues(typeof(UnboundedStatType)))
            {
                if (!existingUnbounded.Contains(type))
                {
                    _defaultUnboundedStats.Add(
                        new DefaultUnboundedStat
                        {
                            StatType = type,
                            Current = GetDefaultValueForStat(type),
                        }
                    );
                    changed = true;
                }
            }

            if (changed)
            {
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        private static (float max, float current, float min) GetDefaultValuesForBoundedStat(
            BoundedStatType statType
        )
        {
            return statType switch
            {
                BoundedStatType.Health => (100f, 100f, 0f), // Full health
                BoundedStatType.Level => (99f, 1f, 1f), // Start at level 1
                BoundedStatType.LevelExperience => (100f, 0f, 0f), // Experience starts empty
                BoundedStatType.ClassExperience => (100f, 0f, 0f), // Class experience starts empty
                _ => (100f, 100f, 0f), // Unknown stats default to full (like health)
            };
        }

        private static float GetDefaultValueForStat(UnboundedStatType statType)
        {
            return statType switch
            {
                UnboundedStatType.Luck => 5f,
                UnboundedStatType.CriticalAvoidance => 0f,
                UnboundedStatType.Authority => 5f,
                _ => 10f,
            };
        }
#endif

        /// <summary>
        /// Creates a list of BoundedCharacterStat instances from the default configuration.
        /// </summary>
        public List<BoundedCharacterStat> CreateBoundedStats()
        {
            var stats = new List<BoundedCharacterStat>();
            foreach (var defaultStat in _defaultBoundedStats)
            {
                stats.Add(
                    new BoundedCharacterStat(
                        defaultStat.Max,
                        defaultStat.Current,
                        defaultStat.Min,
                        defaultStat.StatType
                    )
                );
            }
            return stats;
        }

        /// <summary>
        /// Creates a list of CharacterStat instances from the default configuration.
        /// </summary>
        public List<CharacterStat> CreateUnboundedStats()
        {
            var stats = new List<CharacterStat>();
            foreach (var defaultStat in _defaultUnboundedStats)
            {
                stats.Add(new CharacterStat(defaultStat.Current, defaultStat.StatType));
            }
            return stats;
        }
    }
}
