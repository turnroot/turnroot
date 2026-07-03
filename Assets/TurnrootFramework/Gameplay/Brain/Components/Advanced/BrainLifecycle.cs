using System.Linq;
using Turnroot.Conversations;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Gameplay.Brain.Segments;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using Turnroot.Utilities.SceneFlows;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class Brain : MonoBehaviour
    {
        private bool _awake = false;

        private void MakeInitialConnections()
        {
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
            saveFileBrain = GetComponent<SaveFileBrain>();
            sceneFlowBrain = GetComponent<SceneFlowBrain>();

            ValidationHelper.ValidateNotNull(stateBrain, "stateBrain");
            ValidationHelper.ValidateNotNull(conversationalBrain, "conversationalBrain");
            ValidationHelper.ValidateNotNull(gamewideContextBrain, "gamewideContextBrain");
            ValidationHelper.ValidateNotNull(battleBrain, "battleBrain");
            ValidationHelper.ValidateNotNull(charactersBrain, "charactersBrain");
            ValidationHelper.ValidateNotNull(inventoryBrain, "inventoryBrain");
            ValidationHelper.ValidateNotNull(storehouseBrain, "storehouseBrain");
            ValidationHelper.ValidateNotNull(
                battleInputControllerBrain,
                "battleInputControllerBrain"
            );
            ValidationHelper.ValidateNotNull(uiBrain, "uiBrain");
            ValidationHelper.ValidateNotNull(volumeBrain, "volumeBrain");
            ValidationHelper.ValidateNotNull(audioBrain, "audioBrain");
            ValidationHelper.ValidateNotNull(cameraBrain, "cameraBrain");
            ValidationHelper.ValidateNotNull(cursorBrain, "cursorBrain");
            ValidationHelper.ValidateNotNull(
                positioningInputControllerBrain,
                "positioningInputControllerBrain"
            );
            ValidationHelper.ValidateNotNull(unitAppearanceBrain, "unitAppearanceBrain");
            ValidationHelper.ValidateNotNull(saveFileBrain, "saveFileBrain");
            ValidationHelper.ValidateNotNull(sceneFlowBrain, "sceneFlowBrain");

            // Find all DynamicSceneFlows in other scenes and set their .brain to this
            var allSceneFlows = FindObjectsByType<DynamicSceneFlow>(FindObjectsSortMode.None);
            foreach (var sceneFlow in allSceneFlows)
            {
                if (sceneFlow.gameObject.scene != gameObject.scene)
                {
                    sceneFlow.brain = this;
                }
            }
            _awake = true;

            PublishBrainReady(this);
        }

        public void Awake()
        {
            if (!_awake)
            {
                InitializeLongTermMemory();
                InitializeModules();
                InitializeAdvancedSystems();
                TryLinkConversationController();
                MakeInitialConnections();
            }
        }

        public OperationResult InitializeLongTermMemory()
        {
            ltm =
                gameObject.GetComponent<LongTermMemory>()
                ?? gameObject.AddComponent<LongTermMemory>();

            return ltm == null
                ? OperationResult.Failure("Failed to initialize LongTermMemory.")
                : OperationResult.Successful();
        }

        public OperationResult InitializeModules()
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

            $"Turnroot modules: {(string.IsNullOrEmpty(enabled) ? "None" : enabled)}".LogInfo();
            return OperationResult.Successful();
        }

        #region Conversation Controller Management

        private readonly SingleValueCache<ConversationController> _conversationControllerCache =
            new();

        public void PopulateSceneConversationController(ConversationController controller)
        {
            _sceneConversationController = controller;
            _conversationControllerCache.Invalidate();
        }

        private OperationResult TryLinkConversationController()
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
                return OperationResult.Successful();
            }
            return OperationResult.Failure("No ConversationController found in scene.");
        }

        #endregion

        #region State Control

        public void Pause()
        {
            var stateBrain = GetComponent<StateBrain>();
            stateBrain.Pause();
        }

        public void Resume()
        {
            var stateBrain = GetComponent<StateBrain>();
            stateBrain.Resume();
        }

        #endregion

        #region Cleanup

        private void OnDestroy() => CleanupAdvancedSystems();

        #endregion
    }
}
