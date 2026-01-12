using System.Collections.Generic;
using Turnroot.Gameplay.Combat.PreBattle;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.UI.Components
{
    public class StartingPositions : MonoBehaviour
    {
        public List<GameObject> TileProjectors;
        public GameObject ActiveTileProjector;

        private MapGrid mapGrid;

        public OperationResult Initialize(BattlePreparationObject battlePreparationObject)
        {
            mapGrid = battlePreparationObject.MapGrid;
            var StartingPositions = mapGrid.PlayerTeamSpawnPoints;
            if (StartingPositions == null || StartingPositions.Count <= 0)
            {
                return OperationResult.Failure("Improper starting positions");
            }
            for (var i = 0; i < StartingPositions.Count; i++)
            {
                var coordinates = StartingPositions[i];
                SetUpDecalProjector(i, i == 0, coordinates);
            }
            return OperationResult.SuccessResult();
        }

        private void SetUpDecalProjector(int index, bool ActiveTile, Vector2Int tileCoordinates)
        {
            var projector = TileProjectors[index];
            var worldPosition = mapGrid.GetTerrainAdjustedWorldPosition(tileCoordinates);
            if (ActiveTile)
            {
                ActiveTileProjector.SetActive(true);
                ActiveTileProjector.transform.position = worldPosition + new Vector3(0, 1f, 0f);
                // The active projector is stacked on the active tile on the normal projector
            }

            projector.transform.position = worldPosition + new Vector3(0, 1f, 0f);
        }
    }
}
