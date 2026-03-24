using System;
using Turnroot.Characters;
using Turnroot.Conversations;
using Turnroot.Gameplay.Brain;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Abstract
{
    public abstract class HubVendor : MonoBehaviour
    {
        public CharacterData Shopkeeper;

        public OneShotDialogue[] WelcomeDialogues;
        public OneShotDialogue[] FarewellDialogues;

        private OneShotDialogue[] cachedWelcomeDialogues;

        protected OneShot[] WelcomeDialogueConversations;
        protected OneShot[] FarewellDialogueConversations;

        [HideInInspector]
        public Brain.Brain brain;

        protected AudioBrain audioBrain;

        protected virtual void Awake()
        {
            cachedWelcomeDialogues = WelcomeDialogues;

            brain ??= FindFirstObjectByType<Brain.Brain>();
            audioBrain ??= brain?.audioBrain;

            var speakerName = Shopkeeper != null ? Shopkeeper.DisplayName : "???";

            WelcomeDialogueConversations =
                audioBrain?.ConvertToOneShots(WelcomeDialogues, speakerName)
                ?? Array.Empty<OneShot>();
            FarewellDialogueConversations =
                audioBrain?.ConvertToOneShots(FarewellDialogues, speakerName)
                ?? Array.Empty<OneShot>();
        }

        protected virtual void OnDestroy()
        {
            WelcomeDialogues = cachedWelcomeDialogues;
        }

        protected void NotifyVisited(Action refreshUi, string componentName)
        {
            brain ??= FindFirstObjectByType<Brain.Brain>();
            audioBrain ??= brain?.audioBrain;

            if (refreshUi != null)
            {
                try
                {
                    refreshUi();
                }
                catch (Exception ex)
                {
                    $"{componentName} '{name}': Refresh UI threw: {ex}".LogWarning();
                }
            }

            var welcomeOneShot =
                audioBrain != null
                    ? audioBrain.GetRandomWelcomeOneShot(WelcomeDialogueConversations)
                    : default;
            if (!string.IsNullOrWhiteSpace(welcomeOneShot.Dialogue))
            {
                var player = audioBrain?.GetOrCreateOneShotPlayer();
                if (player == null)
                {
                    $"{componentName} '{name}': Could not create OneShotPlayer for dialogue playback.".LogWarning();
                    return;
                }

                player.PlayOneShot(welcomeOneShot);
            }
            else
            {
                $"{componentName} '{name}': No welcome dialogue to play".LogInfo();
            }
        }

        protected void NotifyExited(Action hideUi, string componentName)
        {
            brain ??= FindFirstObjectByType<Brain.Brain>();
            audioBrain ??= brain?.audioBrain;

            var farewellOneShot =
                audioBrain != null
                    ? audioBrain.GetRandomOneShot(FarewellDialogueConversations)
                    : default;

            if (!string.IsNullOrWhiteSpace(farewellOneShot.Dialogue))
            {
                var player = audioBrain?.GetOrCreateOneShotPlayer();
                if (player == null)
                {
                    $"{componentName} '{name}': player is null, hiding UI immediately.".LogWarning();
                    hideUi?.Invoke();
                    return;
                }

                player.PlayOneShot(farewellOneShot);
            }
            else
            {
                $"{componentName} '{name}': No farewell dialogue to play".LogInfo();
            }
        }

        protected void NotifyTransaction<T>(
            T itemOrItems,
            Action<T> publishAction,
            OneShot[] dialogueConversations,
            string onEmptyLogFormat
        )
        {
            brain ??= FindFirstObjectByType<Brain.Brain>();
            publishAction?.Invoke(itemOrItems);

            var oneShot =
                audioBrain != null ? audioBrain.GetRandomOneShot(dialogueConversations) : default;
            if (!string.IsNullOrWhiteSpace(oneShot.Dialogue))
            {
                audioBrain?.GetOrCreateOneShotPlayer()?.PlayOneShot(oneShot);
            }
            else
            {
                $"{name}: {onEmptyLogFormat}".LogInfo();
            }
        }
    }
}
