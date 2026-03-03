using Turnroot.Characters;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.PreBattle
{
    public partial class BattlePreparationObject
    {
        private (string name, string className, Sprite portrait) BuildUnitDisplayData(
            CharacterInstance unit
        )
        {
            if (unit == null)
            {
                return ("", "n/a", null);
            }

            var name = unit.CharacterTemplate?.DisplayName ?? "";
            var curClass = unit.GetCurrentClass();
            var className = curClass?.ClassData.Identity.ClassName;
            if (string.IsNullOrEmpty(className))
            {
                className = unit.CharacterTemplate?.StartingClass?.Identity.ClassName ?? "n/a";
            }

            var portrait = unit.CharacterTemplate?.DefaultPortrait?.RuntimeSprite;
            return (name, className, portrait);
        }

        private void ApplySwap(Vector2Int from, Vector2Int to)
        {
            // Capture data references BEFORE the swap so we can update instance positions.
            placements.TryGetValue(from, out var dataFrom);
            placements.TryGetValue(to, out var dateTo);

            StartingPositionsComponent.SetSwap(to);
            (placements[from], placements[to]) = (placements[to], placements[from]);
            StartingPositionsComponent.SwapModels(from, to);

            // Update MapGridPosition immediately so any code reading it before the
            // debounced model respawn fires sees the correct positions.
            var gw = Brain?.gamewideContextBrain;
            if (gw != null)
            {
                var instFrom = dataFrom != null ? gw.FindInstanceByTemplate(dataFrom) : null;
                var instTo   = dateTo   != null ? gw.FindInstanceByTemplate(dateTo)   : null;
                if (instFrom != null) instFrom.MapGridPosition = to;   // unit from 'from' is now at 'to'
                if (instTo   != null) instTo.MapGridPosition   = from; // unit from 'to'   is now at 'from'
            }
        }

        private void ApplyMove(Vector2Int from, Vector2Int to)
        {
            // Capture data reference BEFORE the move.
            placements.TryGetValue(from, out var dataFrom);

            StartingPositionsComponent.SetSelected(to);
            placements[to] = placements[from];
            placements.Remove(from);
            StartingPositionsComponent.MoveModel(from, to);

            // Update MapGridPosition immediately.
            var inst = dataFrom != null ? Brain?.gamewideContextBrain?.FindInstanceByTemplate(dataFrom) : null;
            if (inst != null) inst.MapGridPosition = to;
        }

        private bool IsPlayerSpawnPoint(Vector2Int pos) =>
            PlayerTeamSpawnPoints != null && PlayerTeamSpawnPoints.Contains(pos);

        private bool TryGetPlacement(Vector2Int pos, out CharacterData data)
        {
            data = null;
            return placements != null && placements.TryGetValue(pos, out data);
        }
    }
}
