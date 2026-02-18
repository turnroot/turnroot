using System;
using Turnroot.Characters;
using Turnroot.Gameplay.Maps;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class Brain
    {
        #region Character Movement Events

        public event Action<CharacterInstance, MapGridPoint> OnCharacterMoveStarted;
        public event Action<CharacterInstance, MapGridPoint> OnCharacterMoveCompleted;
        public event Action<CharacterInstance> OnPlayerMovePreviewStarted;
        public event Action<CharacterInstance, MapGridPoint> OnPlayerChoseMoveTile;
        public event Action<CharacterInstance, Vector2Int> OnCharacterVisitedTile;

        public void PublishCharacterMoveStarted(
            CharacterInstance character,
            MapGridPoint targetPoint
        ) => OnCharacterMoveStarted?.Invoke(character, targetPoint);

        public void PublishCharacterMoveCompleted(
            CharacterInstance character,
            MapGridPoint targetPoint
        ) => OnCharacterMoveCompleted?.Invoke(character, targetPoint);

        public void PublishCharacterVisitedTile(
            CharacterInstance character,
            Vector2Int tilePosition
        ) => OnCharacterVisitedTile?.Invoke(character, tilePosition);

        public void PublishPlayerMovePreviewStarted(CharacterInstance character) =>
            OnPlayerMovePreviewStarted?.Invoke(character);

        public void PublishPlayerChoseMoveTile(
            CharacterInstance character,
            MapGridPoint targetPoint
        ) => OnPlayerChoseMoveTile?.Invoke(character, targetPoint);

        #endregion
    }
}
