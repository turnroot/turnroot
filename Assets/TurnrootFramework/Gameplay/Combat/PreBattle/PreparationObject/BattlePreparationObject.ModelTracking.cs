using System.Collections.Generic;
using Turnroot.Characters;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.PreBattle
{
    public partial class BattlePreparationObject
    {
        #region Model Tracking State

        // Model tracking - maps position to the spawned GameObject
        private Dictionary<Vector2Int, GameObject> _positionToModel = new();

        // Reverse lookup - maps position to unit ID for quick lookups
        private Dictionary<Vector2Int, string> _positionToUnitId = new();

        // Forward lookup - maps unit ID to position for reverse queries
        private Dictionary<string, Vector2Int> _unitIdToPosition = new();

        // Default placements before user customization (for reset functionality)
        private Dictionary<Vector2Int, CharacterData> _defaultPlacements = new();

        #endregion

        #region Model Queries

        /// <summary>
        /// Get the model GameObject at a specific position.
        /// </summary>
        public GameObject GetModelAtPosition(Vector2Int position) =>
            _positionToModel.TryGetValue(position, out var model) ? model : null;

        /// <summary>
        /// Get the model GameObject for a specific unit ID.
        /// </summary>
        public GameObject GetModelForUnit(string unitId)
        {
            return string.IsNullOrEmpty(unitId) ? null
                : _unitIdToPosition.TryGetValue(unitId, out var position)
                    ? GetModelAtPosition(position)
                : null;
        }

        /// <summary>
        /// Get the unit ID at a specific position.
        /// </summary>
        public string GetUnitIdAtPosition(Vector2Int position) =>
            _positionToUnitId.TryGetValue(position, out var unitId) ? unitId : null;

        /// <summary>
        /// Get the position where a unit ID is located.
        /// </summary>
        public Vector2Int? GetPositionForUnit(string unitId)
        {
            return string.IsNullOrEmpty(unitId) ? null
                : _unitIdToPosition.TryGetValue(unitId, out var position) ? position
                : (Vector2Int?)null;
        }

        /// <summary>
        /// Check if a model exists at a position.
        /// </summary>
        public bool HasModelAtPosition(Vector2Int position) =>
            _positionToModel.ContainsKey(position) && _positionToModel[position] != null;

        /// <summary>
        /// Get all spawned models with their positions.
        /// </summary>
        public IEnumerable<(Vector2Int position, GameObject model, string unitId)> GetAllModels()
        {
            foreach (var kvp in _positionToModel)
            {
                if (kvp.Value != null)
                {
                    var unitId = GetUnitIdAtPosition(kvp.Key);
                    yield return (kvp.Key, kvp.Value, unitId);
                }
            }
        }

        #endregion
    }
}
