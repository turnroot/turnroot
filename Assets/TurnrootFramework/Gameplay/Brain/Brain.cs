using System;
using UnityEngine;

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
namespace Assets.Turnroot.Gameplay.Brain
{
    /* --------------------------- Required components -------------------------- */
    [RequireComponent(typeof(StateBrain))]
    [RequireComponent(typeof(ConversationalBrain))]
    [RequireComponent(typeof(LongTermMemory))]
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

        /* ------------------- Wake up and turn on LongTermMemory ------------------- */
        public void Awake()
        {
            Debug.Log("EventsBrain Awake called.");
            InitializeLongTermMemory();
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
