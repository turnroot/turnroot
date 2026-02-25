using System;
using System.Collections.Generic;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
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

        [NonSerialized]
        private int _totalKills = 0;

        [NonSerialized]
        private int _totalBattles = 0;

        [NonSerialized]
        private int _turnsAliveThisBattle = 0;

        [NonSerialized]
        private int _combatsThisTurn = 0;

        public int TotalKills => _totalKills;
        public int TotalBattles => _totalBattles;
        public int TurnsAliveThisBattle => _turnsAliveThisBattle;
        public int CombatsThisTurn => _combatsThisTurn;

        [field: NonSerialized]
        public CharacterInstance LastAttacker { get; private set; }

        internal void SetLastAttacker(CharacterInstance attacker) => LastAttacker = attacker;

        internal void ClearLastAttacker() => LastAttacker = null;

        public enum BattleEmotion
        {
            Neutral,
            Desperate,
            Enraged,
            Cocky,
            Cautious,
        }

        [field: NonSerialized]
        public BattleEmotion CurrentEmotion { get; set; } = BattleEmotion.Neutral;

        public bool LastTurnCollectedTreasure { get; private set; }

        public bool LastTurnKilledEnemy { get; private set; }

        internal void RecordKill()
        {
            _totalKills++;
            LastTurnKilledEnemy = true; // mark that a kill occurred during the current turn
        }

        public void RecordBattleStart()
        {
            _totalBattles++;
            _turnsAliveThisBattle = 0;
        }

        public void IncrementTurnsAlive() => _turnsAliveThisBattle++;

        public void IncrementCombatCount() => _combatsThisTurn++;

        public void ResetTurnStats()
        {
            _combatsThisTurn = 0;
            // Clear per-turn flags so they're only true for the turn in which they occurred
            LastTurnKilledEnemy = false;
            LastTurnCollectedTreasure = false;
        }

        [NonSerialized]
        private float _currentHit;

        [NonSerialized]
        private float _currentAvoid;

        [NonSerialized]
        private float _currentCritical;

        public float CurrentHit => _currentHit;
        public float CurrentAvoid => _currentAvoid;

        public float CurrentCritical => _currentCritical;

        public void AddHit(float delta) => _currentHit += delta;

        public void AddAvoid(float delta) => _currentAvoid += delta;

        public void AddCritical(float delta) => _currentCritical += delta;

        public void RecalculateCombatRates()
        {
            var settings = GameSettings.GameplayGeneralSettings.Instance;
            if (settings == null)
            {
                return;
            }

            float skill = GetUnboundedStat(Stats.UnboundedStatType.Skill)?.Current ?? 0f;
            float dex = GetUnboundedStat(Stats.UnboundedStatType.Dexterity)?.Current ?? 0f;
            float luck = GetUnboundedStat(Stats.UnboundedStatType.Luck)?.Current ?? 0f;
            float speed = GetUnboundedStat(Stats.UnboundedStatType.Speed)?.Current ?? 0f;

            settings.GetHitFormulaMultipliers(out var sm, out var dm, out var lm);
            _currentHit = (skill * sm) + (dex * dm) + (luck * lm);

            var weaponItem = GetEquippedWeapon();
            if (weaponItem?.Template != null)
            {
                _currentHit += weaponItem.Template.Hit;
            }

            settings.GetAvoidFormulaMultipliers(out var spm, out var lkm);
            _currentAvoid = (speed * spm) + (luck * lkm);

            settings.GetCritFormulaMultipliers(out var csm, out var clm);
            _currentCritical = (skill * csm) + (luck * clm);
            if (weaponItem?.Template != null)
            {
                _currentCritical += weaponItem.Template.Critical;
            }
        }

        public float CalculateAvoid(
            BattleContext context,
            GameSettings.GameplayGeneralSettings settings
        )
        {
            if (settings == null)
            {
                settings = GameSettings.GameplayGeneralSettings.Instance;
            }

            settings.GetAvoidFormulaMultipliers(out float speedMult, out float luckMult);

            float avoid = 0f;
            if (!Mathf.Approximately(speedMult, 0f))
            {
                avoid +=
                    (GetUnboundedStat(Stats.UnboundedStatType.Speed)?.Current ?? 0f) * speedMult;
            }

            if (!Mathf.Approximately(luckMult, 0f) && (settings?.UseLuck ?? false))
            {
                avoid += (GetUnboundedStat(Stats.UnboundedStatType.Luck)?.Current ?? 0f) * luckMult;
            }

            if (context?.MapGrid != null)
            {
                var gp = UnitPositionToMapGridPoint(MapGridPosition, context.MapGrid);
                if (gp != null)
                {
                    avoid += DamageCalculator.CalculateTerrainAvoidBonus(this, gp, settings);
                }
            }

            return avoid;
        }

        public float CalculateCritAvoid(GameSettings.GameplayGeneralSettings settings)
        {
            if (settings == null)
            {
                settings = GameSettings.GameplayGeneralSettings.Instance;
            }

            if (settings?.UseSeparateCriticalAvoidance == true)
            {
                return GetUnboundedStat(Stats.UnboundedStatType.CriticalAvoidance)?.Current ?? 0f;
            }
            else if (settings?.UseLuck == true)
            {
                return GetUnboundedStat(Stats.UnboundedStatType.Luck)?.Current ?? 0f;
            }

            return 0f;
        }

        [NonSerialized]
        private List<Skills.Skill> _activePassiveSkills = new();

        public void AddActivePassiveSkill(Skills.Skill skill)
        {
            if (skill != null && !_activePassiveSkills.Contains(skill))
            {
                _activePassiveSkills.Add(skill);
            }
        }

        public void ClearActivePassiveSkills() => _activePassiveSkills.Clear();

        public void ResetBattleStats()
        {
            _turnsAliveThisBattle = 0;
            _combatsThisTurn = 0;
            ClearActivePassiveSkills();
        }

        #endregion
    }
}
