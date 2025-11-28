using System;
using Turnroot.Characters.Components.Support;
using Turnroot.Conversations;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
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
    public class Brain : MonoBehaviour
    {
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

        /// <summary>
        /// Centralized publisher for LTM key cache version changes.
        /// Other systems should call this to notify subscribed brains/components.
        /// </summary>
        public void PublishLtmKeyCacheUpdated(int version)
        {
            OnLtmKeyCacheUpdated?.Invoke(version);
        }

        public void NotifyIllegalModification(string message)
        {
            OnIllegallyModifiedFileDetected?.Invoke(message);
        }

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

        public void PublishRosterReady(Characters.RosterInstance instance)
        {
            OnRosterReady?.Invoke(instance);
        }

        public void PublishRosterFailed(Characters.Roster roster, string reason)
        {
            OnRosterFailed?.Invoke(roster, reason);
        }

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
        public event Action<BattleContext> OnBattleContextInitialized;

        public void PublishBattleContextInitialized(BattleContext context)
        {
            OnBattleContextInitialized?.Invoke(context);
        }

        #endregion

        #region Initialization

        public void Awake()
        {
            Debug.Log("EventsBrain Awake called.");

            InitializeLongTermMemory();
            CheckScriptingSymbols();
            InitializeModules();
            TryLinkConversationController();

            SceneManager.sceneLoaded += OnSceneLoaded_LinkControllers;
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
                modules.Add("Hub");
            if (bloodlinesModuleEnabled)
                modules.Add("Bloodlines");
            if (unwindModuleEnabled)
                modules.Add("Unwind");
            if (troopsModuleEnabled)
                modules.Add("Troops");
            if (monstersModuleEnabled)
                modules.Add("Monsters");
            if (retroModuleEnabled)
                modules.Add("Retro");

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

        private void OnSceneLoaded_LinkControllers(Scene scene, LoadSceneMode mode)
        {
            TryLinkConversationController();
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

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded_LinkControllers;
        }

        #endregion
    }
}
