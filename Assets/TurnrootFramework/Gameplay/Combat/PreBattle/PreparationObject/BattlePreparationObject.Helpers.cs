using Turnroot.Characters;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.PreBattle
{
    public partial class BattlePreparationObject
    {
        #region Helpers

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

        private bool IsPlayerSpawnPoint(Vector2Int pos) =>
            PlayerTeamSpawnPoints != null && PlayerTeamSpawnPoints.Contains(pos);

        private bool TryGetPlacement(Vector2Int pos, out CharacterData data)
        {
            data = null;
            return placements != null && placements.TryGetValue(pos, out data);
        }

        #endregion
    }
}
