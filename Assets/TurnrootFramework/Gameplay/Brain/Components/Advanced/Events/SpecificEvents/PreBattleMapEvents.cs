using System;
using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Maps;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class Brain
    {
        #region Pre-Battle Map Events

        public event Action<MapGrid> OnPreBattleMapReady;
        public event Action<Vector2Int, CharacterInstance> OnPreBattleSpawnPositionSelected;
        public event Action<CharacterInstance> OnPreBattleSpawnPositionCanceled;

        public event Action OnUiPlayerIsTryingToUnselectLastUnit;

        public void PublishUiPlayerIsTryingToUnselectLastUnit() =>
            OnUiPlayerIsTryingToUnselectLastUnit?.Invoke();

        public void PublishPreBattleMapReady(MapGrid mapGrid) =>
            OnPreBattleMapReady?.Invoke(mapGrid);

        public event Action<
            Dictionary<MapGridPoint, float>,
            Dictionary<MapGridPoint, float>
        > OnValidTilesComputed;

        public void PublishValidTilesComputed(
            Dictionary<MapGridPoint, float> moveTiles,
            Dictionary<MapGridPoint, float> attackTiles
        ) => OnValidTilesComputed?.Invoke(moveTiles, attackTiles);

        public void PublishPreBattleSpawnPositionSelected(
            Vector2Int position,
            CharacterInstance unit
        ) => OnPreBattleSpawnPositionSelected?.Invoke(position, unit);

        public void PublishPreBattleSpawnPositionCanceled(CharacterInstance unit) =>
            OnPreBattleSpawnPositionCanceled?.Invoke(unit);

        #endregion
    }
}
