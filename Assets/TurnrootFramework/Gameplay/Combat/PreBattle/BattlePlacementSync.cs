using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.PreBattle
{
    public static class BattlePlacementSync
    {
        public static void ApplyPlacements(
            Brain.Brain brain,
            Dictionary<Vector2Int, CharacterData> placements,
            bool persist,
            bool forceApplyPlacementsOnLoad = false
        )
        {
            var gw = brain?.gamewideContextBrain;
            if (!ValidationHelper.ValidateNotNull(gw, nameof(gw)))
            {
                return;
            }

            var persistent =
                gw.GamewidePersistentPlayerRoster
                ?? gw.CreateOrRecallGamewidePersistentPlayerRoster();
            if (!ValidationHelper.ValidateNotNull(persistent, nameof(persistent)))
            {
                return;
            }

            var runtimeInstance = gw.GetOrCreatePlayerTeamRoster(persistent);
            if (!ValidationHelper.ValidateNotNull(runtimeInstance, nameof(runtimeInstance)))
            {
                return;
            }

            var decoded = ToDecodedPlacementArray(placements);
            runtimeInstance.ApplyDecodedPlacements(decoded);

            if (persist)
            {
                var lastSaved = forceApplyPlacementsOnLoad ? 2 : 1;
                brain?.PublishSavePlayerRosterRequested(lastSaved);
            }
        }

        public static Characters.Roster.UnitPlacement[] ToDecodedPlacementArray(
            Dictionary<Vector2Int, CharacterData> placements
        )
        {
            var list = new List<Characters.Roster.UnitPlacement>();
            if (!ValidationHelper.ValidateNotNull(placements, nameof(placements)))
            {
                return list.ToArray();
            }

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

            return list.ToArray();
        }
    }
}
