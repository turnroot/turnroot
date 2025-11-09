using Assets.Prototypes.Gameplay.Combat.FundamentalComponents;
using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(
    fileName = "GameplayGeneralSettings",
    menuName = "Turnroot/Game Settings/Gameplay/General Settings"
)]
public class GameplayGeneralSettings : SingletonScriptableObject<GameplayGeneralSettings>
{
    [SerializeField, BoxGroup("General Gameplay"), HorizontalLine(color: EColor.Blue)]
    private bool UseWeatherOnLevels;

    [SerializeField, BoxGroup("General Gameplay")]
    private bool UnitsCanHaveChildren;

    [SerializeField, BoxGroup("General Gameplay")]
    private bool WeaponsCanBeForged;

    [SerializeField, BoxGroup("General Gameplay")]
    private bool WeaponsCanBeRepaired;

    [SerializeField, BoxGroup("General Gameplay")]
    private bool UseExperienceSublevels;

    [SerializeField, BoxGroup("General Gameplay")]
    private bool UseExperienceAptitudes;

    [SerializeField, BoxGroup("Combat Mechanics"), HorizontalLine(color: EColor.Yellow)]
    private bool CombatArts;

    [SerializeField, BoxGroup("Combat Mechanics")]
    private int CombatArtLimit = 3;

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
    private ExperienceType RidingExperienceType = new ExperienceType
    {
        Name = "Riding",
        Enabled = false,
        HasWeaponType = false,
    };

    [SerializeField, BoxGroup("Extra Experience Types")]
    private ExperienceType FlyingExperienceType = new ExperienceType
    {
        Name = "Flying",
        Enabled = false,
        HasWeaponType = false,
    };

    [SerializeField, BoxGroup("Extra Experience Types")]
    private ExperienceType ArmorExperienceType = new ExperienceType
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

    // Public accessors for Experience Settings
    public bool GetUseExperienceSublevels() => UseExperienceSublevels;

    public bool GetUseExperienceAptitudes() => UseExperienceAptitudes;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Auto-refresh DefaultCharacterStats when Extra Unit Stats change
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null)
            {
                RefreshDefaultCharacterStats();
            }
        };
    }

    private void RefreshDefaultCharacterStats()
    {
        var defaultStats = Assets.Prototypes.Characters.DefaultCharacterStats.Instance;
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
