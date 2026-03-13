using System;

namespace Turnroot.Utilities
{
    /// <summary>
    /// Simple struct representing an in-game calendar date.
    /// Stored in long-term memory and passed in brain events.
    /// Month is 1-based (1=January).
    /// </summary>
    [Serializable]
    public struct GameDate : IEquatable<GameDate>
    {
        public int year;
        public int month; // 1-based (1=January)
        public int day;

        public GameDate(int year, int month, int day)
        {
            this.year = year;
            this.month = month;
            this.day = day;
        }

        public static readonly GameDate Default = new(1000, 1, 1);

        /// <summary>
        /// Returns the ordinal suffix for a given day number (1st, 2nd, 3rd, etc.).
        /// Handles the 11‑13 exception.
        /// </summary>
        public static string GetDaySuffix(int day)
        {
            return day is >= 11 and <= 13
                ? "th"
                : (day % 10) switch
                {
                    1 => "st",
                    2 => "nd",
                    3 => "rd",
                    _ => "th",
                };
        }

        // Equality members for comparing dates and using in collections.
        public bool Equals(GameDate other)
        {
            return year == other.year && month == other.month && day == other.day;
        }

        public override bool Equals(object obj)
        {
            return obj is GameDate other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(year, month, day);
        }

        public static bool operator ==(GameDate left, GameDate right) => left.Equals(right);

        public static bool operator !=(GameDate left, GameDate right) => !(left == right);
    }
}
