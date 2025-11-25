using System;
using UnityEngine;

/// <summary>
/// The universal brain for managing and propagating events and data throughout the brain system.
/// All brain events come from here.
/// This is the central pinched part of the farfalle structure of the brain.
/// </summary>
/// <remarks>
/// Subscribe to events in OnEnable() and unsubscribe in OnDestroy() to avoid memory leaks.
/// Other brains should subscribe to events from this brain to maintain the bowtie structure of the brain system.
/// For example, there is a OnPaused event in StateBrain. When the game is paused, StateBrain invokes this event,
/// and other brains that need to respond to the pause event subscribe to it here.
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
