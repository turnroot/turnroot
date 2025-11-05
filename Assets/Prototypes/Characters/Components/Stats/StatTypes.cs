using System.Collections.Generic;

namespace Assets.Prototypes.Characters.Stats
{
    public enum BoundedStatType
    {
        Health,
        Level,
        LevelExperience,
        ClassExperience,
    }

    public enum UnboundedStatType
    {
        Strength,
        Defense,
        Magic,
        Resistance,
        Skill,
        Speed,
        Luck,
        Dexterity,
        Charm,
        Movement,
        Endurance,
        Authority,
        CriticalAvoidance,
    }

    public static class BoundedStatTypeExtensions
    {
        private static readonly Dictionary<
            BoundedStatType,
            (string DisplayName, string Description)
        > StatInfo = new()
        {
            { BoundedStatType.Level, ("Level", "Character's current level") },
            { BoundedStatType.Health, ("HP", "Character's life force") },
            { BoundedStatType.LevelExperience, ("EXP", "Experience points toward next level") },
            {
                BoundedStatType.ClassExperience,
                ("Class EXP", "Experience points in current class")
            },
        };

        public static string GetDisplayName(this BoundedStatType type) =>
            StatInfo[type].DisplayName;

        public static string GetDescription(this BoundedStatType type) =>
            StatInfo[type].Description;
    }

    public static class UnboundedStatTypeExtensions
    {
        private static readonly Dictionary<
            UnboundedStatType,
            (string DisplayName, string Description)
        > StatInfo = new()
        {
            { UnboundedStatType.Strength, ("Str", "Physical power and melee damage") },
            { UnboundedStatType.Defense, ("Def", "Resistance to physical attacks") },
            { UnboundedStatType.Magic, ("Mag", "Magical power and spell damage") },
            { UnboundedStatType.Resistance, ("Res", "Resistance to magical attacks") },
            { UnboundedStatType.Skill, ("Skl", "Accuracy and critical hit chance") },
            { UnboundedStatType.Speed, ("Spd", "Determines turn order and evasion") },
            { UnboundedStatType.Luck, ("Lck", "Affects various chance-based outcomes") },
            { UnboundedStatType.Dexterity, ("Dex", "Affects ranged attack accuracy and dodging") },
            { UnboundedStatType.Charm, ("Chr", "Influences interactions with NPCs") },
            { UnboundedStatType.Movement, ("Mov", "Number of tiles a character can move") },
            { UnboundedStatType.Endurance, ("End", "Affects stamina and resistance to fatigue") },
            { UnboundedStatType.Authority, ("Ahy", "Influences leadership and command abilities") },
            {
                UnboundedStatType.CriticalAvoidance,
                ("CritAvo", "Reduces chance of receiving critical hits")
            },
        };

        public static string GetDisplayName(this UnboundedStatType type) =>
            StatInfo[type].DisplayName;

        public static string GetDescription(this UnboundedStatType type) =>
            StatInfo[type].Description;
    }
}
