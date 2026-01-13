using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class CursorBrain
    {
        #region Utility Methods

        private Vector2Int GetGridMovementFromDirection(Vector2 direction, float threshold)
        {
            Vector2Int gridMovement = Vector2Int.zero;

            // Allow both axes to trigger movement when each exceeds the threshold
            if (Mathf.Abs(direction.x) > threshold)
            {
                gridMovement.x = direction.x > 0 ? 1 : -1;
            }

            if (Mathf.Abs(direction.y) > threshold)
            {
                gridMovement.y = direction.y > 0 ? 1 : -1;
            }

            return gridMovement;
        }

        #endregion
    }
}
