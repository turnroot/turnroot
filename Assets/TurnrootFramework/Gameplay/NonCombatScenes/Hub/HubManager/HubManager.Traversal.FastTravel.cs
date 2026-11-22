using System;
using System.Collections;
using NaughtyAttributes;
using Turnroot.NonCombatScenes.Abstract;
using Turnroot.UI;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public partial class HubManager : MonoBehaviour
    {
        [Foldout("Fast Travel")]
        [InfoBox("UI fade shown when opening the traversal fast-travel location list.")]
        public UIFade FastTravelChoicesFade;

        [Foldout("Fast Travel")]
        [InfoBox("Ordered locations shown when ToggleDetails opens fast travel.")]
        public UnlockableWorldLocationOption[] FastTravelLocations;

        [Foldout("Fast Travel")]
        [Min(0f)]
        [Tooltip("Delay between starting travel FX and the actual teleport.")]
        public float FastTravelTeleportDelay = 0.65f;

        [Foldout("Fast Travel")]
        [Min(0f)]
        [Tooltip("Small delay after arrival FX before movement is restored.")]
        public float FastTravelRecoveryDelay = 0.2f;

        [Foldout("Fast Travel")]
        [InfoBox("Particle prefab spawned on the avatar model before teleport.")]
        public ParticleSystem FastTravelDepartureFxPrefab;

        [Foldout("Fast Travel")]
        [InfoBox("Particle prefab spawned on the avatar model after teleport.")]
        public ParticleSystem FastTravelArrivalFxPrefab;

        [Foldout("Fast Travel")]
        [InfoBox("Audio source used for fast-travel SFX.")]
        public AudioSource FastTravelAudioSource;

        [Foldout("Fast Travel")]
        [Tooltip("SFX played when fast travel begins.")]
        public AudioClip FastTravelDepartureClip;

        private UiChoice[] _fastTravelNavigableChoices;
        private UnlockableWorldLocationOption[] _fastTravelNavigableOptions;
        private int _fastTravelChoiceIndex;
        private bool _fastTravelMenuOpen;
        private bool _isFastTravelInProgress;
        private bool _isTraversalMovementLocked;
        private Coroutine _fastTravelRoutine;
        private FastTravelManager _fastTravelManager;

        private const float DefaultFxCleanupDelaySeconds = 2f;
        private const float LoopingFxMinimumLifetimeSeconds = 3f;
        private const float MinimumFxCleanupDelaySeconds = 1f;
        private const float FxCleanupSafetyBufferSeconds = 0.25f;




    }
}
