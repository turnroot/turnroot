using NaughtyAttributes;
using Turnroot.Characters;
using Turnroot.Characters.CharacterClass;
using Turnroot.Gameplay.Objects.Components;
using UnityEngine;

namespace Turnroot.GameSettings
{
    public partial class GameplayGeneralSettings
    {
        [
            BoxGroup("Unit Classes"),
            InfoBox(
                "Class selection mode affects how characters may change classes:\n- PromotionBased: classes can only be obtained via configured PromotionPaths (use for restrictive progression).\n- RequirementBased: characters may change to any class they meet the requirements for; if they narrowly miss requirements an adjustable 'class exam' chance may allow success."
            ),
            HorizontalLine(color: EColor.Blue)
        ]
        public ClassSelectionMode ClassSelection = ClassSelectionMode.PromotionBased;

        public ClassSelectionMode GetClassSelectionMode() => ClassSelection;

        // Levels always persist when changing classes; they never reset to 1.

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

        [BoxGroup("Weapons"), InfoBox("Put all of the weapon types your game uses here")]
        public WeaponType[] WeaponTypes;

        [BoxGroup("Characters"), InfoBox("Put all of the species types your game uses here")]
        public SpeciesType[] SpeciesTypes;

        [
            BoxGroup("Level Up"),
            InfoBox(
                "If true, growth rates above 100% automatically gain +1 and have a chance to gain an additional +1"
            )
        ]
        public bool LevelUpExtraGrowthChance = false;

        [BoxGroup("Weapons"), InfoBox("If true, weapons can be forged into higher-tier weapons")]
        public bool WeaponsCanBeForged;

        [BoxGroup("Weapons"), InfoBox("If true, weapons can be repaired to renew uses")]
        public bool WeaponsCanBeRepaired;

        [BoxGroup("Weapons"), InfoBox("If true, weapons have a set number of uses")]
        public bool WeaponsHaveDurability;

        [
            BoxGroup("Combat Mechanics"),
            InfoBox("If true, units can attack even when unarmed. Maximum range will be 1.")
        ]
        public bool UnitCanAttackWithoutWeapons;

        [BoxGroup("General Gameplay")]
        public bool UseExperienceAptitudes;

        [BoxGroup("Hub")]
        public bool HubHasTeamLocations;

        [BoxGroup("Hub")]
        [Tooltip("Maximum number of units allowed at a single hub location.")]
        public int MaxUnitsPerHubLocation = 6;
    }
}
