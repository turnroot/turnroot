using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Environment;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Locations;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Context
{
    /// <summary>
    /// Focused context for combat resolution.
    /// Contains data needed for damage calculations, hit chances, etc.
    /// </summary>
    public readonly struct CombatContext
    {
        private readonly BattleContext _context;

        public CombatContext(BattleContext context)
        {
            _context = context;
        }

        #region Combatants

        /// <summary>
        /// The attacking unit.
        /// </summary>
        public CharacterInstance Attacker => _context.UnitInstance;

        /// <summary>
        /// The defending unit (primary target).
        /// </summary>
        public CharacterInstance Defender =>
            _context.Targets != null && _context.Targets.Count > 0 ? _context.Targets[0] : null;

        /// <summary>
        /// All targets of the attack.
        /// </summary>
        public IReadOnlyList<CharacterInstance> AllTargets => _context.Targets;

        #endregion

        #region Environment

        /// <summary>
        /// Environmental conditions affecting combat.
        /// </summary>
        public EnvironmentalConditions Environment => _context.EnvironmentalConditions;

        /// <summary>
        /// The map grid for terrain calculations.
        /// </summary>
        public MapGrid MapGrid => _context.mapGrid;

        #endregion

        #region Combat State

        /// <summary>
        /// Whether the current attack is a critical hit.
        /// </summary>
        public bool IsCriticalHit => _context.IsCriticalHit;

        /// <summary>
        /// The unit that scored a critical hit (if any).
        /// </summary>
        public CharacterInstance CriticalHitUnit => _context.CriticalHitUnit;

        /// <summary>
        /// Sets this attack as a critical hit.
        /// </summary>
        public void SetCriticalHit(CharacterInstance unit)
        {
            _context.IsCriticalHit = true;
            _context.CriticalHitUnit = unit;
        }

        /// <summary>
        /// Clears the critical hit state.
        /// </summary>
        public void ClearCriticalHit()
        {
            _context.IsCriticalHit = false;
            _context.CriticalHitUnit = null;
        }

        #endregion

        #region Weapon Triangle / Advantage

        /// <summary>
        /// Gets the attacker's equipped weapon.
        /// </summary>
        public Objects.ObjectItemInstance AttackerWeapon => GetEquippedWeapon(Attacker);

        /// <summary>
        /// Gets the defender's equipped weapon.
        /// </summary>
        public Objects.ObjectItemInstance DefenderWeapon => GetEquippedWeapon(Defender);

        private static Objects.ObjectItemInstance GetEquippedWeapon(CharacterInstance character)
        {
            var inventory = character?.InventoryInstance;
            if (inventory == null)
            {
                return null;
            }

            int weaponIndex = inventory.GetEquippedWeaponIndex();
            if (weaponIndex < 0 || weaponIndex >= inventory.InventoryItems.Count)
            {
                return null;
            }
            return inventory.InventoryItems[weaponIndex];
        }

        #endregion

        #region Adjacency

        /// <summary>
        /// Units adjacent to the attacker.
        /// </summary>
        public Adjacency AdjacentUnits => _context.AdjacentUnits;

        #endregion

        #region Custom Combat Data

        /// <summary>
        /// Gets a custom combat data value.
        /// </summary>
        public T GetCombatData<T>(string key, T defaultValue = default) =>
            _context.GetCustomData($"combat_{key}", defaultValue);

        /// <summary>
        /// Sets a custom combat data value.
        /// </summary>
        public void SetCombatData(string key, object value) =>
            _context.SetCustomData($"combat_{key}", value);

        #endregion

        /// <summary>
        /// Gets the underlying BattleContext for cases where full access is needed.
        /// </summary>
        public BattleContext GetFullContext() => _context;
    }
}
