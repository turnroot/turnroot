using System;
using UnityEngine;

namespace Turnroot.Characters
{
    /// <summary>
    /// Handles battle statistics tracking for a character instance.
    /// Includes both persistent stats (saved to LTM) and transient per-battle stats.
    /// </summary>
    public partial class CharacterInstance
    {
        #region Battle Statistics

        // Persistent stats (saved to LTM for unique characters)
        [SerializeField]
        private int _totalKills = 0;

        [SerializeField]
        private int _totalBattles = 0;

        // Transient stats (reset each battle, not serialized)
        [NonSerialized]
        private int _turnsAliveThisBattle = 0;

        [NonSerialized]
        private int _combatsThisTurn = 0;

        public int TotalKills => _totalKills;
        public int TotalBattles => _totalBattles;
        public int TurnsAliveThisBattle => _turnsAliveThisBattle;
        public int CombatsThisTurn => _combatsThisTurn;

        public CharacterInstance LastAttacker { get; private set; }

        public enum BattleEmotion
        {
            Neutral,
            Desperate,
            Enraged,
            Cocky,
            Cautious,
        }

        public BattleEmotion CurrentEmotion { get; set; } = BattleEmotion.Neutral;

        public bool LastTurnCollectedTreasure { get; private set; }

        public bool LastTurnKilledEnemy { get; private set; }

        internal void RecordKill() => _totalKills++;

        public void RecordBattleStart()
        {
            _totalBattles++;
            _turnsAliveThisBattle = 0;
        }

        public void IncrementTurnsAlive() => _turnsAliveThisBattle++;

        public void IncrementCombatCount() => _combatsThisTurn++;

        public void ResetTurnStats() => _combatsThisTurn = 0;

        public void ResetBattleStats()
        {
            _turnsAliveThisBattle = 0;
            _combatsThisTurn = 0;
        }

        #endregion
    }
}
