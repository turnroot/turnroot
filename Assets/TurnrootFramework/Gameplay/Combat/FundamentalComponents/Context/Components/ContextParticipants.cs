using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Locations;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    /// <summary>
    /// Partial class containing methods for managing battle participants, adjacency, and targets in range.
    /// </summary>
    public partial class BattleContext : MonoBehaviour
    {
        public void UpdateTargetsInRange()
        {
            if (Unit?.UnitInstance == null)
            {
                TurnrootLogger.Log(
                    "BattleContext.UpdateTargetsInRange: No active unit set",
                    TurnrootLogger.LogLevel.Warning
                );
                return;
            }

            var activeUnit = Unit.UnitInstance;
            var weapon = activeUnit.GetEquippedWeapon();

            if (weapon?.Template == null)
            {
                Participants.TargetsInRange.Clear();
                return;
            }

            if (!TryGetValidTilesForUnit(activeUnit, out _, out var attackTiles))
            {
                TurnrootLogger.Log(
                    $"BattleContext.UpdateTargetsInRange: Failed to get valid tiles for {activeUnit.CharacterTemplate.DisplayName}",
                    TurnrootLogger.LogLevel.Warning
                );
                Participants.TargetsInRange.Clear();
                return;
            }

            Participants.TargetsInRange.Clear();

            // Gather all potential enemies based on unit allegiance and third-party settings
            var potentialTargets = new List<CharacterInstance>();

            if (IsPlayerControlledUnit(activeUnit))
            {
                // Player can attack enemies
                potentialTargets.AddRange(Participants.Targets);

                // Player can also attack third party if they fight allies
                if (Brain.battleBrain.BattleObject.ThirdPartyFightsAllies)
                {
                    potentialTargets.AddRange(Participants.ThirdParty);
                }
            }
            else if (IsEnemyUnit(activeUnit))
            {
                // Enemies can attack player allies
                potentialTargets.AddRange(Participants.Allies);

                // Enemies can also attack third party if they fight enemies
                if (Brain.battleBrain.BattleObject.ThirdPartyFightsEnemies)
                {
                    potentialTargets.AddRange(Participants.ThirdParty);
                }
            }
            else if (IsThirdPartyUnit(activeUnit))
            {
                // Third party attacks based on allegiance flags
                if (Brain?.battleBrain.BattleObject.ThirdPartyFightsAllies ?? false)
                {
                    potentialTargets.AddRange(Participants.Allies);
                }
                if (Brain?.battleBrain.BattleObject.ThirdPartyFightsEnemies ?? false)
                {
                    potentialTargets.AddRange(Participants.Targets);
                }
            }

            // Check which potential targets are on tiles within attack range
            foreach (var enemy in potentialTargets)
            {
                if (enemy == null || enemy.IsDefeatedInCurrentBattle)
                {
                    continue;
                }

                var enemyPoint = enemy.UnitPositionToMapGridPoint(enemy.MapGridPosition, MapGrid);

                if (enemyPoint != null && attackTiles.ContainsKey(enemyPoint))
                {
                    Participants.TargetsInRange.Add(enemy);
                }
            }

            TurnrootLogger.Log(
                $"BattleContext.UpdateTargetsInRange: Found {Participants.TargetsInRange.Count} targets in range for {activeUnit.CharacterTemplate.DisplayName}"
            );
        }

        public void UpdateAdjacentUnits()
        {
            if (Unit?.UnitInstance == null)
            {
                TurnrootLogger.Log(
                    "BattleContext.UpdateAdjacentUnits: No active unit set",
                    TurnrootLogger.LogLevel.Warning
                );
                return;
            }

            var activeUnit = Unit.UnitInstance;

            Participants.AdjacentUnits = new Adjacency(activeUnit);

            if (MapGrid == null)
            {
                TurnrootLogger.Log(
                    "BattleContext.UpdateAdjacentUnits: MapGrid is null",
                    TurnrootLogger.LogLevel.Error
                );
                return;
            }

            var unitPos = activeUnit.MapGridPosition;

            var unitPositions = GetCurrentUnitPositions(invalidateCache: false);

            var adjacentOffsets = new Dictionary<Direction, Vector2Int>
            {
                { Direction.TopLeft, new Vector2Int(-1, -1) },
                { Direction.TopCenter, new Vector2Int(-1, 0) },
                { Direction.TopRight, new Vector2Int(-1, 1) },
                { Direction.CenterLeft, new Vector2Int(0, -1) },
                { Direction.CenterRight, new Vector2Int(0, 1) },
                { Direction.BottomLeft, new Vector2Int(1, -1) },
                { Direction.BottomCenter, new Vector2Int(1, 0) },
                { Direction.BottomRight, new Vector2Int(1, 1) },
            };

            foreach (var (direction, offset) in adjacentOffsets)
            {
                var checkPos = unitPos + offset;

                if (unitPositions.TryGetValue(checkPos, out var adjacentUnit))
                {
                    if (
                        adjacentUnit != null
                        && !adjacentUnit.IsDefeatedInCurrentBattle
                        && adjacentUnit != activeUnit
                    )
                    {
                        Participants.AdjacentUnits.SetUnit(direction, adjacentUnit);
                    }
                }
            }
        }

        public void ClearTargetsInRange()
        {
            Participants.TargetsInRange.Clear();
            Participants.AlliesInRange.Clear();
        }

        public void ClearAdjacentUnits() => Participants.AdjacentUnits = new Adjacency(null);

        public void ClearParticipantDynamicData()
        {
            ClearTargetsInRange();
            ClearAdjacentUnits();
        }

        #region Third-Party Allegiance Helpers

        public bool IsEnemyUnit(CharacterInstance unit) =>
            unit != null && Participants.Targets.Contains(unit);

        public bool IsThirdPartyUnit(CharacterInstance unit) =>
            unit != null && Participants.ThirdParty.Contains(unit);

        /// <summary>
        /// Determines if an attacker can attack a target based on team allegiances and third-party settings.
        /// </summary>
        public bool CanAttack(CharacterInstance attacker, CharacterInstance target)
        {
            if (attacker == null || target == null || attacker == target)
            {
                return false;
            }

            var battleObject = Brain?.battleBrain.BattleObject;

            // Player attacks enemies and potentially third party
            if (IsPlayerControlledUnit(attacker))
            {
                return IsEnemyUnit(target)
                    || IsThirdPartyUnit(target) && (battleObject?.ThirdPartyFightsAllies ?? false);
            }

            // Enemy attacks players and potentially third party
            if (IsEnemyUnit(attacker))
            {
                return IsPlayerControlledUnit(target)
                    || IsThirdPartyUnit(target) && (battleObject?.ThirdPartyFightsEnemies ?? false);
            }

            // Third party attacks based on allegiance flags
            return IsThirdPartyUnit(attacker)
                && (
                    IsPlayerControlledUnit(target)
                        && (battleObject?.ThirdPartyFightsAllies ?? false)
                    || IsEnemyUnit(target) && (battleObject?.ThirdPartyFightsEnemies ?? false)
                    || IsThirdPartyUnit(target)
                        && (battleObject?.ThirdPartyFightsAllies ?? false)
                        && (battleObject?.ThirdPartyFightsEnemies ?? false)
                );
        }

        /// <summary>
        /// Determines if two units are allies based on team membership and third-party settings.
        /// </summary>
        public bool AreAllies(CharacterInstance unit1, CharacterInstance unit2)
        {
            if (unit1 == null || unit2 == null || unit1 == unit2)
            {
                return false;
            }

            var battleObject = Brain?.battleBrain.BattleObject;

            // Same team = allies
            if (IsPlayerControlledUnit(unit1) && IsPlayerControlledUnit(unit2))
            {
                return true;
            }
            if (IsEnemyUnit(unit1) && IsEnemyUnit(unit2))
            {
                return true;
            }
            if (IsThirdPartyUnit(unit1) && IsThirdPartyUnit(unit2))
            {
                // Third party members are only allies to each other if they don't fight everyone
                var fightsEveryone =
                    (battleObject?.ThirdPartyFightsAllies ?? false)
                    && (battleObject?.ThirdPartyFightsEnemies ?? false);
                return !fightsEveryone;
            }

            // Cross-team alliances based on third party settings
            return IsPlayerControlledUnit(unit1) && IsThirdPartyUnit(unit2)
                    ? !(battleObject?.ThirdPartyFightsAllies ?? false)
                : IsThirdPartyUnit(unit1) && IsPlayerControlledUnit(unit2)
                    ? !(battleObject?.ThirdPartyFightsAllies ?? false)
                : IsEnemyUnit(unit1) && IsThirdPartyUnit(unit2)
                    ? !(battleObject?.ThirdPartyFightsEnemies ?? false)
                : IsThirdPartyUnit(unit1)
                    && IsEnemyUnit(unit2)
                    && !(battleObject?.ThirdPartyFightsEnemies ?? false);
        }

        #endregion
    }
}
