using Turnroot.Utilities;

namespace Turnroot.Gameplay.Brain
{
    public partial class CursorBrain
    {
        #region Public Query API

        /// <summary>
        /// Get the character instance at the current cursor position.
        /// </summary>
        public Characters.CharacterInstance GetUnitAtCursor()
        {
            if (CursorPosition == null)
            {
                return null;
            }

            // In battle context
            if (
                _currentContext == CursorContext.Battle
                && Brain.battleBrain?.BattleObject?.Context != null
            )
            {
                var cache = Brain.battleBrain.BattleObject.Context.GetCurrentUnitPositions();
                if (cache.TryGetValue(CursorPosition.CoordinatesInt, out var unit))
                {
                    TurnrootLogger.Log(
                        $"CursorBrain: Found unit {unit.CharacterTemplate.DisplayName} at cursor position"
                    );
                    return unit;
                }
                return null;
            }

            return null;
        }

        public bool IsCursorOnUnit(out Characters.CharacterInstance unit)
        {
            unit = GetUnitAtCursor();
            return unit != null;
        }

        public bool IsCursorOnValidSpawnPoint()
        {
            return _currentContext != CursorContext.PreBattle || CursorPosition == null
                ? false
                : _allowedPositions?.Contains(CursorPosition.CoordinatesInt) ?? false;
        }
        #endregion
    }
}
