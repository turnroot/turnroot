using NaughtyAttributes;
using Turnroot.Characters;
using Turnroot.Characters.CharacterClass;
using Turnroot.CommonAncestors;
using Turnroot.Gameplay.Combat.FundamentalComponents;
using Turnroot.Gameplay.Objects.Components;
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
        Starter = -1,
        Base = 0,
        Advanced = 1,
        Master = 2,
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

    // TrianglePositionEnum defined in TrianglePosition.cs

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
    public class GameplayGeneralSettings : SingletonScriptableObject<GameplayGeneralSettings>
    {
        public enum ClassSelectionMode
        {
            PromotionBased,
            RequirementBased,
        }

        public enum HitFormulaType
        {
            ClassicDouble, // Skill*2 + Dex + Luck/2
            RadiantDouble, // Skill*2.5 + Dex + Luck/2
            Modern, // Skill + Dex + Luck/2
            WeaponOnly, // Just weapon hit (no stat bonuses)
            Custom, // Manual multipliers
        }

        public enum CritFormulaType
        {
            SkillHalf, // Skill/2
            SkillAndLuck, // (Skill + Luck)/2
            WeaponOnly, // Just weapon crit
            Custom, // Manual multiplier
        }

        public enum AvoidFormulaType
        {
            ClassicDouble, // Speed*2 + Luck
            Modern, // Speed + Luck
            SpeedOnly, // Just Speed
            Custom, // Manual multiplier
        }

        [SerializeField, BoxGroup("General Gameplay"), HorizontalLine(color: EColor.Blue)]
        private ClassSelectionMode ClassSelection = ClassSelectionMode.PromotionBased;

        public ClassSelectionMode GetClassSelectionMode() => ClassSelection;

        [SerializeField, BoxGroup("General Gameplay")]
        private CharacterClassData DefaultStartingClass;

        public CharacterClassData GetDefaultStartingClass() => DefaultStartingClass;

        [SerializeField, BoxGroup("General Gameplay")]
        public WeaponType[] WeaponTypes;

        [SerializeField, BoxGroup("General Gameplay")]
        public SpeciesType[] SpeciesTypes;

        [SerializeField, BoxGroup("General Gameplay")]
        private bool WeaponsCanBeForged;

        [SerializeField, BoxGroup("General Gameplay")]
        private bool WeaponsCanBeRepaired;

        [SerializeField, BoxGroup("General Gameplay")]
        private bool WeaponsHaveDurability;

        public bool GetWeaponsCanBeForged() => WeaponsCanBeForged;

        public bool GetWeaponsCanBeRepaired() => WeaponsCanBeRepaired;

        public bool GetWeaponsHaveDurability() => WeaponsHaveDurability;

        [SerializeField, BoxGroup("General Gameplay")]
        private bool UseExperienceAptitudes;

        [SerializeField, BoxGroup("UI"), HorizontalLine(color: EColor.Green)]
        public GoldDisplay GoldDisplayNames = new() { OneLetter = "G", FullName = "gold" };

        [SerializeField, BoxGroup("Combat Mechanics"), HorizontalLine(color: EColor.Yellow)]
        private bool CombatArts;

        [SerializeField, BoxGroup("Combat Mechanics")]
        private int CombatArtLimit = 3;

        [SerializeField, BoxGroup("Combat Mechanics")]
        private int MaxEquippedSkills = 0;

        [SerializeField, BoxGroup("Combat Mechanics")]
        public bool WeaponTriangle;

        [SerializeField, BoxGroup("Combat Mechanics"), ShowIf("WeaponTriangle")]
        private bool ExpandedWeaponTriangle;

        [SerializeField, BoxGroup("Combat Mechanics"), ShowIf("WeaponTriangle")]
        private bool WeaponTriangleAffectsDamage = true;

        [SerializeField, BoxGroup("Combat Mechanics"), ShowIf("WeaponTriangle")]
        private bool WeaponTriangleAffectsHit = true;

        [SerializeField, BoxGroup("Combat Mechanics"), ShowIf("WeaponTriangle")]
        public int WeaponTriangleAdvantage = 20;

        [SerializeField, BoxGroup("Combat Mechanics"), ShowIf("WeaponTriangle")]
        public int WeaponTriangleDisadvantage = -20;

        [SerializeField, BoxGroup("Combat Mechanics")]
        public bool MagicTriangle;

        [SerializeField, BoxGroup("Combat Mechanics"), ShowIf("MagicTriangle")]
        public int MagicTriangleAdvantage = 20;

        [SerializeField, BoxGroup("Combat Mechanics"), ShowIf("MagicTriangle")]
        public int MagicTriangleDisadvantage = -20;

        // Combat formula configuration
        [SerializeField, BoxGroup("Combat Formulas"), HorizontalLine(color: EColor.Red)]
        private HitFormulaType HitFormula = HitFormulaType.ClassicDouble;

        [SerializeField, BoxGroup("Combat Formulas"), ShowIf("HitFormula", HitFormulaType.Custom)]
        private float CustomSkillMultiplierForHit = 2f;

        [SerializeField, BoxGroup("Combat Formulas"), ShowIf("HitFormula", HitFormulaType.Custom)]
        private float CustomDexMultiplierForHit = 1f;

        [SerializeField, BoxGroup("Combat Formulas"), ShowIf("HitFormula", HitFormulaType.Custom)]
        private float CustomLuckMultiplierForHit = 0.5f;

        [SerializeField, BoxGroup("Combat Formulas")]
        private CritFormulaType CritFormula = CritFormulaType.SkillHalf;

        [SerializeField, BoxGroup("Combat Formulas"), ShowIf("CritFormula", CritFormulaType.Custom)]
        private float CustomSkillMultiplierForCrit = 0.5f;

        [SerializeField, BoxGroup("Combat Formulas"), ShowIf("CritFormula", CritFormulaType.Custom)]
        private float CustomLuckMultiplierForCrit = 0f;

        [SerializeField, BoxGroup("Combat Formulas")]
        private AvoidFormulaType AvoidFormula = AvoidFormulaType.ClassicDouble;

        [
            SerializeField,
            BoxGroup("Combat Formulas"),
            ShowIf("AvoidFormula", AvoidFormulaType.Custom)
        ]
        private float CustomSpeedMultiplierForAvoid = 2f;

        [
            SerializeField,
            BoxGroup("Combat Formulas"),
            ShowIf("AvoidFormula", AvoidFormulaType.Custom)
        ]
        private float CustomLuckMultiplierForAvoid = 1f;

        [SerializeField, BoxGroup("Combat Formulas"), ShowIf("ShowWeaponTriangleHitBonus")]
        private float WeaponTriangleHitBonus = 15f;

        // Combat tuning: effectiveness, crit multiplier, double-attack speed threshold, and support bonuses

        [System.Serializable]
        public struct SupportBonus
        {
            public int Hit;
            public int Avoid;
            public int Crit;
            public int Dodge;
        }

        [SerializeField, BoxGroup("Combat Mechanics")]
        private float EffectivenessMultiplier = 1.5f;

        [SerializeField, BoxGroup("Combat Mechanics")]
        private int DoubleAttackSpeedThreshold = 4; // speed threshold for double attacks

        // Support bonuses per rank (C/B/A/S). D/E default to zero.
        [SerializeField, BoxGroup("Combat Mechanics")]
        private SupportBonus SupportBonusC = new SupportBonus
        {
            Hit = 2,
            Avoid = 1,
            Crit = 0,
            Dodge = 0,
        };

        [SerializeField, BoxGroup("Combat Mechanics")]
        private SupportBonus SupportBonusB = new SupportBonus
        {
            Hit = 3,
            Avoid = 2,
            Crit = 1,
            Dodge = 1,
        };

        [SerializeField, BoxGroup("Combat Mechanics")]
        private SupportBonus SupportBonusA = new SupportBonus
        {
            Hit = 4,
            Avoid = 3,
            Crit = 2,
            Dodge = 2,
        };

        [SerializeField, BoxGroup("Combat Mechanics")]
        private SupportBonus SupportBonusD = new SupportBonus
        {
            Hit = 1,
            Avoid = 0,
            Crit = 0,
            Dodge = 0,
        };

        [SerializeField, BoxGroup("Combat Mechanics")]
        private SupportBonus SupportBonusE = new SupportBonus
        {
            Hit = 0,
            Avoid = 0,
            Crit = 0,
            Dodge = 0,
        };

        [SerializeField, BoxGroup("Combat Mechanics")]
        private SupportBonus SupportBonusS = new SupportBonus
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

        [SerializeField, BoxGroup("Combat Mechanics")]
        private bool Battalions;

        [SerializeField, BoxGroup("Combat Mechanics")]
        private int BattalionLimit = 1;

        [SerializeField, BoxGroup("Combat Mechanics")]
        private bool BattalionEndurance;

        [SerializeField, BoxGroup("Combat Mechanics")]
        private bool PairUp;

        [SerializeField, BoxGroup("Combat Mechanics")]
        private bool Adjutants;

        [SerializeField, BoxGroup("Combat Mechanics")]
        private bool AdjutantHeal;

        [SerializeField, BoxGroup("Combat Mechanics")]
        private bool AdjutantGuard;

        [SerializeField, BoxGroup("Combat Mechanics")]
        private bool AdjutantAttack;

        [SerializeField, BoxGroup("Combat Mechanics")]
        private float CriticalHitMultiplier = 3f;

        [SerializeField, BoxGroup("Combat Mechanics")]
        private int MaxWarpDistance = 20;

        [SerializeField, BoxGroup("Combat Mechanics"), Range(0.5f, 1.1f)]
        private float TerrainBonusMultiplier = 0.8f;

        [SerializeField, BoxGroup("Default Stat Values"), HorizontalLine(color: EColor.Blue)]
        private float DefaultMaxHealth = 100f;

        [SerializeField, BoxGroup("Default Stat Values")]
        private float DefaultCurrentHealth = 100f;

        [SerializeField, BoxGroup("Default Stat Values")]
        private float DefaultMinHealth = 0f;

        [SerializeField, BoxGroup("Default Stat Values")]
        private float DefaultMaxLevel = 99f;

        [SerializeField, BoxGroup("Default Stat Values")]
        private float DefaultStartingLevel = 1f;

        [SerializeField, BoxGroup("Default Stat Values")]
        private float DefaultMinLevel = 1f;

        [SerializeField, BoxGroup("Default Stat Values")]
        private float DefaultMaxExperience = 100f;

        [SerializeField, BoxGroup("Default Stat Values")]
        private float DefaultStartingExperience = 0f;

        [SerializeField, BoxGroup("Default Stat Values")]
        private float DefaultMinExperience = 0f;

        [SerializeField, BoxGroup("Default Stat Values")]
        private float DefaultCoreStatValue = 10f;

        [SerializeField, BoxGroup("Default Stat Values")]
        private float DefaultLuckValue = 5f;

        [SerializeField, BoxGroup("Default Stat Values")]
        private float DefaultAuthorityValue = 5f;

        [SerializeField, BoxGroup("Default Stat Values")]
        private float DefaultCriticalAvoidanceValue = 0f;

        [SerializeField, BoxGroup("Range Constants"), HorizontalLine(color: EColor.Pink)]
        private int UnlimitedRange = 0;

        [SerializeField, BoxGroup("Range Constants")]
        private int DefaultMinRange = 0;

        [SerializeField, BoxGroup("Range Constants")]
        private int DefaultMaxRange = 0;

        [SerializeField, BoxGroup("Extra Unit Stats"), HorizontalLine(color: EColor.Green)]
        private bool Weight;

        [SerializeField, BoxGroup("Extra Unit Stats"), ShowIf("Weight")]
        private bool WeightAffectsMovement;

        [SerializeField, BoxGroup("Extra Unit Stats")]
        private bool Luck;

        [SerializeField, BoxGroup("Extra Unit Stats")]
        private bool SeparateCriticalAvoidance;

        [SerializeField, BoxGroup("Extra Unit Stats")]
        private bool Authority;

        [SerializeField, BoxGroup("Items"), HorizontalLine(color: EColor.Violet)]
        private readonly int MaxEquippedNonWeaponItems = 2;

        [SerializeField, BoxGroup("Items")]
        private bool EquippableOutfits;

        [SerializeField, BoxGroup("Items")]
        private bool ItemsCanBeLostItems = true;

        [SerializeField, BoxGroup("Items")]
        private bool ItemsCanBeGifts = true;

        [SerializeField, BoxGroup("Extra Experience Types"), HorizontalLine(color: EColor.Orange)]
        private ExperienceType RidingExperienceType = new()
        {
            Name = "Riding",
            Enabled = false,
            HasWeaponType = false,
        };

        [SerializeField, BoxGroup("Extra Experience Types")]
        private ExperienceType FlyingExperienceType = new()
        {
            Name = "Flying",
            Enabled = false,
            HasWeaponType = false,
        };

        [SerializeField, BoxGroup("Extra Experience Types")]
        private ExperienceType ArmorExperienceType = new()
        {
            Name = "Armor",
            Enabled = false,
            HasWeaponType = false,
        };

        [SerializeField, BoxGroup("Extra Experience Types")]
        private ExperienceType AuthorityExperienceType = new()
        {
            Name = "Authority",
            Enabled = false,
            HasWeaponType = false,
        };

        // Public accessors for Combat Mechanics
        public float GetCriticalHitMultiplier() => CriticalHitMultiplier;

        public int GetWeaponTriangleAdvantage() => WeaponTriangleAdvantage;

        public int GetWeaponTriangleDisadvantage() => WeaponTriangleDisadvantage;

        public int GetMagicTriangleAdvantage() => MagicTriangleAdvantage;

        public int GetMagicTriangleDisadvantage() => MagicTriangleDisadvantage;

        public int GetCombatArtLimit() => CombatArtLimit;

        public float GetTerrainBonusMultiplier() => TerrainBonusMultiplier;

        public int GetMaxEquippedSkills() => MaxEquippedSkills;

        public int GetBattalionLimit() => BattalionLimit;

        public int GetMaxWarpDistance() => MaxWarpDistance;

        public float GetWeaponTriangleHitBonus() => WeaponTriangleHitBonus;

        public bool GetWeaponTriangleAffectsDamage() => WeaponTriangleAffectsDamage;

        public bool GetWeaponTriangleAffectsHit() => WeaponTriangleAffectsHit;

        // Public accessors for Combat Formulas
        public HitFormulaType GetHitFormula() => HitFormula;

        public void GetHitFormulaMultipliers(
            out float skillMult,
            out float dexMult,
            out float luckMult
        )
        {
            switch (HitFormula)
            {
                case HitFormulaType.ClassicDouble:
                    skillMult = 2f;
                    dexMult = 1f;
                    luckMult = 0.5f;
                    break;
                case HitFormulaType.RadiantDouble:
                    skillMult = 2.5f;
                    dexMult = 1f;
                    luckMult = 0.5f;
                    break;
                case HitFormulaType.Modern:
                    skillMult = 1f;
                    dexMult = 1f;
                    luckMult = 0.5f;
                    break;
                case HitFormulaType.WeaponOnly:
                    skillMult = 0f;
                    dexMult = 0f;
                    luckMult = 0f;
                    break;
                case HitFormulaType.Custom:
                    skillMult = CustomSkillMultiplierForHit;
                    dexMult = CustomDexMultiplierForHit;
                    luckMult = CustomLuckMultiplierForHit;
                    break;
                default:
                    skillMult = 2f;
                    dexMult = 1f;
                    luckMult = 0.5f;
                    break;
            }
        }

        public CritFormulaType GetCritFormula() => CritFormula;

        public void GetCritFormulaMultipliers(out float skillMult, out float luckMult)
        {
            switch (CritFormula)
            {
                case CritFormulaType.SkillHalf:
                    skillMult = 0.5f;
                    luckMult = 0f;
                    break;
                case CritFormulaType.SkillAndLuck:
                    skillMult = 0.5f;
                    luckMult = 0.5f;
                    break;
                case CritFormulaType.WeaponOnly:
                    skillMult = 0f;
                    luckMult = 0f;
                    break;
                case CritFormulaType.Custom:
                    skillMult = CustomSkillMultiplierForCrit;
                    luckMult = CustomLuckMultiplierForCrit;
                    break;
                default:
                    skillMult = 0.5f;
                    luckMult = 0f;
                    break;
            }
        }

        public AvoidFormulaType GetAvoidFormula() => AvoidFormula;

        public void GetAvoidFormulaMultipliers(out float speedMult, out float luckMult)
        {
            switch (AvoidFormula)
            {
                case AvoidFormulaType.ClassicDouble:
                    speedMult = 2f;
                    luckMult = 1f;
                    break;
                case AvoidFormulaType.Modern:
                    speedMult = 1f;
                    luckMult = 1f;
                    break;
                case AvoidFormulaType.SpeedOnly:
                    speedMult = 1f;
                    luckMult = 0f;
                    break;
                case AvoidFormulaType.Custom:
                    speedMult = CustomSpeedMultiplierForAvoid;
                    luckMult = CustomLuckMultiplierForAvoid;
                    break;
                default:
                    speedMult = 2f;
                    luckMult = 1f;
                    break;
            }
        }

        // Public accessors for Default Stat Values
        public float GetDefaultMaxHealth() => DefaultMaxHealth;

        public float GetDefaultCurrentHealth() => DefaultCurrentHealth;

        public float GetDefaultMinHealth() => DefaultMinHealth;

        public float GetDefaultMaxLevel() => DefaultMaxLevel;

        public float GetDefaultStartingLevel() => DefaultStartingLevel;

        public float GetDefaultMinLevel() => DefaultMinLevel;

        public float GetDefaultMaxExperience() => DefaultMaxExperience;

        public float GetDefaultStartingExperience() => DefaultStartingExperience;

        public float GetDefaultMinExperience() => DefaultMinExperience;

        public float GetDefaultCoreStatValue() => DefaultCoreStatValue;

        public float GetDefaultLuckValue() => DefaultLuckValue;

        public float GetDefaultAuthorityValue() => DefaultAuthorityValue;

        public float GetDefaultCriticalAvoidanceValue() => DefaultCriticalAvoidanceValue;

        // Public accessors for Range Constants
        public int GetUnlimitedRange() => UnlimitedRange;

        public int GetDefaultMinRange() => DefaultMinRange;

        public int GetDefaultMaxRange() => DefaultMaxRange;

        // Public accessors for Extra Unit Stats
        public bool UseWeight => Weight;
        public bool UseLuck => Luck;
        public bool UseSeparateCriticalAvoidance => SeparateCriticalAvoidance;
        public bool UseAuthority => Authority;

        // Public accessors for Items
        public int GetMaxEquippedNonWeaponItems() => MaxEquippedNonWeaponItems;

        public bool UseEquippableOutfits() => EquippableOutfits;

        public bool UseItemsCanBeLostItems() => ItemsCanBeLostItems;

        public bool UseItemsCanBeGifts() => ItemsCanBeGifts;

        public bool GetUseExperienceAptitudes() => UseExperienceAptitudes;

        // Helper method for ShowIf condition
        private bool ShowWeaponTriangleHitBonus()
        {
            return (WeaponTriangle && WeaponTriangleAffectsHit)
                || (MagicTriangle && WeaponTriangleAffectsHit);
        }

        /// <summary>
        /// Returns all configured experience types (weapon types + extra types that are enabled)
        /// </summary>
        public ExperienceType[] GetAllExperienceTypes()
        {
            var list = new System.Collections.Generic.List<ExperienceType>();

            // Add weapon-based experience types
            if (WeaponTypes != null)
            {
                list.AddRange(
                    System.Array.ConvertAll(
                        WeaponTypes,
                        wt => new ExperienceType
                        {
                            Name = wt.ToString(),
                            Enabled = true,
                            HasWeaponType = true,
                        }
                    )
                );
            }

            // Add extra experience types if enabled
            if (RidingExperienceType.Enabled)
            {
                list.Add(RidingExperienceType);
            }

            if (FlyingExperienceType.Enabled)
            {
                list.Add(FlyingExperienceType);
            }

            if (ArmorExperienceType.Enabled)
            {
                list.Add(ArmorExperienceType);
            }

            if (AuthorityExperienceType.Enabled)
            {
                list.Add(AuthorityExperienceType);
            }

            return list.ToArray();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // When gameplay toggles change, refresh related assets so their
            // OnValidate/OnEnable handlers can re-apply defaults (ObjectItem, etc.)
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null)
                {
                    return;
                }

                UnityEditor.EditorApplication.delayCall += () =>
                {
                    try
                    {
                        // Refresh ObjectItems
                        var guids = UnityEditor.AssetDatabase.FindAssets("t:ObjectItem");
                        foreach (var g in guids)
                        {
                            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                            if (string.IsNullOrEmpty(path))
                            {
                                continue;
                            }

                            // Force update so ScriptableObject OnValidate/OnEnable re-run
                            UnityEditor.AssetDatabase.ImportAsset(
                                path,
                                UnityEditor.ImportAssetOptions.ForceUpdate
                            );
                        }

                        // Refresh CharacterClassData to update ShowIf conditions
                        var classGuids = UnityEditor.AssetDatabase.FindAssets(
                            "t:CharacterClassData"
                        );
                        foreach (var g in classGuids)
                        {
                            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                            if (string.IsNullOrEmpty(path))
                            {
                                continue;
                            }

                            // Force reimport to trigger OnEnable and update cached mode
                            UnityEditor.AssetDatabase.ImportAsset(
                                path,
                                UnityEditor.ImportAssetOptions.ForceUpdate
                            );
                        }
                    }
                    catch { }
                };
            };
        }
#endif
    }
}
