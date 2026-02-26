using NaughtyAttributes;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.GameSettings
{
    [System.Serializable]
    public struct GoldDisplay
    {
        public string OneLetter;
        public string FullName;
    }

    public enum ProgressionLevel
    {
        Starter = 0,
        Base = 1,
        Advanced = 2,
        Master = 3,
        Expert = 4,
    }

    public enum MovementType
    {
        Infantry,
        Riding,
        Flying,
        Armored,
        None,
    }

    public enum EquipableObjectType
    {
        Accessory,
        Shield,
        Staff,
        Ring,
    }

    public enum EquipableOutfitType
    {
        Helmet,
        Hat,
        Shirt,
        Pants,
        Dress,
        Skirt,
        Robe,
        Gloves,
        Coat,
        Armor,
        Boots,
        Cloak,
    }

    public enum ReplenishUseType
    {
        None,
        Quarter,
        Third,
        Half,
        Full,
        One,
        Two,
        Three,
        Four,
        Five,
        Six,
        Seven,
        Eight,
        Nine,
        Ten,
    }

    [CreateAssetMenu(
        fileName = "GameplayGeneralSettings",
        menuName = "Turnroot/Game Settings/Gameplay/General Settings"
    )]
    public partial class GameplayGeneralSettings
        : SingletonScriptableObject<GameplayGeneralSettings>
    {
        public enum ClassSelectionMode
        {
            PromotionBased,
            RequirementBased,
        }

        public float MinimumPercentChanceToAttemptClassChange => .6f;

        [System.Serializable]
        public struct MasteryTuning
        {
            [Tooltip(
                "Multiplier applied to battle-based mastery points (applies to points passed into IncrementBattleCount)"
            )]
            public float BattlePointMultiplier;

            [Tooltip(
                "Additional multiplier applied when the battle was 'successful' (e.g. unit scored a kill)"
            )]
            public float BattleSuccessMultiplier;

            public static MasteryTuning Default() =>
                new() { BattlePointMultiplier = 1f, BattleSuccessMultiplier = 1f };
        }

        [System.Serializable]
        public struct RequirementExamTuning
        {
            [
                Range(0f, 1f),
                Tooltip("Minimum floor applied to Requirement-based class exam chance (0..1)")
            ]
            public float ExamFloor;

            [Tooltip("Weight applied to level-proximity when calculating exam chance")]
            public float WeightLevel;

            [Tooltip("Weight applied to stat-proximity when calculating exam chance")]
            public float WeightStats;

            [Tooltip("Weight applied to experience-proximity when calculating exam chance")]
            public float WeightExperience;

            public static RequirementExamTuning Default() =>
                new()
                {
                    ExamFloor = 0f,
                    WeightLevel = 1f,
                    WeightStats = 1f,
                    WeightExperience = 1f,
                };
        }

        [BoxGroup("Unit Classes")]
        public MasteryTuning MasterySettings = MasteryTuning.Default();

        [BoxGroup("Unit Classes")]
        public RequirementExamTuning RequirementExamSettings = RequirementExamTuning.Default();

        public enum HitFormulaType
        {
            ClassicSkillHeavy,
            ExtraSkillHeavy,
            ModernBalanced,
            WeaponOnly,
            Custom,
        }

        public enum CritFormulaType
        {
            SkillHalf,
            SkillAndLuck,
            WeaponOnly,
            Custom,
        }

        public enum AvoidFormulaType
        {
            ClassicSpeedHeavy,
            ModernBalanced,
            SpeedOnly,
            Custom,
        }
    }
}
