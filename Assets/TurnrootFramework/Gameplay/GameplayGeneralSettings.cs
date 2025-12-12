using NaughtyAttributes;
using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents;
using Turnroot.Gameplay.Objects.Components;
using UnityEngine;

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

    [SerializeField, BoxGroup("General Gameplay"), HorizontalLine(color: EColor.Blue)]
    private ClassSelectionMode ClassSelection = ClassSelectionMode.PromotionBased;

    public ClassSelectionMode GetClassSelectionMode() => ClassSelection;

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
    private bool WeaponTriangle;

    [SerializeField, BoxGroup("Combat Mechanics")]
    private bool ExpandedWeaponTriangle;

    [SerializeField, BoxGroup("Combat Mechanics")]
    private int WeaponTriangleAdvantage = 20;

    [SerializeField, BoxGroup("Combat Mechanics")]
    private int WeaponTriangleDisadvantage = -20;

    [SerializeField, BoxGroup("Combat Mechanics")]
    private bool MagicTriangle;

    [SerializeField, BoxGroup("Combat Mechanics")]
    private int MagicTriangleAdvantage = 20;

    [SerializeField, BoxGroup("Combat Mechanics")]
    private int MagicTriangleDisadvantage = -20;

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

    [SerializeField, BoxGroup("Experience Types"), HorizontalLine(color: EColor.Red)]
    private ExperienceType[] ExperienceWeaponTypes;

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

    public int GetMaxEquippedSkills() => MaxEquippedSkills;

    public int GetBattalionLimit() => BattalionLimit;

    public int GetMaxWarpDistance() => MaxWarpDistance;

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

    /// <summary>
    /// Returns all configured experience types (weapon types + extra types that are enabled)
    /// </summary>
    public ExperienceType[] GetAllExperienceTypes()
    {
        var list = new System.Collections.Generic.List<ExperienceType>();

        // Add weapon-based experience types
        if (ExperienceWeaponTypes != null)
        {
            list.AddRange(ExperienceWeaponTypes);
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
        // Auto-refresh DefaultCharacterStats when Extra Unit Stats change
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null)
            {
                RefreshDefaultCharacterStats();
                // When gameplay toggles change, refresh related assets so their
                // OnValidate/OnEnable handlers can re-apply defaults (ObjectItem, etc.)
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
            }
        };
    }

    private void RefreshDefaultCharacterStats()
    {
        var defaultStats = Turnroot.Characters.DefaultCharacterStats.Instance;
        if (defaultStats != null)
        {
            // Use reflection to call the editor-only refresher
            var refresherType = System.Type.GetType("DefaultCharacterStatsRefresher");
            if (refresherType != null)
            {
                var method = refresherType.GetMethod(
                    "RefreshStats",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
                );
                if (method != null)
                {
                    method.Invoke(null, new object[] { defaultStats, this });
                    UnityEditor.EditorUtility.SetDirty(defaultStats);
                }
            }
        }
    }
#endif
}
