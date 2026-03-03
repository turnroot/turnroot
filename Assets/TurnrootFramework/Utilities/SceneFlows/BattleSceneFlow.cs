using Turnroot.Gameplay.Maps;
using UnityEngine.Events;

namespace Turnroot.Utilities.AbstractScripts
{
    /// <summary>
    /// Represents sub-states within a battle scene for managing player input and conversations.
    /// </summary>
    public enum MiniBattleState
    {
        // A conversation can interrupt at any point
        NoBattlePlayerInput,
        Conversation,
        EnableBattlePlayerInput,
    }

    /// <summary>
    /// Types of interrupts that can pause battle flow.
    /// </summary>
    public enum InterruptType
    {
        None,
        Conversation,
        // Future: Cutscene, EventTrigger, etc.
    }

    public class BattleSceneFlow : DynamicSceneFlow
    {
        // Once this reaches Combat.Battle, activate a mini state machine
        // that goes NoBattlePlayerInput -> EnableBattlePlayerInput -> NoBattlePlayerInput
        // Conversation can interrupt at any point
        public MiniBattleState CurrentMiniBattleState { get; private set; } =
            MiniBattleState.NoBattlePlayerInput;
        private bool _isInTopdownBattleView = true;

        /// <summary>
        /// Tracks whether the camera is currently in top‑down battle mode.
        /// Setting the value will fire <see cref="IsInTopdownBattleViewChanged"/>
        /// if the value actually changes.
        /// </summary>
        public bool IsInTopdownBattleView
        {
            get => _isInTopdownBattleView;
            set
            {
                if (_isInTopdownBattleView == value)
                {
                    return;
                }

                _isInTopdownBattleView = value;
                IsInTopdownBattleViewChanged?.Invoke();
                HandleTopdownBattleViewChangedMapGrid();
            }
        }

        public OperationResult HandleTopdownBattleViewChangedMapGrid()
        {
            MapGrid grid = brain.battleBrain.BattleObject.MapGrid;
            if (grid == null)
            {
                return OperationResult.Failure("No MapGrid found in BattleObject");
            }
            else
            {
                var ObjectsToToggleVisibility = grid.HideOnTopDownLayerModels;
                foreach (var obj in ObjectsToToggleVisibility)
                {
                    if (obj != null)
                    {
                        obj.SetActive(!_isInTopdownBattleView);
                    }
                }
            }
            return OperationResult.Successful();
        }

        public UnityEvent IsInTopdownBattleViewChanged = new();
        private System.Action _onInterruptCompleted;
        private float _lastInterruptActivityTime;
        private bool _interruptIsWaitingForPlayerInput;
        private const float INTERRUPT_INACTIVITY_TIMEOUT = 60f;

        public bool IsInterruptQueued => CurrentInterrupt != InterruptType.None;
        public InterruptType CurrentInterrupt { get; private set; } = InterruptType.None;
        public bool InterruptIsWaitingForPlayerInput
        {
            get => _interruptIsWaitingForPlayerInput;
            set
            {
                _interruptIsWaitingForPlayerInput = value;
                if (value)
                {
                    // When we start waiting for input, that's expected activity - reset timer
                    ResetInterruptActivityTimer();
                }
            }
        }

        /// <summary>
        /// Call this whenever the interrupt system is actively doing something (frame updates, player choices, etc.)
        /// to prevent inactivity timeout from triggering.
        /// </summary>
        public void ResetInterruptActivityTimer() =>
            _lastInterruptActivityTime = UnityEngine.Time.time;

        private void Update()
        {
            // Check for stuck interrupts using inactivity timer
            if (CurrentInterrupt != InterruptType.None)
            {
                float inactivityDuration = UnityEngine.Time.time - _lastInterruptActivityTime;

                // Only timeout if we've been inactive AND we're not waiting for player input
                // If we're waiting for player input, that's expected - the player might be taking their time
                if (
                    inactivityDuration > INTERRUPT_INACTIVITY_TIMEOUT
                    && !_interruptIsWaitingForPlayerInput
                )
                {
                    $"BattleSceneFlow: Interrupt {CurrentInterrupt} inactive for {inactivityDuration:F1}s with no player input expected - forcing completion".LogError();
                    CompleteInterrupt();
                }

                // TODO: Add "are you still there?" system for long player input waits
                // If _interruptIsWaitingForPlayerInput is true for more than X minutes (5-10?),
                // show a non-intrusive prompt asking if player is still present.
                // This prevents AFK players from blocking the system indefinitely.
            }
        }

        /// <summary>
        /// Queue an interrupt to be processed at the next appropriate time.
        /// </summary>
        /// <param name="interruptType">The type of interrupt to queue</param>
        /// <param name="onCompleted">Callback to invoke when interrupt finishes</param>
        public void QueueInterrupt(InterruptType interruptType, System.Action onCompleted = null)
        {
            CurrentInterrupt = interruptType;
            _onInterruptCompleted = onCompleted;
            _lastInterruptActivityTime = UnityEngine.Time.time;
            _interruptIsWaitingForPlayerInput = false;
        }

        public void QueueConversation(System.Action onCompleted = null) =>
            QueueInterrupt(InterruptType.Conversation, onCompleted);

        /// <summary>
        /// Cleanup method to be called when battle ends.
        /// Clears queued interrupts, resets state, and unsubscribes from events.
        /// </summary>
        public void CleanupBattle()
        {
            CurrentInterrupt = InterruptType.None;
            _onInterruptCompleted = null;
            CurrentMiniBattleState = MiniBattleState.NoBattlePlayerInput;
            _lastInterruptActivityTime = 0f;
            _interruptIsWaitingForPlayerInput = false;
        }

        /// <summary>
        /// Called when the current interrupt has completed.
        /// </summary>
        public void CompleteInterrupt()
        {
            _onInterruptCompleted?.Invoke();
            _onInterruptCompleted = null;
            CurrentInterrupt = InterruptType.None;
            _interruptIsWaitingForPlayerInput = false;
        }

        public void InitializeMiniBattleState()
        {
            CurrentMiniBattleState = MiniBattleState.NoBattlePlayerInput;
            DisableBattleInput();
            OnPlayerPreTurn?.Invoke();
        }

        public UnityEvent OnPlayerPreTurn = new();

        public void ProgressMiniBattleState()
        {
            switch (CurrentMiniBattleState)
            {
                case MiniBattleState.NoBattlePlayerInput:
                    HandleNoBattlePlayerInputState();
                    break;

                case MiniBattleState.Conversation:
                    HandleConversationState();
                    break;

                case MiniBattleState.EnableBattlePlayerInput:
                    HandleEnableBattlePlayerInputState();
                    break;

                default:
                    $"BattleSceneFlow: Unknown state {CurrentMiniBattleState}".LogWarning();
                    break;
            }
        }

        private void HandleNoBattlePlayerInputState()
        {
            if (IsInterruptQueued)
            {
                ProcessQueuedInterrupt();
            }
            else
            {
                CurrentMiniBattleState = MiniBattleState.EnableBattlePlayerInput;
                EnableBattleInput();
            }
        }

        private void HandleConversationState()
        {
            CurrentMiniBattleState = MiniBattleState.EnableBattlePlayerInput;
            EnableBattleInput();
        }

        private void HandleEnableBattlePlayerInputState()
        {
            CurrentMiniBattleState = MiniBattleState.NoBattlePlayerInput;
            // If TurnRotisserie indicates that this is the start of the player turn, call that event:
            // for now, just call it
            if (brain)
            {
                OnPlayerPreTurn?.Invoke();
            }
            DisableBattleInput();
        }

        public void EnableBattleInput() => brain.PublishBattleInputEnabled();

        public void DisableBattleInput() => brain.PublishBattleInputDisabled();

        private void ProcessQueuedInterrupt()
        {
            IsInTopdownBattleView = false;
            switch (CurrentInterrupt)
            {
                case InterruptType.Conversation:
                    CurrentMiniBattleState = MiniBattleState.Conversation;
                    DisableBattleInput();

                    // Start the conversation via the conversation controller
                    var conversationController =
                        FindFirstObjectByType<Conversations.ConversationController>();
                    if (conversationController != null)
                    {
                        conversationController.StartCurrentConversation();
                        // Conversation is actively running, reset activity timer
                        ResetInterruptActivityTimer();
                    }
                    else
                    {
                        "BattleSceneFlow: Conversation queued but no ConversationController found".LogWarning();
                        // No conversation controller, immediately complete
                        CompleteInterrupt();
                    }
                    break;

                // TODO: Handle other interrupt types:
                // case InterruptType.Cutscene:
                //     Play cutscene, call ResetInterruptActivityTimer() every frame while cutscene is playing
                //     Call CompleteInterrupt() when cutscene ends
                // case InterruptType.EventTrigger:
                //     Execute event, call ResetInterruptActivityTimer() during execution
                //     Call CompleteInterrupt() when event finishes

                case InterruptType.None:
                    // No interrupt, just continue
                    break;

                default:
                    // Unknown interrupt type, log warning and clear
                    $"BattleSceneFlow: Unknown interrupt type {CurrentInterrupt}".LogWarning();
                    CompleteInterrupt();
                    break;
            }
        }

        public void HandlePreBattleTransitionToBattleCompleted() =>
            brain?.stateBrain.HandlePreBattleTransitionToBattleCompleted();

        protected override void SubscribeToBrainEvents()
        {
            brain.OnStateChanged += HandleStateChanged;
            brain.OnPrecomputeCompleted += HandlePrecomputeCompleted;
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            brain.OnStateChanged -= HandleStateChanged;
            brain.OnPrecomputeCompleted -= HandlePrecomputeCompleted;
        }

        protected void HandlePrecomputeCompleted()
        {
            HandlePreBattleTransitionToBattleCompleted();
            HandleTopdownBattleViewChangedMapGrid();
        }

        protected override void OnSegmentReached(int segmentIndex)
        {
            if (segmentIndex < segments.Count)
            {
                CurrentSegment?.onSegmentReached?.Invoke();

                if (
                    CurrentSegment?.stateId != null
                    && CurrentSegment.stateId.Contains(BrainStateNames.PreBattleTransitionToBattle)
                )
                {
                    StartPreLoading?.Invoke();
                    loadingController?.Initialize();
                }
            }
        }
    }
}
