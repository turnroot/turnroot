using System;
using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Locations;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    /// <summary>
    /// Partial class containing methods for managing battle participants, adjacency, and targets in range.
    /// </summary>
    public partial class BattleContext : MonoBehaviour
    {
        public List<CharacterInstance> GetCurrentTargets() =>
            Participants?.GetCurrentTargets() ?? new List<CharacterInstance>();

        public List<CharacterInstance> GetCurrentTargets(BattleTargetIntent intent)
        {
            var targets = new List<CharacterInstance>();
            var actor = Unit?.UnitInstance;
            if (actor == null)
            {
                return targets;
            }

            foreach (var target in Participants.GetCurrentTargets())
            {
                if (target != null && IsEligibleTarget(actor, target, intent))
                {
                    targets.Add(target);
                }
            }
            return targets;
        }

        public List<CharacterInstance> GetPotentialTargets(
            BattleTargetIntent intent,
            Func<CharacterInstance, bool> isInRange
        )
        {
            var targets = new List<CharacterInstance>();
            GetPotentialTargets(intent, isInRange, targets);
            return targets;
        }

        public void GetPotentialTargets(
            BattleTargetIntent intent,
            Func<CharacterInstance, bool> isInRange,
            List<CharacterInstance> targets
        )
        {
            if (isInRange == null)
            {
                throw new ArgumentNullException(nameof(isInRange));
            }
            if (targets == null)
            {
                throw new ArgumentNullException(nameof(targets));
            }
            if (intent != BattleTargetIntent.Offensive && intent != BattleTargetIntent.Supportive)
            {
                throw new ArgumentOutOfRangeException(nameof(intent), intent, null);
            }

            targets.Clear();
            var actor = Unit?.UnitInstance;
            if (actor == null)
            {
                this.LogWarning("GetPotentialTargets: No active unit set");
                return;
            }

            AddPotentialTargets(Participants.Allies, actor, intent, isInRange, targets);
            AddPotentialTargets(Participants.Enemies, actor, intent, isInRange, targets);
            AddPotentialTargets(Participants.ThirdParty, actor, intent, isInRange, targets);
        }

        private void AddPotentialTargets(
            List<CharacterInstance> candidates,
            CharacterInstance actor,
            BattleTargetIntent intent,
            Func<CharacterInstance, bool> isInRange,
            List<CharacterInstance> targets
        )
        {
            if (candidates == null)
            {
                return;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                var target = candidates[i];
                if (
                    target != null
                    && !target.IsDefeatedInCurrentBattle
                    && IsEligibleTarget(actor, target, intent)
                    && isInRange(target)
                )
                {
                    targets.Add(target);
                }
            }
        }

        private bool IsEligibleTarget(
            CharacterInstance actor,
            CharacterInstance target,
            BattleTargetIntent intent
        )
        {
            return intent == BattleTargetIntent.Offensive
                ? CanAttack(actor, target)
                : intent != BattleTargetIntent.Supportive
                ? throw new ArgumentOutOfRangeException(nameof(intent), intent, null)
                : IsPlayerControlledUnit(actor)
                ? IsPlayerControlledUnit(target)
                : IsEnemyUnit(actor) ? IsEnemyUnit(target) : IsThirdPartyUnit(actor) && IsThirdPartyUnit(target);
        }

        public void SetCurrentTargets(IEnumerable<CharacterInstance> targets)
        {
            ClearCurrentTargets();
            if (targets == null)
            {
                return;
            }

            foreach (var target in targets)
            {
                if (target == null)
                {
                    continue;
                }

                if (IsEnemyUnit(target))
                {
                    Participants.CurrentTargetedEnemies.Add(target);
                }
                else if (IsPlayerControlledUnit(target))
                {
                    Participants.CurrentTargetedAllies.Add(target);
                }
                else if (IsThirdPartyUnit(target))
                {
                    Participants.ThirdPartyTargets.Add(target);
                }
                else
                {
                    this.LogWarning(
                        $"SetCurrentTargets: {target.CharacterTemplate?.DisplayName} is not registered as a battle participant"
                    );
                }
            }
        }

        public void ClearCurrentTargets()
        {
            Participants.CurrentTargetedEnemies?.Clear();
            Participants.CurrentTargetedAllies?.Clear();
            Participants.ThirdPartyTargets?.Clear();
        }

        public void UpdateTargetsInRange()
        {
            if (Unit?.UnitInstance == null)
            {
                this.LogWarning("UpdateTargetsInRange: No active unit set");
                return;
            }

            var activeUnit = Unit.UnitInstance;
            var weapon = activeUnit.GetEquippedWeapon();

            if (weapon?.Template == null)
            {
                ClearTargetsInRange();
                return;
            }

            if (!TryGetValidTilesForUnit(activeUnit, out _, out var attackTiles))
            {
                this.LogWarning(
                    $"UpdateTargetsInRange: Failed to get valid tiles for {activeUnit.CharacterTemplate.DisplayName}"
                );
                ClearTargetsInRange();
                return;
            }

            ClearTargetsInRange();
            var targetsInRange = GetPotentialTargets(
                BattleTargetIntent.Offensive,
                target =>
                {
                    var targetPoint = target.UnitPositionToMapGridPoint(
                        target.MapGridPosition,
                        MapGrid
                    );
                    return targetPoint != null && attackTiles.ContainsKey(targetPoint);
                }
            );

            foreach (var target in targetsInRange)
            {
                if (IsEnemyUnit(target))
                {
                    Participants.EnemiesInRange.Add(target);
                }
                else if (IsPlayerControlledUnit(target))
                {
                    Participants.AlliesInRange.Add(target);
                }
                else if (IsThirdPartyUnit(target))
                {
                    Participants.ThirdPartyInRange.Add(target);
                }
            }

            this.LogInfo(
                $"UpdateTargetsInRange: Found {targetsInRange.Count} targets in range for {activeUnit.CharacterTemplate.DisplayName}"
            );
        }

        public void UpdateAdjacentUnits()
        {
            if (Unit?.UnitInstance == null)
            {
                this.LogWarning("UpdateAdjacentUnits: No active unit set");
                return;
            }

            var activeUnit = Unit.UnitInstance;

            Participants.AdjacentUnits = new Adjacency(activeUnit);

            if (MapGrid == null)
            {
                this.LogError("UpdateAdjacentUnits: MapGrid is null");
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
            Participants.EnemiesInRange.Clear();
            Participants.AlliesInRange.Clear();
            Participants.ThirdPartyInRange.Clear();
        }

        public void ClearAdjacentUnits() => Participants.AdjacentUnits = new Adjacency(null);

        public void ClearParticipantDynamicData()
        {
            ClearCurrentTargets();
            ClearTargetsInRange();
            ClearAdjacentUnits();
        }

        #region Third-Party Allegiance Helpers

        public bool IsEnemyUnit(CharacterInstance unit) =>
            unit != null && Participants.Enemies.Contains(unit);

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
