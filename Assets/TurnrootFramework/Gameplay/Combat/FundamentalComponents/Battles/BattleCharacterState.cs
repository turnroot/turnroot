using System;
using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Characters.Components;
using Turnroot.Characters.Stats;
using Turnroot.Characters.StatusEffects;
using Turnroot.Gameplay.Objects;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    /// <summary>
    /// Wrapper that combines a CharacterInstance with temporary battle-only state.
    /// This provides a single source of truth for a character's state during battle.
    /// </summary>
    public class BattleCharacterState
    {
        #region Wrapped Instance

        /// <summary>
        /// The underlying character instance.
        /// Prefer using BattleCharacterState properties over accessing this directly.
        /// </summary>
        public CharacterInstance Instance { get; }

        #endregion

        #region Position

        /// <summary>
        /// Current position on the battle map.
        /// </summary>
        public Vector2Int Position { get; set; }

        /// <summary>
        /// Position at the start of the turn.
        /// </summary>
        public Vector2Int TurnStartPosition { get; private set; }

        /// <summary>
        /// Whether the unit has moved this turn.
        /// </summary>
        public bool HasMoved => Position != TurnStartPosition;

        #endregion

        #region Health

        private int _battleHP;
        private int _battleMaxHP;

        /// <summary>
        /// Current HP during battle (may differ from CharacterInstance HP).
        /// </summary>
        public int CurrentHP
        {
            get => _battleHP;
            private set => _battleHP = Mathf.Clamp(value, 0, _battleMaxHP);
        }

        /// <summary>
        /// Maximum HP during battle (may be modified by buffs/debuffs).
        /// </summary>
        public int MaxHP
        {
            get => _battleMaxHP;
            private set
            {
                _battleMaxHP = Mathf.Max(1, value);
                // Clamp current HP if max decreased
                if (_battleHP > _battleMaxHP)
                {
                    _battleHP = _battleMaxHP;
                }
            }
        }

        /// <summary>
        /// HP as a percentage (0-1).
        /// </summary>
        public float HPPercentage => _battleMaxHP > 0 ? (float)_battleHP / _battleMaxHP : 0f;

        /// <summary>
        /// Whether this unit is alive.
        /// </summary>
        public bool IsAlive => _battleHP > 0;

        #endregion

        #region Action State

        /// <summary>
        /// Whether the unit has acted this turn.
        /// </summary>
        public bool HasActed { get; private set; }

        /// <summary>
        /// Whether the unit is exhausted (cannot act).
        /// </summary>
        public bool IsExhausted { get; set; }

        /// <summary>
        /// Whether the unit can still take actions.
        /// </summary>
        public bool CanAct => !HasActed && !IsExhausted && IsAlive;

        /// <summary>
        /// Number of additional turns granted to this unit.
        /// </summary>
        public int BonusTurns { get; private set; }

        #endregion

        #region Combat State

        /// <summary>
        /// Whether this unit is currently engaged in combat.
        /// </summary>
        public bool IsInCombat { get; set; }

        /// <summary>
        /// Whether this unit is retreating.
        /// </summary>
        public bool IsRetreating { get; set; }

        /// <summary>
        /// Whether this unit has been defeated.
        /// </summary>
        public bool IsDefeated { get; private set; }

        /// <summary>
        /// Turn number when this unit was defeated.
        /// </summary>
        public int DefeatedOnTurn { get; private set; }

        #endregion

        #region Stat Modifiers

        private readonly Dictionary<UnboundedStatType, int> _statModifiers = new();

        /// <summary>
        /// Gets the current value of an unbounded stat including battle modifiers.
        /// </summary>
        public int GetStat(UnboundedStatType statType)
        {
            var stat = Instance.GetUnboundedStat(statType);
            int baseStat = stat?.Get() ?? 0;
            int modifier = _statModifiers.TryGetValue(statType, out int mod) ? mod : 0;
            return baseStat + modifier;
        }

        /// <summary>
        /// Adds a temporary stat modifier for this battle.
        /// </summary>
        public void AddStatModifier(UnboundedStatType statType, int amount)
        {
            if (!_statModifiers.ContainsKey(statType))
            {
                _statModifiers[statType] = 0;
            }
            _statModifiers[statType] += amount;
        }

        /// <summary>
        /// Removes all stat modifiers.
        /// </summary>
        public void ClearStatModifiers() => _statModifiers.Clear();

        #endregion

        #region Status Effects (Battle-Specific Tracking)

        private readonly List<StatusEffectInstance> _battleStatusEffects = new();

        /// <summary>
        /// Active status effects during this battle.
        /// </summary>
        public IReadOnlyList<StatusEffectInstance> StatusEffects => _battleStatusEffects;

        /// <summary>
        /// Adds a status effect for battle tracking.
        /// </summary>
        public void AddStatusEffect(StatusEffectInstance effect)
        {
            _battleStatusEffects.Add(effect);
        }

        /// <summary>
        /// Removes a status effect.
        /// </summary>
        public bool RemoveStatusEffect(StatusEffectInstance effect)
        {
            return _battleStatusEffects.Remove(effect);
        }

        /// <summary>
        /// Checks if unit has a specific status effect by effect type ID.
        /// </summary>
        public bool HasStatusEffect(string effectTypeId)
        {
            foreach (var effect in _battleStatusEffects)
            {
                if (effect.EffectType?.Id == effectTypeId)
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new BattleCharacterState wrapping a CharacterInstance.
        /// </summary>
        /// <param name="instance">The character instance to wrap.</param>
        /// <param name="startPosition">Starting position on the battle map.</param>
        public BattleCharacterState(CharacterInstance instance, Vector2Int startPosition)
        {
            Instance = instance ?? throw new ArgumentNullException(nameof(instance));
            Position = startPosition;
            TurnStartPosition = startPosition;

            // Initialize HP from instance's bounded health stat
            var healthStat = instance.GetBoundedStat(BoundedStatType.Health);
            if (healthStat != null)
            {
                _battleMaxHP = healthStat.MaxInt;
                _battleHP = healthStat.GetCurrent();
            }
            else
            {
                // Fallback if no health stat exists
                _battleMaxHP = 1;
                _battleHP = 1;
            }
        }

        #endregion

        #region Actions

        /// <summary>
        /// Applies damage to this unit.
        /// </summary>
        /// <returns>The actual damage dealt (after clamping).</returns>
        public int TakeDamage(int damage)
        {
            if (damage <= 0)
                return 0;

            int previousHP = _battleHP;
            CurrentHP -= damage;
            return previousHP - _battleHP;
        }

        /// <summary>
        /// Heals this unit.
        /// </summary>
        /// <returns>The actual HP restored (after clamping).</returns>
        public int Heal(int amount)
        {
            if (amount <= 0)
                return 0;

            int previousHP = _battleHP;
            CurrentHP += amount;
            return _battleHP - previousHP;
        }

        /// <summary>
        /// Marks the unit as having acted this turn.
        /// </summary>
        public void MarkActed() => HasActed = true;

        /// <summary>
        /// Marks the unit as defeated.
        /// </summary>
        /// <param name="turnNumber">The current turn number.</param>
        public void MarkDefeated(int turnNumber = 0)
        {
            IsDefeated = true;
            DefeatedOnTurn = turnNumber;
            _battleHP = 0;
        }

        /// <summary>
        /// Grants a bonus turn to this unit.
        /// </summary>
        public void GrantBonusTurn() => BonusTurns++;

        /// <summary>
        /// Consumes a bonus turn.
        /// </summary>
        /// <returns>True if a bonus turn was available and consumed.</returns>
        public bool ConsumeBonusTurn()
        {
            if (BonusTurns <= 0)
                return false;
            BonusTurns--;
            return true;
        }

        #endregion

        #region Turn Management

        /// <summary>
        /// Called at the start of each turn to reset turn-specific state.
        /// </summary>
        public void BeginTurn()
        {
            TurnStartPosition = Position;
            HasActed = false;
            IsExhausted = false;
        }

        /// <summary>
        /// Called at the end of each turn.
        /// </summary>
        public void EndTurn()
        {
            // Tick down status effect durations using the proper method
            var expiredEffects = new List<StatusEffectInstance>();
            foreach (var effect in _battleStatusEffects)
            {
                // TickDuration returns true if the effect expired
                if (effect.TickDuration())
                {
                    expiredEffects.Add(effect);
                }
            }

            foreach (var effect in expiredEffects)
            {
                _battleStatusEffects.Remove(effect);
            }
        }

        #endregion

        #region Battle End

        /// <summary>
        /// Commits battle results back to the underlying CharacterInstance.
        /// Call this at the end of battle to persist HP changes, etc.
        /// </summary>
        public void CommitToPersistentState()
        {
            // Apply final HP to instance's health stat
            var healthStat = Instance.GetBoundedStat(BoundedStatType.Health);
            healthStat?.SetCurrent(_battleHP);

            // Clear temporary battle state
            ClearStatModifiers();
            _battleStatusEffects.Clear();
        }

        /// <summary>
        /// Discards all battle changes without persisting.
        /// Call this if battle is cancelled or player retreats.
        /// </summary>
        public void DiscardBattleChanges()
        {
            // Restore HP from instance's health stat
            var healthStat = Instance.GetBoundedStat(BoundedStatType.Health);
            if (healthStat != null)
            {
                _battleHP = healthStat.GetCurrent();
                _battleMaxHP = healthStat.MaxInt;
            }

            // Clear temporary state
            ClearStatModifiers();
            _battleStatusEffects.Clear();
        }

        #endregion

        #region Convenience Properties (Delegated to Instance)

        /// <summary>
        /// The character's display name.
        /// </summary>
        public string DisplayName => Instance.CharacterTemplate?.DisplayName ?? "Unknown";

        /// <summary>
        /// The character's level.
        /// </summary>
        public int Level => Instance.CurrentLevel;

        /// <summary>
        /// The character's faction/team identifier (ALLY, ENEMY, NPC, AVATAR).
        /// </summary>
        public string Which => Instance.CharacterTemplate?.Which?.Value ?? CharacterWhich.NPC;

        /// <summary>
        /// Whether this unit is on the player's team.
        /// </summary>
        public bool IsPlayerUnit => Which is CharacterWhich.ALLY or CharacterWhich.AVATAR;

        /// <summary>
        /// Whether this unit is an enemy.
        /// </summary>
        public bool IsEnemyUnit => Which == CharacterWhich.ENEMY;

        /// <summary>
        /// Gets the equipped weapon from the character's inventory.
        /// </summary>
        public ObjectItemInstance EquippedWeapon
        {
            get
            {
                var inventory = Instance.InventoryInstance;
                if (inventory == null)
                    return null;

                int weaponIndex = inventory.GetEquippedWeaponIndex();
                if (weaponIndex < 0 || weaponIndex >= inventory.InventoryItems.Count)
                {
                    return null;
                }
                return inventory.InventoryItems[weaponIndex];
            }
        }

        #endregion
    }
}
