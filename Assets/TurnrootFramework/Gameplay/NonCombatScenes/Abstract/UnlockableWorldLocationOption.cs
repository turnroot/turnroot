using System;
using Turnroot.Gameplay.NonCombatScenes.Hub;
using Turnroot.UI;
using UnityEngine;

namespace Turnroot.NonCombatScenes.Abstract
{
    [Serializable]
    public struct UnlockableWorldLocationOption
    {
        public UiChoice Choice;

        [Tooltip("Location name used to update CurrentLocationName on travel.")]
        public HubSublocationName LocationName;
        public UnlockableWorldLocation UnlockableLocation;

        [HideInInspector]
        public bool available;
    }
}
