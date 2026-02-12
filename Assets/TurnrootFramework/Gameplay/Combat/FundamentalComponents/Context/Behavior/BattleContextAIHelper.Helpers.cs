using Turnroot.Gameplay.Maps;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    public partial class BattleContextAIHelper
    {
        private bool IsAttackable(MapGridPoint gp) =>
            gp != null && _reusableAttackTiles.ContainsKey(gp);

        private bool IsHealable(MapGridPoint gp) =>
            gp != null && _reusableHealTiles.ContainsKey(gp);

        private MapGridPoint DestinationFromTargetGridPoint(MapGridPoint targetGridPoint) =>
            _context.Unit.UnitInstance.UnitPositionToMapGridPoint(
                targetGridPoint.CoordinatesInt,
                _context.MapGrid
            );
    }
}
