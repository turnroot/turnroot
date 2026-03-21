using NaughtyAttributes;
using Turnroot.CommonAncestors;
using UnityEngine;

namespace Turnroot.GameSettings
{
    public partial class GameplayGeneralSettings
    {
        [
            BoxGroup("Combat Mechanics"),
            HorizontalLine(color: EColor.Yellow),
            MinMaxSlider(-0.35f, .35f),
            InfoBox("Don't change this directly; use Tools -> Turnroot -> Test Generic Enemy Skew")
        ]
        public Vector2 GenericEnemySkewAdjustmentRange = new(-0.15f, .2f);

        [BoxGroup("Combat Mechanics")]
        public int MaxEquippedSkills = 0;

        [
            BoxGroup("Combat Mechanics"),
            HideInInspector,
            Tooltip("Deprecated: use WeaponTriangleIsActive from UnitAndWeaponSettings")
        ]
        public bool WeaponTriangle;

        public bool WeaponTriangleEnabled => WeaponTriangleIsActive || WeaponTriangle;

        [BoxGroup("Combat Mechanics"), ShowIf("WeaponTriangleIsActive")]
        public bool WeaponTriangleAffectsDamage = true;

        [BoxGroup("Combat Mechanics"), ShowIf("WeaponTriangleIsActive")]
        public bool WeaponTriangleAffectsHit = true;

        [InfoBox("Percentage bonus/penalty (out of 100)")]
        [BoxGroup("Combat Mechanics"), ShowIf("WeaponTriangleIsActive")]
        public int WeaponTriangleAdvantage = 20;

        [BoxGroup("Combat Mechanics"), ShowIf("WeaponTriangleIsActive")]
        public int WeaponTriangleDisadvantage = -20;

        [BoxGroup("Combat Mechanics")]
        public bool MagicTriangle;

        [BoxGroup("Combat Mechanics"), ShowIf("MagicTriangle")]
        public int MagicTriangleAdvantage = 20;

        [BoxGroup("Combat Mechanics"), ShowIf("MagicTriangle")]
        public int MagicTriangleDisadvantage = -20;

        [BoxGroup("Combat Formulas"), HorizontalLine(color: EColor.Red)]
        public HitFormulaType HitFormula = HitFormulaType.ModernBalanced;

        [BoxGroup("Combat Formulas"), ShowIf("HitFormula", HitFormulaType.Custom)]
        public float CustomSkillMultiplierForHit = 2f;

        [BoxGroup("Combat Formulas"), ShowIf("HitFormula", HitFormulaType.Custom)]
        public float CustomDexMultiplierForHit = 1f;

        [BoxGroup("Combat Formulas"), ShowIf("HitFormula", HitFormulaType.Custom)]
        public float CustomLuckMultiplierForHit = 0.5f;

        [BoxGroup("Combat Formulas")]
        public CritFormulaType CritFormula = CritFormulaType.SkillHalf;

        [BoxGroup("Combat Formulas"), ShowIf("CritFormula", CritFormulaType.Custom)]
        public float CustomSkillMultiplierForCrit = 0.5f;

        [BoxGroup("Combat Formulas"), ShowIf("CritFormula", CritFormulaType.Custom)]
        public float CustomLuckMultiplierForCrit = 0f;

        [BoxGroup("Combat Formulas")]
        public AvoidFormulaType AvoidFormula = AvoidFormulaType.ModernBalanced;

        [BoxGroup("Combat Formulas"), ShowIf("AvoidFormula", AvoidFormulaType.Custom)]
        public float CustomSpeedMultiplierForAvoid = 2f;

        [BoxGroup("Combat Formulas"), ShowIf("AvoidFormula", AvoidFormulaType.Custom)]
        public float CustomLuckMultiplierForAvoid = 1f;

        [BoxGroup("Combat Formulas"), ShowIf("ShowWeaponTriangleHitBonus")]
        public float WeaponTriangleHitBonus = 15f;

        [System.Serializable]
        public struct SupportBonus
        {
            public int Hit;
            public int Avoid;
            public int Crit;
            public int Dodge;
        }

        [BoxGroup("Combat Mechanics")]
        public float EffectivenessMultiplier = 1.5f;

        [BoxGroup("Combat Mechanics")]
        public int DoubleAttackSpeedThreshold = 4;

        [BoxGroup("Combat Mechanics")]
        public SupportBonus SupportBonusC = new()
        {
            Hit = 2,
            Avoid = 1,
            Crit = 0,
            Dodge = 0,
        };

        [BoxGroup("Combat Mechanics")]
        public SupportBonus SupportBonusB = new()
        {
            Hit = 3,
            Avoid = 2,
            Crit = 1,
            Dodge = 1,
        };

        [BoxGroup("Combat Mechanics")]
        public SupportBonus SupportBonusA = new()
        {
            Hit = 4,
            Avoid = 3,
            Crit = 2,
            Dodge = 2,
        };

        [BoxGroup("Combat Mechanics")]
        public SupportBonus SupportBonusD = new()
        {
            Hit = 1,
            Avoid = 0,
            Crit = 0,
            Dodge = 0,
        };

        [BoxGroup("Combat Mechanics")]
        public SupportBonus SupportBonusE = new()
        {
            Hit = 0,
            Avoid = 0,
            Crit = 0,
            Dodge = 0,
        };

        [BoxGroup("Combat Mechanics")]
        public SupportBonus SupportBonusS = new()
        {
            Hit = 5,
            Avoid = 4,
            Crit = 3,
            Dodge = 3,
        };

        public float GetEffectivenessMultiplier() => EffectivenessMultiplier;

        public int GetDoubleAttackSpeedThreshold() => DoubleAttackSpeedThreshold;

        public SupportBonus GetSupportBonusForRank(string rank)
        {
            return rank switch
            {
                LeveledLetteredField.S => SupportBonusS,
                LeveledLetteredField.A => SupportBonusA,
                LeveledLetteredField.B => SupportBonusB,
                LeveledLetteredField.C => SupportBonusC,
                LeveledLetteredField.D => SupportBonusD,
                LeveledLetteredField.E => SupportBonusE,
                _ => new SupportBonus(),
            };
        }

        [BoxGroup("Combat Mechanics")]
        public bool Battalions;

        [BoxGroup("Combat Mechanics")]
        public int BattalionLimit = 1;

        [BoxGroup("Combat Mechanics")]
        public bool PairUp;

        [BoxGroup("Combat Mechanics")]
        public bool Adjutants;

        [BoxGroup("Combat Mechanics")]
        public bool AdjutantHeal;

        [BoxGroup("Combat Mechanics")]
        public bool AdjutantGuard;

        [BoxGroup("Combat Mechanics")]
        public bool AdjutantAttack;

        [BoxGroup("Combat Mechanics")]
        public float CriticalHitMultiplier = 3f;

        [BoxGroup("Combat Mechanics")]
        public int MaxWarpDistance = 20;

        [BoxGroup("Combat Mechanics"), Range(0.5f, 1.1f)]
        public float TerrainBonusMultiplier = 0.8f;
    }
}
