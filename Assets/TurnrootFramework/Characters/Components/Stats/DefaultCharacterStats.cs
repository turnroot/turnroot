using System.Collections.Generic;
using Turnroot.Characters.Stats;
using UnityEngine;

namespace Turnroot.Characters
{
    [CreateAssetMenu(
        fileName = "DefaultCharacterStats",
        menuName = "Turnroot/Character/Default Character Stats"
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
