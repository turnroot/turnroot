using System.Collections.Generic;
using Turnroot.Characters;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.PreBattle
{
    public static class BattlePlacementSync
    {
        public static void ApplyPlacements(
            Turnroot.Gameplay.Brain.Brain brain,
            Dictionary<Vector2Int, CharacterData> placements,
            bool persist,
            bool forceApplyPlacementsOnLoad = false
        )
        {
            var gw = brain?.gamewideContextBrain;
            if (gw == null)
            {
                return;
            }

            var persistent =
                gw.GamewidePersistentPlayerRoster
                ?? gw.CreateOrRecallGamewidePersistentPlayerRoster();
            if (persistent == null)
            {
                return;
            }

            var runtimeInstance = gw.GetOrCreatePlayerTeamRoster(persistent);
            if (runtimeInstance == null)
            {
                return;
            }

            var list = new List<Characters.Roster.UnitPlacement>();
            foreach (var kvp in placements)
            {
                var pos = kvp.Key;
                var data = kvp.Value;
                if (data == null)
                {
                    continue;
                }

                var up = new Characters.Roster.UnitPlacement
                {
                    CharacterData = data,
                    SpawnPosition = pos,
                    Order = list.Count,
                };
                up.SetStatus(Characters.Roster.UnitStatus.NotSpawned);
                up.SetActiveRightNow(true);
                list.Add(up);
            }

            runtimeInstance.ApplyDecodedPlacements(list.ToArray());

            if (persist)
            {
                var lastSaved = forceApplyPlacementsOnLoad ? 2 : 1;
                brain?.PublishSavePlayerRosterRequested(lastSaved);
            }
        }
    }
}
