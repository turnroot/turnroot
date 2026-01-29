using System;
using System.Collections;
using System.Collections.Generic;
using Turnroot.Gameplay.Brain;
using UnityEngine;
using UnityEngine.Events;

namespace Turnroot.Utilities.AbstractScripts
{
    public enum MiniBattleState
    {
        // A conversation can interrupt at any point
        BattleTurnStart,
        Conversation,
        BattleTurn,
    }

    public class BattleSceneFlow : DynamicSceneFlow
    {
        // Once this reaches Combat.Battle, activate a mini state machine
        // that goes BattleTurnStart -> BattleTurn -> BattleTurnStart
        // Conversation can interrupt at any point
        // TODO: Hook this in to the TurnRotisserie
        public MiniBattleState CurrentMiniBattleState { get; private set; } =
            MiniBattleState.BattleTurnStart;

        public bool ConversationQueued = false; // Set to true to trigger conversation at next opportunity

        public void QueueConversation() => ConversationQueued = true; // TODO: Connect ConversationController

        public void InitializeMiniBattleState()
        {
            CurrentMiniBattleState = MiniBattleState.BattleTurnStart;
            DisableBattleInput();
            OnMiniBattleStateBattleTurnStart.Invoke();
        }

        public UnityEvent OnMiniBattleStateBattleTurnStart = new();
        public UnityEvent OnMiniBattleStateBattleTurn = new();

        public void ProgressMiniBattleState()
        {
            TurnrootLogger.Log($"Progressing MiniBattleState from {CurrentMiniBattleState}");
            switch (CurrentMiniBattleState)
            {
                case MiniBattleState.BattleTurnStart:
                    if (ConversationQueued)
                    {
                        CurrentMiniBattleState = MiniBattleState.Conversation;
                        ConversationQueued = false;
                        DisableBattleInput();
                    }
                    else
                    {
                        CurrentMiniBattleState = MiniBattleState.BattleTurn;
                        OnMiniBattleStateBattleTurn.Invoke();
                        EnableBattleInput();
                    }
                    break;
                case MiniBattleState.Conversation:
                    CurrentMiniBattleState = MiniBattleState.BattleTurn;
                    OnMiniBattleStateBattleTurn.Invoke();
                    EnableBattleInput();
                    break;
                case MiniBattleState.BattleTurn:
                    CurrentMiniBattleState = MiniBattleState.BattleTurnStart;
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
