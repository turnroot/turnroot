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

        [NonSerialized]
        private readonly HashSet<string> _targetsAttackedThisTurn = new();

        public int TotalKills => _totalKills;
        public int TotalBattles => _totalBattles;
        public int TurnsAliveThisBattle => _turnsAliveThisBattle;
        public int CombatsThisTurn => _combatsThisTurn;
        public int TargetsAttackedThisTurnCount => _targetsAttackedThisTurn.Count;
        public bool HasAttackedTargetThisTurn => _targetsAttackedThisTurn.Count > 0;

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
            NeedsPersist = true;
        }

        public void RecordBattleStart()
        {
            _totalBattles++;
            _turnsAliveThisBattle = 0;
            NeedsPersist = true;
        }

        public void IncrementTurnsAlive() => _turnsAliveThisBattle++;

        public void IncrementCombatCount() => _combatsThisTurn++;

        public void RecordTargetAttackedThisTurn(CharacterInstance target)
        {
            if (target == null || string.IsNullOrEmpty(target.Id))
            {
                return;
            }

            _targetsAttackedThisTurn.Add(target.Id);
        }

        public void ResetTurnStats()
        {
            _combatsThisTurn = 0;
            _targetsAttackedThisTurn.Clear();
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

        // Combat-scoped skill bonuses — cleared at the start of each combat exchange.
        // Skills using CombatStartsNode write here; the values are added on top of
        // the recalculated base when reading CurrentHit / CurrentAvoid.
        [NonSerialized]
        private float _combatHitBonus;

        [NonSerialized]
        private float _combatAvoidBonus;

        // Adjusts the weapon-triangle advantage/disadvantage percentage for this unit's
        // attacks this exchange (written by AdjustAdvantagePercentsNode).
        [NonSerialized]
        private float _combatWeaponAdvantageBonus;

        public float CurrentHit => _currentHit + _combatHitBonus;
        public float CurrentAvoid => _currentAvoid + _combatAvoidBonus;

        public float CurrentCritical => _currentCritical;

        public void AddHit(float delta) => _currentHit += delta;

        public void AddAvoid(float delta) => _currentAvoid += delta;

        public void AddCritical(float delta) => _currentCritical += delta;

        public float CombatHitBonus => _combatHitBonus;
        public float CombatAvoidBonus => _combatAvoidBonus;
        public float CombatWeaponAdvantageBonus => _combatWeaponAdvantageBonus;

        /// <summary>Adds a combat-scoped hit bonus that is cleared after the combat exchange ends.</summary>
        public void AddCombatHitBonus(float delta) => _combatHitBonus += delta;

        /// <summary>Adds a combat-scoped avoid bonus that is cleared after the combat exchange ends.</summary>
        public void AddCombatAvoidBonus(float delta) => _combatAvoidBonus += delta;

        /// <summary>Adds to the weapon-triangle advantage/disadvantage modifier for this unit's
        /// attacks this exchange (positive = more advantage or less disadvantage).</summary>
        public void AddCombatWeaponAdvantageBonus(float delta) =>
            _combatWeaponAdvantageBonus += delta;

        /// <summary>Resets all combat-scoped bonuses. Called at the start of each combat exchange.</summary>
        public void ClearCombatBonuses()
        {
            _combatHitBonus = 0f;
            _combatAvoidBonus = 0f;
            _combatWeaponAdvantageBonus = 0f;
        }

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
            float statHit = skill * sm + dex * dm + luck * lm;

            var weaponItem = GetEquippedWeapon();
            float weaponHit = weaponItem?.Template?.Hit ?? 0f;

            _currentHit = (statHit + weaponHit) * 0.5f;

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

        /// <summary>
        /// Read-only list of passive skills that have triggered for this unit in the
        /// current battle.  Cleared at the start of each battle and when the unit's
        /// battle stats reset.
        /// </summary>
        public IReadOnlyList<Skills.Skill> ActivePassiveSkills => _activePassiveSkills.AsReadOnly();

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
            _targetsAttackedThisTurn.Clear();
            ClearActivePassiveSkills();
        }

        #endregion
    }
}
