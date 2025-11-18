using System;
using Turnroot.Characters;
using UnityEngine;

namespace Turnroot.Maps.Components.Grids
{
    [System.Serializable]
    public class SpawnPoint
    {
        /* ---------------------------- Spawn point data ---------------------------- */
        [SerializeField, HideInInspector]
        private bool _isPossibleAllySpawnPoint = false;

        [SerializeField, HideInInspector]
        private bool _isPossibleAvatarSpawnPoint = false;

        [SerializeField, HideInInspector]
        private bool _isPossibleEnemySpawnPoint = false;

        [SerializeField, HideInInspector]
        private CharacterInstance[] _possibleSpawnedCharacters = null;

        [SerializeField, HideInInspector]
        private CharacterInstance _spawnedCharacter = null;

        [SerializeField, HideInInspector]
        private bool _isOccupied = false;
        public bool IsPossibleAllySpawnPoint
        {
            get => _isPossibleAllySpawnPoint;
            private set => _isPossibleAllySpawnPoint = value;
        }

        public bool IsPossibleAvatarSpawnPoint
        {
            get => _isPossibleAvatarSpawnPoint;
            private set => _isPossibleAvatarSpawnPoint = value;
        }

        public bool IsPossibleEnemySpawnPoint
        {
            get => _isPossibleEnemySpawnPoint;
            private set => _isPossibleEnemySpawnPoint = value;
        }

        public CharacterInstance[] PossibleSpawnedCharacters
        {
            get => _possibleSpawnedCharacters;
            set => _possibleSpawnedCharacters = value;
        }

        public CharacterInstance SpawnedCharacter
        {
            get => _spawnedCharacter;
            set
            {
                _spawnedCharacter = value;
                _isOccupied = value != null;
            }
        }

        public bool IsOccupied
        {
            get => _isOccupied;
            private set => _isOccupied = value;
        }

        /* ------------------------------ Events ---------------------------------- */
        public event Action<CharacterInstance> OnCharacterSpawned;
        public event Action<CharacterInstance> OnCharacterRemoved;
        public event Action<bool, bool, bool> OnFlagsChanged;

        /* ------------------------------ Spawn methods ----------------------------- */
        public void SpawnCharacter(CharacterInstance character)
        {
            if (character == null)
                return;

            SpawnedCharacter = character;
            IsOccupied = true;
            OnCharacterSpawned?.Invoke(character);
        }

        public void RemoveCharacter()
        {
            if (SpawnedCharacter == null)
                return;

            var removedCharacter = SpawnedCharacter;
            SpawnedCharacter = null;
            IsOccupied = false;
            OnCharacterRemoved?.Invoke(removedCharacter);
        }

        /* ------------------------------ Flags Setter ------------------------------- */
        public void SetFlags(bool isAllySpawn, bool isAvatarSpawn, bool isEnemySpawn)
        {
            // Enforce simple mutual exclusions used elsewhere in the codebase.
            if (isAllySpawn && isAvatarSpawn)
            {
                Debug.LogWarning(
                    "A spawn point cannot be both an ally spawn and an avatar spawn. Defaulting to ally spawn."
                );
                isAvatarSpawn = false;
            }
            if (isEnemySpawn && isAvatarSpawn)
            {
                Debug.LogWarning(
                    "A spawn point cannot be both an enemy spawn and an avatar spawn. Defaulting to enemy spawn."
                );
                isAvatarSpawn = false;
            }
            if (isAllySpawn && isEnemySpawn)
            {
                Debug.LogWarning(
                    "A spawn point cannot be both an ally spawn and an enemy spawn. Defaulting to ally spawn."
                );
                isEnemySpawn = false;
            }

            var changed =
                _isPossibleAllySpawnPoint != isAllySpawn
                || _isPossibleAvatarSpawnPoint != isAvatarSpawn
                || _isPossibleEnemySpawnPoint != isEnemySpawn;

            _isPossibleAllySpawnPoint = isAllySpawn;
            _isPossibleAvatarSpawnPoint = isAvatarSpawn;
            _isPossibleEnemySpawnPoint = isEnemySpawn;

            if (changed)
                OnFlagsChanged?.Invoke(
                    _isPossibleAllySpawnPoint,
                    _isPossibleAvatarSpawnPoint,
                    _isPossibleEnemySpawnPoint
                );
        }
    }
}
