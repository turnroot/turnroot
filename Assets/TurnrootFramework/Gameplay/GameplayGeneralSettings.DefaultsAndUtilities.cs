using NaughtyAttributes;
using Turnroot.Characters.Stats;
using Turnroot.Gameplay.Combat.FundamentalComponents;
using Turnroot.Utilities;
using UnityEngine; // for GameDate

namespace Turnroot.GameSettings
{
    public partial class GameplayGeneralSettings
    {
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

        [Header("Game Date")]
        [Tooltip("Initial in-game calendar date used until a scene with a date is loaded.")]
        public GameDate StartingGameDate = GameDate.Default;

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

        // ---------------------------------------------------------------------
        // Default stats helper (single source-of-truth)
        // ---------------------------------------------------------------------
        public BoundedStatType[] GetDefaultBoundedStatTypes()
        {
            var core = new System.Collections.Generic.List<BoundedStatType>
            {
                BoundedStatType.Health,
                BoundedStatType.Level,
                BoundedStatType.LevelExperience,
            };

            if (GetUseExperienceAptitudes())
            {
                core.Add(BoundedStatType.ClassExperience);
            }

            return core.ToArray();
        }

        public UnboundedStatType[] GetDefaultUnboundedStatTypes()
        {
            var core = new System.Collections.Generic.List<UnboundedStatType>
            {
                UnboundedStatType.Strength,
                UnboundedStatType.Defense,
                UnboundedStatType.Magic,
                UnboundedStatType.Resistance,
                UnboundedStatType.Skill,
                UnboundedStatType.Speed,
                UnboundedStatType.Dexterity,
                UnboundedStatType.Charm,
                UnboundedStatType.Movement,
            };

            if (UseLuck)
            {
                core.Add(UnboundedStatType.Luck);
            }

            if (UseSeparateCriticalAvoidance)
            {
                core.Add(UnboundedStatType.CriticalAvoidance);
            }

            if (UseAuthority)
            {
                core.Add(UnboundedStatType.Authority);
            }

            return core.ToArray();
        }

        public System.Collections.Generic.List<BoundedCharacterStat> CreateDefaultBoundedStats()
        {
            var outList = new System.Collections.Generic.List<BoundedCharacterStat>();
            foreach (var t in GetDefaultBoundedStatTypes())
            {
                var (max, current, min) = StatHelpers.GetDefaultValuesForBoundedStatInternal(t);
                outList.Add(new BoundedCharacterStat(max, current, min, t));
            }
            return outList;
        }

        public System.Collections.Generic.List<CharacterStat> CreateDefaultUnboundedStats()
        {
            var outList = new System.Collections.Generic.List<CharacterStat>();
            foreach (var t in GetDefaultUnboundedStatTypes())
            {
                var value = StatHelpers.GetDefaultValueForUnboundedStatInternal(t);
                outList.Add(new CharacterStat(value, t));
            }
            return outList;
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
                        // also refresh CharacterData assets so experience rank lists update
                        var charGuids = UnityEditor.AssetDatabase.FindAssets("t:CharacterData");
                        foreach (var g in charGuids)
                        {
                            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                            if (string.IsNullOrEmpty(path))
                            {
                                continue;
                            }

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
