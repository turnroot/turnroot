using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.PreBattle
{
    public partial class BattlePreparationObject
    {
        private bool TryUseRuntimePlacements(
            GamewideContextBrain gw,
            PlayerTeamRoster persistent,
            PlayerTeamRosterInstance runtimeInstance
        )
        {
            if (gw == null || runtimeInstance == null)
            {
                return false;
            }

            var runtimePlacements = runtimeInstance.GetPlacements();

            if (runtimePlacements == null || runtimePlacements.Length == 0)
            {
                return false;
            }

            var candidate = new Dictionary<Vector2Int, CharacterInstance>();
            foreach (var p in runtimePlacements)
            {
                if (p == null || p.CharacterData == null)
                {
                    continue;
                }

                CharacterInstance inst = null;
                try
                {
                    inst =
                        runtimeInstance.GetInstanceFor(p.CharacterData)
                        ?? gw.FindInstanceByTemplate(p.CharacterData);
                }
                catch
                {
                    // Defensive: if runtime instance access fails, skip it and continue validation.
                    continue;
                }

                if (inst != null)
                {
                    candidate[p.SpawnPosition] = inst;
                }
            }

            // Validate runtime placements: positions must be valid spawn points and instances must exist
            bool invalidPlacementFound = false;
            if (candidate.Count > 0)
            {
                foreach (var kvp in candidate)
                {
                    var pos = kvp.Key;
                    if (PlayerTeamSpawnPoints == null || !PlayerTeamSpawnPoints.Contains(pos))
                    {
                        invalidPlacementFound = true;
                        break;
                    }

                    if (kvp.Value == null)
                    {
                        TurnrootLogger.Log(
                            $"InitializePlacements: Invalid runtime placement for missing instance at {pos}",
                            TurnrootLogger.LogLevel.Warning
                        );
                        invalidPlacementFound = true;
                        break;
                    }
                }
            }

            if (!invalidPlacementFound && candidate.Count > 0)
            {
                placements = candidate;
                TurnrootLogger.Log(
                    $"InitializePlacements: Using runtime placements ({placements.Count})",
                    TurnrootLogger.LogLevel.Info
                );
                CurrentPlacementState = PlacementState.DefaultPlaced;
                Brain?.PublishPlacementsInitialized();
                return true;
            }

            // Invalid runtime placements found — discard and fall back to default selection
            if (invalidPlacementFound)
            {
                placements = null;
            }

            return false;
        }

        private void ApplyPlacementsFromSelectedUnits(List<CharacterInstance> finalSelected)
        {
            placements = new Dictionary<Vector2Int, CharacterInstance>();

            int spawnPointCount = PlayerTeamSpawnPoints != null ? PlayerTeamSpawnPoints.Count : 0;
            int spawnLimit = System.Math.Min(finalSelected.Count, spawnPointCount);
            for (int i = 0; i < spawnLimit; i++)
            {
                if (i >= MaxPlayerTeamUnits)
                {
                    break;
                }

                var spawnPos = PlayerTeamSpawnPoints[i];
                var unit = finalSelected[i];
                placements[spawnPos] = unit;
            }

            foreach (var kvp in placements)
            {
                var unit = kvp.Value;
                if (unit != null)
                {
                    // Do not mark these as user-changed selections (markChanged: false)
                    SetBattleSelected(unit, true, publish: false, markChanged: false);
                }
            }

            CurrentPlacementState = PlacementState.DefaultPlaced;
            Brain?.PublishPlacementsInitialized();
        }
    }
}
