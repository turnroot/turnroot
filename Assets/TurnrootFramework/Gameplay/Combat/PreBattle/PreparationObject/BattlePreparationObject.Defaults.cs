using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.PreBattle
{
    public partial class BattlePreparationObject
    {
        #region Default Placements

        public void StoreDefaultPlacements() =>
            _defaultPlacements = new Dictionary<Vector2Int, CharacterData>(placements);

        public OperationResult ResetToDefaultPlacements()
        {
            if (_defaultPlacements == null || _defaultPlacements.Count == 0)
            {
                return OperationResult.Failure("No default placements to restore");
            }

            placements = new Dictionary<Vector2Int, CharacterData>(_defaultPlacements);
            CurrentPlacementState = PlacementState.DefaultPlaced;

            StartingPositionsComponent?.DespawnAllModels();
            ClearAllModelTracking();

            Brain?.PublishPlacementsInitialized();

            return OperationResult.Successful();
        }

        #endregion
    }
}
