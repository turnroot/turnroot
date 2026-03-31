using Turnroot.Gameplay.NonCombatScenes.Hub.Character;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    [RequireComponent(typeof(HubManager))]
    public partial class SpecificUiHandler : MonoBehaviour
    {
        private HubCharacterManager _hubCharacterManager;

        private HubCharacterManager GetHubCharacterManager()
        {
            if (_hubCharacterManager == null)
            {
                _hubCharacterManager = FindFirstObjectByType<HubCharacterManager>();
            }

            return _hubCharacterManager;
        }

        /// <summary>
        /// Entry point called from <see cref="SetCurrentSelection"/> when the player selects a
        /// <see cref="HubSublocationName.Unit"/> POI.
        /// </summary>
        public void HandleCharacterPoiSelection(HubPoiUi poi)
        {
            var manager = GetHubCharacterManager();
            if (manager == null)
            {
                $"SpecificUiHandler: No HubCharacterManager found in scene. Add one to the hub to support character interactions.".LogWarning();
                return;
            }

            var character = poi?.UnitCharacter;
            if (character == null)
            {
                $"SpecificUiHandler: POI '{poi?.name}' has Type=Unit but no UnitCharacter is set on its HubPoiUi.".LogWarning();
                return;
            }

            // Avoid re-activating the same character if already open.
            if (manager.ActiveCharacter == character)
            {
                return;
            }

            _activeHubCharacter = manager;

            var chapterNumber = 0;
            if (hubManager?._brain?.saveFileBrain != null)
            {
                chapterNumber = hubManager._brain.saveFileBrain.ActiveSaveFile.ChapterNumber;
            }

            // Check welcome dialogue before triggering visited so we can set the wait flag first.
            var welcomeOneShot = manager.GetRandomWelcomeOneShot(character, chapterNumber);
            if (!string.IsNullOrWhiteSpace(welcomeOneShot.Dialogue))
            {
                _waitingForShopEntryDialogue = true;
                SubscribeToConversationFinished();
            }

            manager.NotifyCharacterVisited(character, chapterNumber, poi.AvatarPoint);

            if (string.IsNullOrWhiteSpace(welcomeOneShot.Dialogue))
            {
                ShowCharacterInteractions();
            }
        }

        /// <summary>Show the actions menu via the active HubCharacterManager's interaction component.</summary>
        public void ShowCharacterInteractions()
        {
            _activeHubCharacter?.CharacterInteraction?.ShowActionsMenu();
        }

        public void HandleCharacterBack(string action)
        {
            CompleteExit();
        }

        public void HandleCharacterSelection(string action)
        {
            // Forward to HubCharacterInteraction when implemented.
        }

        public void HandleCharacterUpDown(string action)
        {
            // Forward to HubCharacterInteraction when implemented.
        }

        public void HandleCharacterLeftRight(string action)
        {
            // Forward to HubCharacterInteraction when implemented.
        }

        public void HandleCharacterPageChange(string action)
        {
            // Forward to HubCharacterInteraction when implemented.
        }
    }
}
