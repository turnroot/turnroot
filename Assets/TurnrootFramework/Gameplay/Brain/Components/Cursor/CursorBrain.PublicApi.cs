namespace Turnroot.Gameplay.Brain
{
    public partial class CursorBrain
    {
        #region Public Query API (moved)

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
                && _brain?.battleBrain?.BattleObject?.Context != null
            )
            {
                var cache = _brain.battleBrain.BattleObject.Context.GetCurrentUnitPositions();
                return cache.TryGetValue(CursorPosition.CoordinatesInt, out var unit) ? unit : null;
            }

            // In pre-battle context, check preparation object
            if (
                _currentContext == CursorContext.PreBattle
                && _brain?.battleBrain?.PreparationObject != null
            )
            {
                // TODO: Query pre-battle unit placements
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
