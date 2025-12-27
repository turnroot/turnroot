using System.Collections.Generic;
using Turnroot.Characters.Stats;
using Turnroot.Gameplay.Objects.Components;
using Turnroot.GameSettings;

namespace Turnroot.Characters
{
    /// <summary>
    /// Interface for character information needed by AI systems.
    /// Decouples AI logic from direct CharacterClassData dependencies.
    /// </summary>
    public interface ICharacterAIData
    {
        MovementType MovementType { get; }

        int Movement { get; }

        bool IsMagic { get; }

        (int min, int max) AttackRange { get; }

        int GetStat(UnboundedStatType statType);

        Dictionary<string, float> BehaviorSettings { get; }
        bool CanUseWeaponType(WeaponType weaponType);
    }

    /// <summary>
    /// Extension methods to convert CharacterInstance to AI data interface.
    /// </summary>
    public static class CharacterAIDataExtensions
    {
        public static ICharacterAIData ToAIData(this CharacterInstance character) =>
            character == null
                ? null
                : (ICharacterAIData)new CharacterInstanceAIDataAdapter(character);

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
