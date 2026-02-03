using UnityEngine.Events;

namespace Turnroot.Utilities.AbstractScripts
{
    public enum MiniBattleState
    {
        // A conversation can interrupt at any point
        NoBattlePlayerInput,
        Conversation,
        EnableBattlePlayerInput,
    }

    public class BattleSceneFlow : DynamicSceneFlow
    {
        // Once this reaches Combat.Battle, activate a mini state machine
        // that goes NoBattlePlayerInput -> EnableBattlePlayerInput -> NoBattlePlayerInput
        // Conversation can interrupt at any point
        // TODO: Hook this in to the TurnRotisserie
        public MiniBattleState CurrentMiniBattleState { get; private set; } =
            MiniBattleState.NoBattlePlayerInput;

        public bool ConversationQueued = false; // Set to true to trigger conversation at next opportunity

        public void QueueConversation() => ConversationQueued = true; // TODO: Connect ConversationController

        public void InitializeMiniBattleState()
        {
            CurrentMiniBattleState = MiniBattleState.NoBattlePlayerInput;
            DisableBattleInput();
            OnPlayerPreTurn.Invoke();
        }

        public UnityEvent OnPlayerPreTurn = new();

        public void ProgressMiniBattleState()
        {
            switch (CurrentMiniBattleState)
            {
                case MiniBattleState.NoBattlePlayerInput:
                    if (ConversationQueued)
                    {
                        CurrentMiniBattleState = MiniBattleState.Conversation;
                        ConversationQueued = false;
                        DisableBattleInput();
                    }
                    else
                    {
                        CurrentMiniBattleState = MiniBattleState.EnableBattlePlayerInput;
                        EnableBattleInput();
                    }
                    break;
                case MiniBattleState.Conversation:
                    CurrentMiniBattleState = MiniBattleState.EnableBattlePlayerInput;
                    EnableBattleInput();
                    break;
                case MiniBattleState.EnableBattlePlayerInput:
                    CurrentMiniBattleState = MiniBattleState.NoBattlePlayerInput;
                    // If TurnRotisserie indicates that this is the start of the player turn, call that event:
                    // for now, just call it
                    if (brain)
                    {
                        OnPlayerPreTurn.Invoke();
                    }

                    DisableBattleInput();
                    break;
            }
        }

        public void EnableBattleInput() => brain.PublishBattleInputEnabled();

        public void DisableBattleInput() => brain.PublishBattleInputDisabled();

        public void HandlePreBattleTransitionToBattleCompleted() =>
            brain?.stateBrain?.HandlePreBattleTransitionToBattleCompleted();

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

        protected void HandlePrecomputeCompleted() => HandlePreBattleTransitionToBattleCompleted();

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
                    StartPreLoading.Invoke();

                    loadingController?.Initialize();

                    var loader =
                        FindFirstObjectByType<Gameplay.Combat.Precompute.BattlePrecomputeLoader>();
                    if (loader != null)
                    {
                        var initRes = loader.Initialize(
                            brain,
                            brain?.battleBrain?.BattleObject?.Context
                        );
                        if (!initRes.Success)
                        {
                            TurnrootLogger.Log(
                                $"DynamicSceneFlow: BattlePrecomputeLoader.Initialize failed: {initRes.ErrorMessage}",
                                TurnrootLogger.LogLevel.Warning
                            );
                            HandlePreBattleTransitionToBattleCompleted();
                        }
                        else
                        {
                            loader.ForceStartPrecomputeIfPossible();
                        }
                    }
                    else
                    {
                        HandlePreBattleTransitionToBattleCompleted();
                    }
                }
            }
        }
    }
}
