using System;
using Turnroot.Characters;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class Brain
    {
        #region Character Spawn Events

        public event Action<CharacterInstance, Vector2Int> OnCharacterSpawned;
        public event Action<CharacterInstance, Vector2Int> OnCharacterRemovedFromSpawn;
        public event Action<string> OnUniqueEnemySpawnRequested;

        public void PublishCharacterSpawned(CharacterInstance character, Vector2Int position) =>
            OnCharacterSpawned?.Invoke(character, position);

        public void PublishCharacterRemovedFromSpawn(
            CharacterInstance character,
            Vector2Int position
        ) => OnCharacterRemovedFromSpawn?.Invoke(character, position);

        public void PublishUniqueEnemySpawn(string enemyName) =>
            OnUniqueEnemySpawnRequested?.Invoke(enemyName);

        #endregion
    }
}
