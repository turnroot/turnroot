using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Maps;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    public partial class BattleContext
    {
        #region Repair & Roster Helpers

        public void RepairUnitPositionsFromRoster()
        {
#if !(UNITY_EDITOR || DEVELOPMENT_BUILD)
            TurnrootLogger.Log(
                "RepairUnitPositionsFromRoster called in release build; skipping repair. Enable development build to perform repairs.",
                TurnrootLogger.LogLevel.Warning
            );
            return;
#endif
            var roster = GetPlayerTeamRosterInstance();
            if (roster == null)
            {
                return;
            }

            var placements = roster.GetPlacements();
            var occupancy = BuildOccupancyMapTyped(roster, placements);

            foreach (var p in placements)
            {
                var inst = roster.GetInstanceFor(p.CharacterData);
                if (inst == null)
                {
                    continue;
                }

                var currentPoint = inst.UnitPositionToMapGridPoint(inst.MapGridPosition, MapGrid);
                var desiredPoint = MapGrid.GetGridPoint(p.SpawnPosition.x, p.SpawnPosition.y);
                if (desiredPoint == null)
                {
                    continue;
                }

                var isInvalid = currentPoint == null;
                var isDuplicate = false;
                if (occupancy.TryGetValue(inst.MapGridPosition, out List<CharacterInstance> owners))
                {
                    isDuplicate = owners.Count > 1;
                }

                if (!isInvalid && !isDuplicate)
                {
                    continue;
                }

                TurnrootLogger.Log(
                    $"RepairUnitPositionsFromRoster: Repairing {inst.CharacterTemplate.DisplayName} MapGridPosition from {inst.MapGridPosition} to {p.SpawnPosition} (invalid={isInvalid}, duplicate={isDuplicate})",
                    TurnrootLogger.LogLevel.Warning
                );

                RemoveOccupiedSafeTyped(currentPoint);
                SetOccupiedOrFallbackTyped(desiredPoint, inst, p.SpawnPosition);
            }
        }

        private PlayerTeamRosterInstance GetPlayerTeamRosterInstance()
        {
            var bb = Brain?.battleBrain;
            var battleObj = bb?.BattleObject;
            return battleObj?.PlayerTeamRoster as Turnroot.Characters.PlayerTeamRosterInstance;
        }

        private Dictionary<Vector2Int, List<CharacterInstance>> BuildOccupancyMapTyped(
            PlayerTeamRosterInstance roster,
            Characters.Roster.UnitPlacement[] placements
        )
        {
            var occupancy = new Dictionary<Vector2Int, List<CharacterInstance>>();
            foreach (var placement in placements)
            {
                if (placement == null)
                {
                    continue;
                }

                var i2 = roster.GetInstanceFor(placement.CharacterData);
                if (i2 == null)
                {
                    continue;
                }

                if (!occupancy.TryGetValue(i2.MapGridPosition, out List<CharacterInstance> list))
                {
                    list = new List<CharacterInstance>();
                    occupancy[i2.MapGridPosition] = list;
                }
                list.Add(i2);
            }
            return occupancy;
        }

        private void RemoveOccupiedSafeTyped(MapGridPoint currentPoint)
        {
            if (currentPoint == null)
            {
                return;
            }

            var res = MapGrid.RemoveOccupied(currentPoint);
            if (!res.Success)
            {
                TurnrootLogger.Log(
                    $"RepairUnitPositionsFromRoster: RemoveOccupied failed: {res.ErrorMessage}",
                    TurnrootLogger.LogLevel.Warning
                );
            }
        }

        private void SetOccupiedOrFallbackTyped(
            MapGridPoint desiredPoint,
            CharacterInstance inst,
            Vector2Int fallbackPosition
        )
        {
            var setResult = MapGrid.SetOccupied(desiredPoint, inst);
            if (!setResult.Success)
            {
                TurnrootLogger.Log(
                    $"RepairUnitPositionsFromRoster: SetOccupied failed: {setResult.ErrorMessage}",
                    TurnrootLogger.LogLevel.Warning
                );
                inst.MapGridPosition = fallbackPosition;
            }
        }

        #endregion
    }
}
