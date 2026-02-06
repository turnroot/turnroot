using UnityEngine;

namespace Turnroot.Characters.Stats
{
    /// <summary>
    /// Represents an unbounded character stat such as Strength, Speed, or Luck.
    /// </summary>
    [System.Serializable]
    public class CharacterStat : BaseCharacterStat
    {
        [SerializeField]
        private UnboundedStatType _statType = UnboundedStatType.Strength;

        // Explicit default constructor to avoid unintentionally assuming other values
        public CharacterStat()
        {
            _statType = UnboundedStatType.Strength;
            _current = 0f;
            _bonus = 0f;
        }

        // Copy constructor used for safe cloning
        public CharacterStat(CharacterStat other)
        {
            if (other == null)
            {
                // Ensure sensible defaults if a null source is provided
                _statType = UnboundedStatType.Strength;
                _current = 0f;
                _bonus = 0f;
                return;
            }
            _statType = other._statType;
            _current = other._current;
            _bonus = other._bonus;
        }

        public CharacterStat(
            float current = 0,
            UnboundedStatType statType = UnboundedStatType.Strength
        )
        {
            _statType = statType;
            _current = current;
            _bonus = 0;
        }

        public UnboundedStatType StatType => _statType;
        public override string Name => _statType.ToString();
        public override string DisplayName => _statType.GetDisplayName();
        public override string Description => _statType.GetDescription();

        public override void SetCurrent(float value) => _current = value;

        // Allow using CharacterStat as an int in code: int value = myStat;
        public static implicit operator int(CharacterStat s) => s?.Get() ?? 0;
    }
}
