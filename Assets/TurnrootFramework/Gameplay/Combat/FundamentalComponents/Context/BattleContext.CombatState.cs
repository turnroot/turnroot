using Turnroot.Characters;
using Turnroot.Gameplay.Maps;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    public partial class BattleContext
    {
        #region Combat State and Effectiveness

        public bool AttackIsEffective(CharacterInstance unit, CharacterInstance target)
        {
            if (unit == null || target == null)
            {
                return false;
            }

            var attackerWeapon = unit.GetEquippedWeapon();
            if (attackerWeapon == null || attackerWeapon.Template == null)
            {
                return false;
            }

            var weaponTemplate = attackerWeapon.Template;

            var targetSpecies = target.CharacterTemplate?.Species;
            if (targetSpecies != null && weaponTemplate.SpeciesEffectiveAgainst != null)
            {
                foreach (var s in weaponTemplate.SpeciesEffectiveAgainst)
                {
                    if (s == targetSpecies)
                    {
                        return true;
                    }
                }
            }

            var targetWeapon = target.GetEquippedWeapon();
            if (targetWeapon?.Template != null)
            {
                var targetWeaponType = targetWeapon.Template.WeaponType;
                if (weaponTemplate.WeaponTypesEffectiveAgainst != null)
                {
                    foreach (var wt in weaponTemplate.WeaponTypesEffectiveAgainst)
                    {
                        return wt == targetWeaponType;
                    }
                }
            }

            return false;
        }

        public bool AttackWouldKill(CharacterInstance target)
        {
            if (Unit.UnitInstance == null || target == null)
            {
                return false;
            }

            var weaponItem = Unit.UnitInstance.GetEquippedWeapon();
            return weaponItem != null
                && DamageCalculator.WouldKill(Unit.UnitInstance, target, weaponItem, this);
        }

        public bool TargetCanCounterattack(
            CharacterInstance self,
            CharacterInstance target,
            MapGridPoint projectedDestination
        )
        {
            if (self == null || target == null)
            {
                return false;
            }
            var targetWeapon = target.GetEquippedWeapon();
            if (targetWeapon == null)
            {
                return false;
            }

            var targetAttackRange = targetWeapon.Template.UpperRange;

            var targetGridPoint = target.UnitPositionToMapGridPoint(
                target.MapGridPosition,
                MapGrid
            );
            var parameters = PathfindingParameters.FromCharacter(target, MapGrid, targetGridPoint);

            return parameters != null
                && projectedDestination != null
                && PathfinderHelpers.TryComputePathMovementCost(
                    MapGrid,
                    parameters,
                    projectedDestination,
                    out float totalCost
                )
                && totalCost <= targetAttackRange;
        }

        #endregion
    }
}
