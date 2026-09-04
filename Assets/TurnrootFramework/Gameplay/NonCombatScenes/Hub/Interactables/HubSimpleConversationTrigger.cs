using Turnroot.Conversations;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    [RequireComponent(typeof(Collider))]
    public class HubSimpleConversationTrigger : HubFadableVisualBase, IHubSelectable
    {
        public Conversation conversationPlayOnInteraction;

        public bool CanSelect => enabled && conversationPlayOnInteraction != null;

        private HubManager _hubManager;
        private bool _isConversationActive;

        private void Awake()
        {
            _hubManager = HubManager.GetCurrent();

            if (poiVisual == null)
            {
                $"HubSimpleConversationTrigger on {gameObject.name} has no poiVisual assigned, disabling.".LogWarning();
                enabled = false;
                return;
            }

            InitializeVisualMaterials();
            Hide();
        }

        public void Select()
        {
            if (_isConversationActive || conversationPlayOnInteraction == null)
            {
                return;
            }

            var controller = FindFirstObjectByType<ConversationController>();
            if (controller == null)
            {
                $"HubSimpleConversationTrigger on {gameObject.name}: No ConversationController found in scene.".LogWarning();
                return;
            }

            _isConversationActive = true;
            PlayPoiSelectSound();
            _hubManager?.SetInputMode(HubManager.HubInputMode.Chosen);
            Hide();

            controller.PlayConversationDirect(
                conversationPlayOnInteraction,
                OnConversationFinished
            );
        }

        private void OnConversationFinished()
        {
            _isConversationActive = false;
            _hubManager?.RevertToPreviousInputMode();
            Show();
        }

        private void Update() => FaceCamera();
    }
}
