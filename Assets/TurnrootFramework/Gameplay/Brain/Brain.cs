using System;
using Turnroot.Characters.Components.Support;
using Turnroot.Conversations;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Turnroot.Gameplay.Brain
{
    /* --------------------------- Required components -------------------------- */
    [RequireComponent(typeof(StateBrain))]
    [RequireComponent(typeof(ConversationalBrain))]
    [RequireComponent(typeof(LongTermMemory))]
    /// <summary>
    /// The universal brain for managing and propagating events and data throughout the brain system.
    /// All brain events come from here.
    /// This is the central pinched part of the farfalle structure of the brain.
    /// </summary>
    /// <remarks>
    /// If you want to extend brain functionality, create new scripts that interact with the brain components.
    /// Hook into the brain, don't alter it.
    /// If you're looking for hooks, there are lots of events you can subscribe to,
    /// search for `public event Action`.
    ///
    /// Note: if you are going to make changes here, make sure you fork the Turnroot Framework repository
    /// on GitHub and make your changes there. If you go in willy-nily without a git connection,
    /// I can't help you if you mess up your game!
    /// </remarks>
    public class Brain : MonoBehaviour
    {
        public LongTermMemory ltm;

        /* ------------------------------ Module flags ------------------------------ */
        // These are paid add-on modules, you can find them on the asset store.
        // The modules will self-install and self-enable these.
        // Thanks for supporting Turnroot :)
        private bool campModuleEnabled = false;
        private bool hubModuleEnabled = false;
        private bool bloodlinesModuleEnabled = false;
        private bool retroModuleEnabled = false;

        /* ------------------------------ State events ------------------------------ */
        public event Action<BrainState> OnPaused;
        public event Action<BrainState> OnResumed;
        public event Action<BrainState> OnStateChanged;
        public event Action OnGameOver;
        public event Action HighLevelStatesInitialized;

        /* -------------------------- Conversation events --------------------------- */
        public event Action<SupportRelationshipInstance> OnSupportPointsChanged;
        public event Action<SupportRelationshipInstance> OnSupportConversationAvailable;
        public event Action<SupportRelationshipInstance> SLevelSupportConversationAvailable;
        public event Action<Conversation> OnConversationStarted;
        public event Action<Conversation> OnConversationEnded;
        public event Action<ConversationLayer> OnConversationLayerStarted;
        public event Action<ConversationLayer> OnConversationLayerEnded;

        /* ------------------------ Scene-level dependencies ------------------------ */
        private ConversationController _sceneConversationController;

        /* --------------------------------- Wake up -------------------------------- */
        public void Awake()
        {
            Debug.Log("EventsBrain Awake called.");
            InitializeLongTermMemory();
            CheckScriptingSymbols();
            InitializeModules();

            try
            {
                var controllers = FindObjectsByType<ConversationController>(
                    FindObjectsSortMode.None
                );
                if (controllers != null && controllers.Length > 0)
                {
                    PopulateSceneConversationController(controllers[0]);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Brain failed to auto-link ConversationController: " + ex.Message);
            }

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

        /* ------------------------------ Check modules ----------------------------- */
        public void CheckScriptingSymbols()
        {
#if TURNROOT_BLOODLINES_MODULE
            bloodlinesModuleEnabled = true;
#endif
#if TURNROOT_CAMP_MODULE
            campModuleEnabled = true;
#endif
#if TURNROOT_HUB_MODULE
            hubModuleEnabled = true;
#endif
#if TURNROOT_RETRO_MODULE
            retroModuleEnabled = true;
#endif
        }

        /* ------------------------- Initialize any modules ------------------------- */
        public void InitializeModules()
        {
            Debug.Log(
                "Turnroot add-on modules you have access to: "
                    + (campModuleEnabled ? "Camp, " : "")
                    + (hubModuleEnabled ? "Hub, " : "")
                    + (bloodlinesModuleEnabled ? "Bloodlines, " : "")
                    + (retroModuleEnabled ? "Retro " : "")
            );
            Debug.Log(
                "You can find more info about Turnroot add-on modules on the Unity Asset Store."
            );
            Debug.Log("All available Turnroot modules initialized.");
            // TODO: Add module brain segments
        }

        /* ---------------------------- Populator methods --------------------------- */
        public void PopulateSceneConversationController(ConversationController controller)
        {
            _sceneConversationController = controller;
            Debug.Log("Brain populated scene ConversationController.");
        }

        private void OnSceneLoaded_LinkControllers(Scene scene, LoadSceneMode mode)
        {
            try
            {
                var controllers = FindObjectsByType<ConversationController>(
                    FindObjectsSortMode.None
                );
                if (controllers != null && controllers.Length > 0)
                {
                    PopulateSceneConversationController(controllers[0]);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    "Brain failed to auto-link ConversationController on scene load: " + ex.Message
                );
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded_LinkControllers;
        }

        /* ------------------------------ State methods ----------------------------- */
        public void PublishPaused(BrainState prev)
        {
            Debug.Log("EventsBrain: State paused -> " + (prev?.Name ?? "(null)"));
            OnPaused?.Invoke(prev);
        }

        public void PublishResumed(BrainState prev)
        {
            Debug.Log("EventsBrain: State resumed -> " + (prev?.Name ?? "(null)"));
            OnResumed?.Invoke(prev);
        }

        public void PublishStateChanged(BrainState newState)
        {
            Debug.Log("EventsBrain: State changed -> " + (newState?.Name ?? "(null)"));
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
    }
}
