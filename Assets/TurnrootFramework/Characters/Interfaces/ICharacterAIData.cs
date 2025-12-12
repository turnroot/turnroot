using System.Collections.Generic;
using Turnroot.Characters.Stats;
using Turnroot.Gameplay.Objects.Components;

namespace Turnroot.Characters
{
    /// <summary>
    /// Interface for character information needed by AI systems.
    /// Decouples AI logic from direct CharacterClassData dependencies.
    /// </summary>
    public interface ICharacterAIData
    {
        /// <summary>
        /// Movement type for pathfinding calculations.
        /// </summary>
        MovementType MovementType { get; }

        /// <summary>
        /// Base movement range in tiles.
        /// </summary>
        int Movement { get; }

        /// <summary>
        /// Whether the character uses magic.
        /// </summary>
        bool IsMagic { get; }

        /// <summary>
        /// Character's attack range (min-max).
        /// </summary>
        (int min, int max) AttackRange { get; }

        /// <summary>
        /// Get a specific stat value by type.
        /// </summary>
        int GetStat(UnboundedStatType statType);

        /// <summary>
        /// Character's behavioral tendencies for AI decision-making.
        /// </summary>
        Dictionary<string, float> BehaviorSettings { get; }

        /// <summary>
        /// Whether the character can use a specific weapon type.
        /// </summary>
        bool CanUseWeaponType(WeaponType weaponType);
    }

    /// <summary>
    /// Extension methods to convert CharacterInstance to AI data interface.
    /// </summary>
    public static class CharacterAIDataExtensions
    {
        /// <summary>
        /// Creates an AI data wrapper for a character instance.
        /// </summary>
        public static ICharacterAIData ToAIData(this CharacterInstance character)
        {
            if (character == null)
            {
                return null;
            }

            return new CharacterInstanceAIDataAdapter(character);
        }

        private class CharacterInstanceAIDataAdapter : ICharacterAIData
        {
            private readonly CharacterInstance _character;

            public CharacterInstanceAIDataAdapter(CharacterInstance character)
            {
                _character = character;
            }

            public MovementType MovementType =>
                _character.CurrentClass?.ClassData?.Identity.MovementType ?? MovementType.Infantry;

            public int Movement =>
                _character.GetUnboundedStat(UnboundedStatType.Movement)?.Get() ?? 0;

            public bool IsMagic => _character.CurrentClass?.ClassData?.Identity.IsMagic ?? false;

            public (int min, int max) AttackRange
            {
                get
                {
                    var min = _character.GetMinRange();
                    var max = _character.GetMaxRange();
                    return (min, max);
                }
            }

            public int GetStat(UnboundedStatType statType) =>
                _character.GetUnboundedStat(statType)?.Get() ?? 0;

            public Dictionary<string, float> BehaviorSettings =>
                _character.CharacterTemplate?.BehaviorSettings.GetBehaviorDictionary()
                ?? new Dictionary<string, float>();

            public bool CanUseWeaponType(WeaponType weaponType) =>
                _character.CanEquipWeaponType(weaponType);
        }
    }
}
