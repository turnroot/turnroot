using System.Linq;
using Turnroot.Conversations;
using Turnroot.Utilities;
using TurnrootFramework.Gameplay.Brain.Segments;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class Brain : MonoBehaviour
    {
        public void Awake()
        {
            InitializeLongTermMemory();
            InitializeModules();
            InitializeAdvancedSystems();
            TryLinkConversationController();

            // populate remaining core components
            stateBrain = GetComponent<StateBrain>();
            conversationalBrain = GetComponent<ConversationalBrain>();
            gamewideContextBrain = GetComponent<GamewideContextBrain>();
            battleBrain = GetComponent<BattleBrain>();
            charactersBrain = GetComponent<CharactersBrain>();
            inventoryBrain = GetComponent<InventoryBrain>();
            storehouseBrain = GetComponent<StorehouseBrain>();
            battleInputControllerBrain = GetComponent<BattleInputControllerBrain>();
            uiBrain = GetComponent<UiBrain>();
            volumeBrain = GetComponent<VolumeBrain>();
            audioBrain = GetComponent<AudioBrain>();
            cameraBrain = GetComponent<CameraBrain>();
            cursorBrain = GetComponent<CursorBrain>();
            positioningInputControllerBrain = GetComponent<PositioningInputController>();
            unitAppearanceBrain = GetComponent<UnitAppearanceBrain>();

            // Find all DynamicSceneFlows in other scenes and set their .brain to this
            var allSceneFlows = FindObjectsByType<DynamicSceneFlow>(FindObjectsSortMode.None);
            foreach (var sceneFlow in allSceneFlows)
            {
                if (sceneFlow.gameObject.scene != gameObject.scene)
                {
                    sceneFlow.brain = this;
                }
            }
        }

        public void InitializeLongTermMemory()
        {
            ltm =
                gameObject.GetComponent<LongTermMemory>()
                ?? gameObject.AddComponent<LongTermMemory>();

            if (ltm == null)
            {
#if UNITY_EDITOR
                Debug.LogError("Brain failed to initialize LongTermMemory.");
#endif
                Debug.Break();
            }
            else
            {
#if UNITY_EDITOR
                Debug.Log("Brain initialized LongTermMemory.");
#endif
            }
        }

        public void InitializeModules()
        {
            var modules = new[]
            {
                (HubModuleEnabled, "Hub"),
                (BloodlinesModuleEnabled, "Bloodlines"),
                (UnwindModuleEnabled, "Unwind"),
                (TroopsModuleEnabled, "Troops"),
                (MonstersModuleEnabled, "Monsters"),
                (RetroModuleEnabled, "Retro"),
            };
            var enabled = string.Join(", ", modules.Where(m => m.Item1).Select(m => m.Item2));
#if UNITY_EDITOR
            Debug.Log($"Turnroot modules: {(string.IsNullOrEmpty(enabled) ? "None" : enabled)}");
#endif
        }

        #region Conversation Controller Management

        private readonly SingleValueCache<ConversationController> _conversationControllerCache =
            new();

        public void PopulateSceneConversationController(ConversationController controller)
        {
            _sceneConversationController = controller;
            _conversationControllerCache.Invalidate(); // Invalidate cache when manually set
#if UNITY_EDITOR
            Debug.Log("Brain populated scene ConversationController.");
#endif
        }

        private void TryLinkConversationController()
        {
            var controller = _conversationControllerCache.GetOrCompute(() =>
            {
                var controllers = FindObjectsByType<ConversationController>(
                    FindObjectsSortMode.None
                );
                return (controllers != null && controllers.Length > 0) ? controllers[0] : null;
            });

            if (controller != null)
            {
                _sceneConversationController = controller;
            }
        }

        #endregion

        #region State Control

        public void Pause()
        {
            var stateBrain = GetComponent<StateBrain>();
            stateBrain?.Pause();
        }

        public void Resume()
        {
            var stateBrain = GetComponent<StateBrain>();
            stateBrain?.Resume();
        }

        #endregion

        #region Cleanup

        private void OnDestroy() => CleanupAdvancedSystems();

        #endregion
    }
}
