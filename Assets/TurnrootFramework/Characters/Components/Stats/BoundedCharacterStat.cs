using UnityEngine;

namespace Turnroot.Characters.Stats
{
    /// <summary>
    /// Bounded stat - clamped between min and max values with progress bar visualization
    /// </summary>
    [System.Serializable]
    public class BoundedCharacterStat : BaseCharacterStat
    {
        [SerializeField]
        private BoundedStatType _statType = BoundedStatType.Health;

        [SerializeField]
        private float _max = 100f;

        [SerializeField]
        private float _min;

        public BoundedCharacterStat()
        {
            _current = 100f;
        }

        // Copy constructor - preserves all fields
        public BoundedCharacterStat(BoundedCharacterStat other)
        {
            if (other == null)
            {
                return;
            }
            _statType = other._statType;
            _max = other._max;
            _min = other._min;
            _current = other._current;
            _bonus = other._bonus;
        }

        public BoundedCharacterStat(
            float max,
            float current = -1,
            float min = 0,
            BoundedStatType statType = BoundedStatType.Health
        )
        {
            _statType = statType;
            _max = max;
            _min = min;
            _current = current < 0 ? max : Mathf.Clamp(current, min, max);
            _bonus = 0;
        }

        public BoundedStatType StatType => _statType;
        public override string Name => _statType.ToString();
        public override string DisplayName => _statType.GetDisplayName();
        public override string Description => _statType.GetDescription();
        public override float Current => Mathf.Round(Mathf.Clamp(_current, _min, _max));
        public override float Bonus => Mathf.Round(_bonus);
        public float Max => Mathf.Round(_max);
        public float Min => Mathf.Round(_min);
        public float Ratio =>
            Mathf.Approximately(_max, 0) ? 0 : (Mathf.Clamp(_current, _min, _max) + _bonus) / _max;

        // Int accessors for all values
        public override int CurrentInt => Mathf.RoundToInt(Mathf.Clamp(_current, _min, _max));
        public override int BonusInt => Mathf.RoundToInt(_bonus);
        public int MaxInt => Mathf.RoundToInt(_max);
        public int MinInt => Mathf.RoundToInt(_min);

        // Returns current + bonus as int (clamped)
        public override int Get() => Mathf.RoundToInt(Mathf.Clamp(_current, _min, _max) + _bonus);

        public int GetCurrent() => Mathf.RoundToInt(Mathf.Clamp(_current, _min, _max));

        public void SetMax(float value)
        {
            _max = value;
            _current = Mathf.Clamp(_current, _min, _max);
        }

        public void SetMin(float value)
        {
            _min = value;
            _current = Mathf.Clamp(_current, _min, _max);
        }

        public override void SetCurrent(float value) => _current = Mathf.Clamp(value, _min, _max);

        public void SetBonusPercent(float percent) => _bonus = _max * percent / 100f;

        // Allow using BoundedCharacterStat as an int in code: int hp = myStat;
        public static implicit operator int(BoundedCharacterStat s) => s?.Get() ?? 0;
    }
}
