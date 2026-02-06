using NaughtyAttributes;
using Turnroot.Characters;
using Turnroot.Characters.CharacterClass;
using Turnroot.CommonAncestors;
using Turnroot.Gameplay.Combat.FundamentalComponents;
using Turnroot.Gameplay.Objects.Components;
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
            ClassicSkillHeavy, // Skill*2 + Dex + Luck/2
            ExtraSkillHeavy, // Skill*2.5 + Dex + Luck/2
            ModernBalanced, // Skill + Dex + Luck/2
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
            ClassicSpeedHeavy, // Speed*2 + Luck
            ModernBalanced, // Speed + Luck
            SpeedOnly, // Just Speed
            Custom, // Manual multiplier
        }

        [BoxGroup("Uni Classes"), HorizontalLine(color: EColor.Blue)]
        public ClassSelectionMode ClassSelection = ClassSelectionMode.PromotionBased;

        public ClassSelectionMode GetClassSelectionMode() => ClassSelection;

        [BoxGroup("Unit Classes"), InfoBox("Units without a class assigned will use this class")]
        public CharacterClassData DefaultStartingClass;

        [
            BoxGroup("Unit Classes"),
            InfoBox(
                "Don't change this unless you're making your own shader graph system to handle unit appearance!",
                EInfoBoxType.Warning
            )
        ]
        public Material UnitOutfitMaterialTemplate;

        [BoxGroup("Animations"), InfoBox("Base runtime AnimatorController used for unit models")]
        public RuntimeAnimatorController DefaultUnitAnimatorController;

        public CharacterClassData GetDefaultStartingClass() => DefaultStartingClass;

        [BoxGroup("General Gameplay"), InfoBox("Put all of the weapon types your game uses here")]
        public WeaponType[] WeaponTypes;

        [BoxGroup("General Gameplay"), InfoBox("Put all of the species types your game uses here")]
        public SpeciesType[] SpeciesTypes;

        [
            BoxGroup("General Gameplay"),
            InfoBox("If true, weapons can be forged into higher-tier weapons")
        ]
        public bool WeaponsCanBeForged;

        [BoxGroup("General Gameplay"), InfoBox("If true, weapons can be repaired to renew uses")]
        public bool WeaponsCanBeRepaired;

        [BoxGroup("General Gameplay"), InfoBox("If true, weapons have a set number of uses")]
        public bool WeaponsHaveDurability;

        [
            BoxGroup("General Gameplay"),
            InfoBox("If true, units can attack even when unarmed. Maximum range will be 1.")
        ]
        public bool UnitCanAttackWithoutWeapons;

        [BoxGroup("General Gameplay")]
        public bool UseExperienceAptitudes;

        [BoxGroup("UI"), HorizontalLine(color: EColor.Green)]
        public GoldDisplay GoldDisplayNames = new() { OneLetter = "G", FullName = "gold" };

        [BoxGroup("Visuals"), HorizontalLine(color: EColor.Yellow)]
        public float UnitMovementCurveSmoothing = 4f;

        [BoxGroup("Visuals")]
        public float UnitMovementCurveRandomness = 0.25f;

        [BoxGroup("Visuals")]
        public float UnitMovementDecelerationRange = 1.5f;

        [BoxGroup("Visuals")]
        public float UnitMovementMinSpeedMultiplier = 0.4f;

        [BoxGroup("Maps"), HorizontalLine(color: EColor.Green)]
        public bool UnexploredMaps;

        [ShowIf("UnexploredMaps"), BoxGroup("Maps"), Range(1, 3)]
        public int MaxNumberOfExplorers = 2;

        [ShowIf("UnexploredMaps"), BoxGroup("Maps")]
        public bool RidersAndFliersAreBetterExplorers = true;

        [ShowIf("UnexploredMaps"), BoxGroup("Maps")]
        public bool ExplorersFailIfInjured = false;

        [BoxGroup("Combat Mechanics"), HorizontalLine(color: EColor.Yellow)]
        public bool CombatArts;

        [BoxGroup("Combat Mechanics")]
        public int CombatArtLimit = 3;

        [BoxGroup("Combat Mechanics")]
        public int MaxEquippedSkills = 0;

        [BoxGroup("Combat Mechanics")]
        public bool WeaponTriangle;

        [BoxGroup("Combat Mechanics"), ShowIf("WeaponTriangle")]
        public bool ExpandedWeaponTriangle;

        [BoxGroup("Combat Mechanics"), ShowIf("WeaponTriangle")]
        public bool WeaponTriangleAffectsDamage = true;

        [BoxGroup("Combat Mechanics"), ShowIf("WeaponTriangle")]
        public bool WeaponTriangleAffectsHit = true;

        [BoxGroup("Combat Mechanics"), ShowIf("WeaponTriangle")]
        public int WeaponTriangleAdvantage = 20;

        [BoxGroup("Combat Mechanics"), ShowIf("WeaponTriangle")]
        public int WeaponTriangleDisadvantage = -20;

        [BoxGroup("Combat Mechanics")]
        public bool MagicTriangle;

        [BoxGroup("Combat Mechanics"), ShowIf("MagicTriangle")]
        public int MagicTriangleAdvantage = 20;

        [BoxGroup("Combat Mechanics"), ShowIf("MagicTriangle")]
        public int MagicTriangleDisadvantage = -20;

        // Combat formula configuration
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

        // Combat tuning: effectiveness, crit multiplier, double-attack speed threshold, and support bonuses

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
        public int DoubleAttackSpeedThreshold = 4; // speed threshold for double attacks

        // Support bonuses per rank (C/B/A/S). D/E default to zero.
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
        public bool BattalionEndurance;

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

        [BoxGroup("Default Stat Values"), HorizontalLine(color: EColor.Blue)]
        public float DefaultMaxHealth = 100f;

        [BoxGroup("Default Stat Values")]
        public float DefaultCurrentHealth = 100f;

        [BoxGroup("Default Stat Values")]
        public float DefaultMinHealth = 0f;

        [BoxGroup("Default Stat Values")]
        public float DefaultMaxLevel = 99f;

        [BoxGroup("Default Stat Values")]
        public float DefaultStartingLevel = 1f;

        [BoxGroup("Default Stat Values")]
        public float DefaultMinLevel = 1f;

        [BoxGroup("Default Stat Values")]
        public float DefaultMaxExperience = 100f;

        [BoxGroup("Default Stat Values")]
        public float DefaultStartingExperience = 0f;

        [BoxGroup("Default Stat Values")]
        public float DefaultMinExperience = 0f;

        [BoxGroup("Default Stat Values")]
        public float DefaultCoreStatValue = 10f;

        [BoxGroup("Default Stat Values")]
        public float DefaultLuckValue = 5f;

        [BoxGroup("Default Stat Values")]
        public float DefaultAuthorityValue = 5f;

        [BoxGroup("Default Stat Values")]
        public float DefaultCriticalAvoidanceValue = 0f;

        [BoxGroup("Range Constants"), HorizontalLine(color: EColor.Pink)]
        public int UnlimitedRange = 0;

        [BoxGroup("Range Constants")]
        public int DefaultMinRange = 0;

        [BoxGroup("Range Constants")]
        public int DefaultMaxRange = 0;

        [BoxGroup("Extra Unit Stats"), HorizontalLine(color: EColor.Green)]
        public bool Weight;

        [BoxGroup("Extra Unit Stats"), ShowIf("Weight")]
        public bool WeightAffectsMovement;

        [BoxGroup("Extra Unit Stats")]
        public bool Luck;

        [BoxGroup("Extra Unit Stats")]
        public bool SeparateCriticalAvoidance;

        [BoxGroup("Extra Unit Stats")]
        public bool Authority;

        [BoxGroup("Items"), HorizontalLine(color: EColor.Violet)]
        public readonly int MaxEquippedNonWeaponItems = 2;

        [BoxGroup("Items")]
        public bool EquippableOutfits;

        [BoxGroup("Items")]
        public bool ItemsCanBeLostItems = true;

        [BoxGroup("Items")]
        public bool ItemsCanBeGifts = true;

        [BoxGroup("Extra Experience Types"), HorizontalLine(color: EColor.Orange)]
        public ExperienceType RidingExperienceType = new()
        {
            Name = "Riding",
            Enabled = false,
            HasWeaponType = false,
        };

        [BoxGroup("Extra Experience Types")]
        public ExperienceType FlyingExperienceType = new()
        {
            Name = "Flying",
            Enabled = false,
            HasWeaponType = false,
        };

        [BoxGroup("Extra Experience Types")]
        public ExperienceType ArmorExperienceType = new()
        {
            Name = "Armor",
            Enabled = false,
            HasWeaponType = false,
        };

        [BoxGroup("Extra Experience Types")]
        public ExperienceType AuthorityExperienceType = new()
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
                case HitFormulaType.ClassicSkillHeavy:
                    skillMult = 2f;
                    dexMult = 1f;
                    luckMult = 0.5f;
                    break;
                case HitFormulaType.ExtraSkillHeavy:
                    skillMult = 2.5f;
                    dexMult = 1f;
                    luckMult = 0.5f;
                    break;
                case HitFormulaType.ModernBalanced:
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
                case AvoidFormulaType.ClassicSpeedHeavy:
                    speedMult = 2f;
                    luckMult = 1f;
                    break;
                case AvoidFormulaType.ModernBalanced:
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
        public bool ShowWeaponTriangleHitBonus()
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
        public void OnValidate()
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
