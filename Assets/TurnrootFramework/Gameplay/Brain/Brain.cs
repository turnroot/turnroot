using System;
using Assets.Turnroot.Gameplay.Combat;
using Turnroot.Characters;
using Turnroot.Characters.Components.Support;
using Turnroot.Conversations;
using Turnroot.Gameplay.Objects;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Turnroot.Gameplay.Brain
{
    /// <summary>
    /// The universal brain for managing and propagating events and data throughout the brain system.
    /// All brain events come from here.
    /// It's like a farfalle, lots of stuff in, one central point, lots of stuff out.
    /// </summary>
    /// <remarks>
    /// If you want to extend brain functionality, create new scripts that interact with the brain components.
    /// Hook into the brain, don't alter it.
    /// If you're looking for hooks, there are lots of events you can subscribe to,
    /// search for `public event Action`.
    ///
    /// Note: if you are going to make changes here, make sure you fork the Turnroot Framework repository
    /// on GitHub and make your changes there. If you go in willy-nily without git history,
    /// I can't help you if you mess up your game!
    /// </remarks>
    [RequireComponent(typeof(StateBrain))]
    [RequireComponent(typeof(ConversationalBrain))]
    [RequireComponent(typeof(LongTermMemory))]
    [RequireComponent(typeof(GamewideContextBrain))]
    [RequireComponent(typeof(CharactersBrain))]
    [RequireComponent(typeof(BattleBrain))]
    [RequireComponent(typeof(InventoryBrain))]
    [RequireComponent(typeof(StorehouseBrain))]
    public class Brain : MonoBehaviour
    {
        // Core components
        [HideInInspector]
        public StateBrain stateBrain;

        [HideInInspector]
        public ConversationalBrain conversationalBrain;

        [HideInInspector]
        public GamewideContextBrain gamewideContextBrain;

        [HideInInspector]
        public CharactersBrain charactersBrain;

        [HideInInspector]
        public BattleBrain battleBrain;

        [HideInInspector]
        public InventoryBrain inventoryBrain;

        [HideInInspector]
        public StorehouseBrain storehouseBrain;

        [HideInInspector]
        public LongTermMemory ltm;

        // Scene-level dependencies
        private ConversationController _sceneConversationController;

        // Module flags - paid add-on modules that self-install
        private bool hubModuleEnabled = false;
        private bool bloodlinesModuleEnabled = false;
        private bool retroModuleEnabled = false;
        private bool unwindModuleEnabled = false;
        private bool troopsModuleEnabled = false;
        private bool monstersModuleEnabled = false;

        #region Memory Events

        public event Action<string> OnIllegallyModifiedFileDetected;
        public event Action<int> OnLtmKeyCacheUpdated;

        public void PublishLtmKeyCacheUpdated(int version) => OnLtmKeyCacheUpdated?.Invoke(version);

        public void NotifyIllegalModification(string message) =>
            OnIllegallyModifiedFileDetected?.Invoke(message);

        #endregion

        #region State Events

        public event Action<BrainState> OnPaused;
        public event Action<BrainState> OnResumed;
        public event Action<BrainState> OnStateChanged;
        public event Action OnGameOver;
        public event Action HighLevelStatesInitialized;

        public void PublishPaused(BrainState prev)
        {
            Debug.Log($"EventsBrain: State paused -> {prev?.Name ?? "(null)"}");
            OnPaused?.Invoke(prev);
        }

        public void PublishResumed(BrainState prev)
        {
            Debug.Log($"EventsBrain: State resumed -> {prev?.Name ?? "(null)"}");
            OnResumed?.Invoke(prev);
        }

        public void PublishStateChanged(BrainState newState)
        {
            Debug.Log($"EventsBrain: State changed -> {newState?.Name ?? "(null)"}");
            OnStateChanged?.Invoke(newState);
        }

        public void PublishGameOver()
        {
            Debug.Log("EventsBrain: GameOver event received");
            OnGameOver?.Invoke();
        }

        public void PublishHighLevelStatesInitialized()
        {
            Debug.Log("EventsBrain: High-level states initialized");
            HighLevelStatesInitialized?.Invoke();
        }

        #endregion

        #region Roster Lifecycle Events

        public event Action<Characters.RosterInstance> OnRosterReady;
        public event Action<Characters.Roster, string> OnRosterFailed;

        public void PublishRosterReady(Characters.RosterInstance instance) =>
            OnRosterReady?.Invoke(instance);

        public void PublishRosterFailed(Characters.Roster roster, string reason) =>
            OnRosterFailed?.Invoke(roster, reason);

        #endregion

        #region Character Progression Events

        public event Action<CharacterInstance> OnCharacterLevelUp;
        public event Action<CharacterInstance> OnCharacterKill;
        public event Action<CharacterInstance, Skill> OnCharacterLearnedSkill;
        public event Action<CharacterInstance, Skill> OnCharacterRemovedSkill;
        public event Action<CharacterInstance> OnCharacterClassChanged;
        public event Action<CharacterInstance, string, int> OnExperienceGained;
        public event Action<CharacterInstance, CharacterData, int> OnSupportIncreased;

        public void PublishCharacterLevelUp(CharacterInstance character) =>
            OnCharacterLevelUp?.Invoke(character);

        public void PublishCharacterKill(CharacterInstance character) =>
            OnCharacterKill?.Invoke(character);

        public void PublishCharacterLearnedSkill(CharacterInstance character, Skill skill) =>
            OnCharacterLearnedSkill?.Invoke(character, skill);

        public void PublishCharacterRemovedSkill(CharacterInstance character, Skill skill) =>
            OnCharacterRemovedSkill?.Invoke(character, skill);

        public void PublishCharacterClassChanged(CharacterInstance character) =>
            OnCharacterClassChanged?.Invoke(character);

        public void PublishExperienceGained(
            CharacterInstance character,
            string experienceTypeId,
            int amount
        ) => OnExperienceGained?.Invoke(character, experienceTypeId, amount);

        public void PublishSupportIncreased(
            CharacterInstance character,
            CharacterData targetCharacter,
            int amount
        ) => OnSupportIncreased?.Invoke(character, targetCharacter, amount);

        #endregion

        #region Item Events

        public event Action<ObjectItemInstance, int> OnItemUsed;
        public event Action<ObjectItemInstance> OnItemBroken;
        public event Action<ObjectItemInstance, CharacterInventoryInstance> OnItemTransferred;
        public event Action<ObjectItemInstance> OnItemDiscarded;
        public event Action<ObjectItemInstance> OnItemSold;
        public event Action<ObjectItemInstance, CharacterInventoryInstance> OnItemBought;
        public event Action<ObjectItemInstance, int> OnItemRepaired;
        public event Action<ObjectItemInstance, ObjectItem> OnItemForged;
        public event Action<ObjectItemInstance> OnItemDeposited;
        public event Action<ObjectItemInstance, CharacterInventoryInstance> OnItemWithdrawn;

        public void PublishItemUsed(ObjectItemInstance item, int remainingUses) =>
            OnItemUsed?.Invoke(item, remainingUses);

        public void PublishItemBroken(ObjectItemInstance item) => OnItemBroken?.Invoke(item);

        public void PublishItemTransferred(
            ObjectItemInstance item,
            CharacterInventoryInstance targetInventory
        ) => OnItemTransferred?.Invoke(item, targetInventory);

        public void PublishItemDiscarded(ObjectItemInstance item) => OnItemDiscarded?.Invoke(item);

        public void PublishItemSold(ObjectItemInstance item) => OnItemSold?.Invoke(item);

        public void PublishItemBought(
            ObjectItemInstance item,
            CharacterInventoryInstance buyerInventory
        ) => OnItemBought?.Invoke(item, buyerInventory);

        public void PublishItemRepaired(ObjectItemInstance item, int repairUses) =>
            OnItemRepaired?.Invoke(item, repairUses);

        public void PublishItemForged(ObjectItemInstance item, ObjectItem targetItem) =>
            OnItemForged?.Invoke(item, targetItem);

        public void PublishItemDeposited(ObjectItemInstance item) => OnItemDeposited?.Invoke(item);

        public void PublishItemWithdrawn(
            ObjectItemInstance item,
            CharacterInventoryInstance targetInventory
        ) => OnItemWithdrawn?.Invoke(item, targetInventory);

        #endregion

        #region Conversation Events

        public event Action<SupportRelationshipInstance> OnSupportPointsChanged;
        public event Action<SupportRelationshipInstance> OnSupportConversationAvailable;
        public event Action<SupportRelationshipInstance> SLevelSupportConversationAvailable;
        public event Action<Conversation> OnConversationStarted;
        public event Action<Conversation> OnConversationEnded;
        public event Action<ConversationLayer> OnConversationLayerStarted;
        public event Action<ConversationLayer> OnConversationLayerEnded;

        #endregion

        #region Battle Events
        public event Action OnStartBattle;
        public event Action<BattleExitType> OnExitBattle;
        public event Action OnBattleContextInitialized;
        public event Action OnPreBattleStarted;
        public event Action OnPreBattleEnded;
        public event Action OnTurnBegin;
        public event Action OnPlayerTurnStarted;
        public event Action OnEnemyTurnStarted;
        public event Action OnThirdPartyTurnStarted;
        public event Action OnPlayerTurnEnded;
        public event Action OnEnemyTurnEnded;
        public event Action OnThirdPartyTurnEnded;
        public event Action OnTurnEnded;

        public void InvokeStartBattle() => OnStartBattle?.Invoke();

        public void InvokePreBattleStarted() => OnPreBattleStarted?.Invoke();

        public void InvokePreBattleEnded() => OnPreBattleEnded?.Invoke();

        public void InvokeExitBattle(BattleExitType exitType) => OnExitBattle?.Invoke(exitType);

        public void InvokeBattleContextInitialized() => OnBattleContextInitialized?.Invoke();

        public void InvokeTurnBegin() => OnTurnBegin?.Invoke();

        public void InvokePlayerTurnStarted() => OnPlayerTurnStarted?.Invoke();

        public void InvokeEnemyTurnStarted() => OnEnemyTurnStarted?.Invoke();

        public void InvokeThirdPartyTurnStarted() => OnThirdPartyTurnStarted?.Invoke();

        public void InvokePlayerTurnEnded() => OnPlayerTurnEnded?.Invoke();

        public void InvokeEnemyTurnEnded() => OnEnemyTurnEnded?.Invoke();

        public void InvokeThirdPartyTurnEnded() => OnThirdPartyTurnEnded?.Invoke();

        public void InvokeTurnEnded() => OnTurnEnded?.Invoke();

        #endregion

        #region Initialization

        public void Awake()
        {
            Debug.Log("Brain Awake!");

            InitializeLongTermMemory();
            CheckScriptingSymbols();
            InitializeModules();
            TryLinkConversationController();

            SceneManager.sceneLoaded += OnSceneLoaded_LinkControllers;

            // populate remaining core components
            stateBrain = GetComponent<StateBrain>();
            conversationalBrain = GetComponent<ConversationalBrain>();
            gamewideContextBrain = GetComponent<GamewideContextBrain>();
            battleBrain = GetComponent<BattleBrain>();
            charactersBrain = GetComponent<CharactersBrain>();
        }

        public void InitializeLongTermMemory()
        {
            ltm =
                gameObject.GetComponent<LongTermMemory>()
                ?? gameObject.AddComponent<LongTermMemory>();

            if (ltm == null)
            {
                Debug.LogError("Brain failed to initialize LongTermMemory.");
                Debug.Break();
            }
            else
            {
                Debug.Log("Brain initialized LongTermMemory.");
            }
        }

        public void CheckScriptingSymbols()
        {
#if TURNROOT_BLOODLINES_MODULE
            bloodlinesModuleEnabled = true;
#endif
#if TURNROOT_HUB_MODULE
            hubModuleEnabled = true;
#endif
#if TURNROOT_RETRO_MODULE
            retroModuleEnabled = true;
#endif
#if TURNROOT_UNWIND_MODULE
            unwindModuleEnabled = true;
#endif
#if TURNROOT_TROOPS_MODULE
            troopsModuleEnabled = true;
#endif
#if TURNROOT_MONSTERS_MODULE
            monstersModuleEnabled = true;
#endif
        }

        public void InitializeModules()
        {
            var enabledModules = GetEnabledModulesString();

            Debug.Log($"Turnroot add-on modules you have access to: {enabledModules}");
            Debug.Log(
                "You can find more info about Turnroot add-on modules on the Unity Asset Store."
            );
            Debug.Log("All available Turnroot modules initialized.");
        }

        private string GetEnabledModulesString()
        {
            var modules = new System.Collections.Generic.List<string>();

            if (hubModuleEnabled)
            {
                modules.Add("Hub");
            }

            if (bloodlinesModuleEnabled)
            {
                modules.Add("Bloodlines");
            }

            if (unwindModuleEnabled)
            {
                modules.Add("Unwind");
            }

            if (troopsModuleEnabled)
            {
                modules.Add("Troops");
            }

            if (monstersModuleEnabled)
            {
                modules.Add("Monsters");
            }

            if (retroModuleEnabled)
            {
                modules.Add("Retro");
            }

            return modules.Count > 0 ? string.Join(", ", modules) : "None";
        }

        #endregion

        #region Conversation Controller Management

        public void PopulateSceneConversationController(ConversationController controller)
        {
            _sceneConversationController = controller;
            Debug.Log("Brain populated scene ConversationController.");
        }

        private void TryLinkConversationController()
        {
            var controllers = FindObjectsByType<ConversationController>(FindObjectsSortMode.None);

            if (controllers != null && controllers.Length > 0)
            {
                PopulateSceneConversationController(controllers[0]);
            }
        }

        private void OnSceneLoaded_LinkControllers(Scene scene, LoadSceneMode mode) =>
            TryLinkConversationController();

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

        private void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded_LinkControllers;

        #endregion
    }
}
