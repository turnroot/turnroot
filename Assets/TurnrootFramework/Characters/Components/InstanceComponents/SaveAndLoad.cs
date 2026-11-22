using System;
using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters.Stats;
using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Characters
{
    /// <summary>
    /// Runtime instance of a character containing all state and behavior.
    /// This partial class contains stat persistence and long-term memory integration.
    /// </summary>
    public partial class CharacterInstance : Serialization.IPostDeserialize, IHasStats
    {
        private static Brain CurrentBrain => Brain.Instance;

        #region Stat Persistence

        // DTOs for serializing stats into LTM
        /// <summary>
        /// Data transfer object for serializing bounded character stats to long-term memory.
        /// </summary>
        [Serializable]
        private class BoundedStatDto
        {
            public string StatType;
            public float Max;
            public float Current;
            public float Min;
        }

        /// <summary>
        /// Data transfer object for serializing unbounded character stats to long-term memory.
        /// </summary>
        [Serializable]
        private class UnboundedStatDto
        {
            public string StatType;
            public float Current;
        }

        /// <summary>
        /// Data transfer object containing all character stats for serialization to long-term memory.
        /// </summary>
        [Serializable]
        private class CharacterInstanceStatsDto
        {
            public BoundedStatDto[] BoundedStats;
            public UnboundedStatDto[] UnboundedStats;
        }

        [NonSerialized]
        private bool _deferredPersistRegistered = false;

        #endregion
    }
}
