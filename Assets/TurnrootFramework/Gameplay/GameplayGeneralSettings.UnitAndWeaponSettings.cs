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

        [BoxGroup("Magic"), InfoBox("Put all of the magic types your game uses here")]
        public WeaponType[] MagicTypes;

        [
            BoxGroup("Combat Mechanics"),
            InfoBox("Weapon triangle relationship: Top > Left, Left > Right, Right > Top")
        ]
        public bool WeaponTriangleIsActive;

        [
            BoxGroup("Combat Mechanics"),
            InfoBox("Weapon types that live in the Top triangle position")
        ]
        public WeaponType[] TopTriangleWeaponTypes;

        [
            BoxGroup("Combat Mechanics"),
            InfoBox("Weapon types that live in the Left triangle position")
        ]
        public WeaponType[] LeftTriangleWeaponTypes;

        [
            BoxGroup("Combat Mechanics"),
            InfoBox("Weapon types that live in the Right triangle position")
        ]
        public WeaponType[] RightTriangleWeaponTypes;

        [BoxGroup("Combat Mechanics"), InfoBox("Weapon types that are not on the triangle")]
        public WeaponType[] NotOnTriangleWeaponTypes;

        public WeaponType GetWeaponTypeById(string id)
        {
            if (string.IsNullOrEmpty(id) || WeaponTypes == null)
            {
                return null;
            }

            foreach (var weapon in WeaponTypes)
            {
                if (
                    weapon != null
                    && string.Equals(weapon.Id, id, System.StringComparison.OrdinalIgnoreCase)
                )
                {
                    return weapon;
                }
            }
            return null;
        }

        public TrianglePositionEnum GetWeaponTrianglePosition(string weaponId)
        {
            var weapon = GetWeaponTypeById(weaponId);
            if (weapon == null)
            {
                return TrianglePositionEnum.NotOnTriangle;
            }

            if (
                TopTriangleWeaponTypes != null
                && System.Array.Exists(TopTriangleWeaponTypes, w => w == weapon)
            )
            {
                return TrianglePositionEnum.Top;
            }

            if (
                LeftTriangleWeaponTypes != null
                && System.Array.Exists(LeftTriangleWeaponTypes, w => w == weapon)
            )
            {
                return TrianglePositionEnum.Left;
            }

            if (
                RightTriangleWeaponTypes != null
                && System.Array.Exists(RightTriangleWeaponTypes, w => w == weapon)
            )
            {
                return TrianglePositionEnum.Right;
            }

            if (
                NotOnTriangleWeaponTypes != null
                && System.Array.Exists(NotOnTriangleWeaponTypes, w => w == weapon)
            )
            {
                return TrianglePositionEnum.NotOnTriangle;
            }

            if (weapon.TrianglePosition != null)
            {
                return weapon.TrianglePosition.Position;
            }

            return TrianglePositionEnum.NotOnTriangle;
        }

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

        [BoxGroup("General Gameplay")]
        [Tooltip("Experience points required to advance one weapon/skill rank (E→D, D→C, etc.).")]
        public int ExperienceRankUpThreshold = 100;

        [BoxGroup("Hub")]
        public bool HubHasTeamLocations;

        [BoxGroup("Hub")]
        [Tooltip("Maximum number of units allowed at a single hub location.")]
        public int MaxUnitsPerHubLocation = 6;

        [BoxGroup("General Gameplay")]
        public int StartingGold = 2500;
    }
}
