using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class BattleInputControllerBrain : BrainComponent
    {
        #region Helper Methods

        private List<Vector2Int> GetValidMoveCoordinates() =>
            new(
                _validMoveTiles?.Keys.Select(k => k.CoordinatesInt)
                    ?? System.Array.Empty<Vector2Int>()
            );

        private List<Vector2Int> GetValidAttackCoordinates() =>
            new(
                _validAttackTiles?.Keys.Select(k => k.CoordinatesInt)
                    ?? System.Array.Empty<Vector2Int>()
            );

        #endregion
    }
}
