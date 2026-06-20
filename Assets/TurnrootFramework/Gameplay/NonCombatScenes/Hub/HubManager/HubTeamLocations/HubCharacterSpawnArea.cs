using System;
using Turnroot.Characters;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    [Serializable]
    public struct UnitSpawnEntry
    {
        [Tooltip("Transform where the unit model is placed in the hub.")]
        public Transform UnitSpawnPoint;

        [Tooltip(
            "Transform used for the avatar model spawn and as the point the unit looks toward."
        )]
        public Transform AvatarPoint;
    }

    public class HubCharacterSpawnArea : MonoBehaviour
    {
        public HubSublocationName LocationName;
        public UnitSpawnEntry[] UnitSpawnPoints;

        [HideInInspector]
        public CharacterInstance[] CharactersPresent;
    }
}
