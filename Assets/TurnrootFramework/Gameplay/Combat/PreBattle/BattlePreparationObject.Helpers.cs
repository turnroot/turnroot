using System.Collections.Generic;
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
            StartingPositionsComponent.SetSwap(to);
            (placements[from], placements[to]) = (placements[to], placements[from]);
            StartingPositionsComponent.SwapModels(from, to);
        }

        private void ApplyMove(Vector2Int from, Vector2Int to)
        {
            StartingPositionsComponent.SetSelected(to);
            placements[to] = placements[from];
            placements.Remove(from);
            StartingPositionsComponent.MoveModel(from, to);
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
