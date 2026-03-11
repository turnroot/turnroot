using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class BattleInputControllerBrain : BrainComponent
    {
        #region Navigation Helpers

        private static Vector2 RotateVectorBy90StepsCW(Vector2 v, int steps)
        {
            steps = ((steps % 4) + 4) % 4;
            return steps switch
            {
                0 => v,
                1 => new Vector2(v.y, -v.x),
                2 => new Vector2(-v.x, -v.y),
                3 => new Vector2(-v.y, v.x),
                _ => v,
            };
        }

        private static Vector2 SnapDirectionToFour(Vector2 v)
        {
            if (v.magnitude < 0.0001f)
            {
                return Vector2.zero;
            }

            var angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
            // Snap to nearest 45 degrees (8 directions including diagonals)
            var snapped = Mathf.Round(angle / 45f) * 45f;
            var rad = snapped * Mathf.Deg2Rad;
            // Round cosine/sine to avoid floating point imprecision and yield exact integer direction vectors
            return new Vector2(Mathf.Round(Mathf.Cos(rad)), Mathf.Round(Mathf.Sin(rad)));
        }

        #endregion
    }
}
