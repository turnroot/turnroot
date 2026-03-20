using System.Collections;
using System.Collections.Generic;
using Turnroot.AbstractScripts.Graphics2D;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Conversations
{
    /// <summary>
    /// Partial class managing conversation execution flow, validation, and coroutine lifecycle for both linear and branching conversations.
    /// </summary>
    public partial class ConversationController : MonoBehaviour
    {
        private bool ValidateConversationStart()
        {
            if (!Application.isPlaying)
            {
                return LogError("StartConversation must be run in Play Mode.");
            }

            if (SelectedInstance == null)
            {
                return LogError(
                    $"No ConversationInstance selected at index {_currentConversation}"
                );
            }

            if (SelectedConversation == null)
            {
                return LogError(
                    $"Instance '{SelectedInstance.name}' has no Conversation assigned."
                );
            }

            if (
                SelectedConversation.BranchingConversation
                && SelectedConversation.ConversationGraph == null
            )
            {
                ResetUI();
                return LogError(
                    $"Conversation '{SelectedConversation.name}' is branching but has no graph."
                );
            }
            return true;
        }

        private static bool LogError(string message)
        {
            message.LogError("ConversationController.ValidateConversationStart");
            return false;
        }

        private void CleanupPreviousConversation()
        {
            if (_conversationRoutine != null)
            {
                StopCoroutine(_conversationRoutine);
                _conversationRoutine = null;
            }

            // cancel any in‑flight animations, bump run id so any that slip through will bail
            CancelActiveTweens();
            _tweenRunId++;
        }

        private void ResetUI()
        {
            Graphics2DUtils.ResetImage(_speakerPortraitImageActive);
            Graphics2DUtils.ResetImage(_speakerPortraitImageInactive);
            _lastActiveSprite = null;
            if (_dialogueText != null)
            {
                _dialogueText.text = string.Empty;
            }

            if (_speakerNameText != null)
            {
                _speakerNameText.text = string.Empty;
            }

            ClearChoiceButtons();
        }

        private IEnumerator RunConversation(ConversationInstance instance)
        {
            if (instance?.Conversation == null)
            {
                yield break;
            }

            var conversation = instance.Conversation;

            // Get scene flow reference once at start
            var sceneFlow = FindFirstObjectByType<Utilities.AbstractScripts.BattleSceneFlow>();

            yield return conversation.BranchingConversation
                ? RunBranchingConversation(conversation, sceneFlow)
                : RunLinearConversation(conversation, sceneFlow);

            instance?.OnConversationFinished?.Invoke();
            UnsubscribeAdvanceInput();
            OnAnyConversationFinished?.Invoke();

            // Complete any battle scene interrupt that was waiting for this conversation
            if (
                sceneFlow != null
                && sceneFlow.IsInterruptQueued
                && sceneFlow.CurrentInterrupt
                    == Utilities.AbstractScripts.InterruptType.Conversation
            )
            {
                sceneFlow.CompleteInterrupt();
            }

            _conversationRoutine = null;
        }

        private IEnumerator RunBranchingConversation(
            Conversation conversation,
            Utilities.AbstractScripts.BattleSceneFlow sceneFlow
        )
        {
            var nodes = conversation.GetGraphNodes();
            if (nodes == null || nodes.Count == 0)
            {
                $"Branching conversation '{conversation.name}' has no nodes.".LogError(
                    "ConversationController.RunBranchingConversation"
                );
                ResetUI();
                yield break;
            }

            int currentNodeId = FindEntryNode(nodes);

            while (currentNodeId != int.MinValue)
            {
                if (!nodes.TryGetValue(currentNodeId, out var nodeData) || nodeData == null)
                {
                    break;
                }

                _activeBranchingNodeId = currentNodeId;

                if (nodeData.conversationLayer != null)
                {
                    yield return ProcessLayer(nodeData.conversationLayer, conversation, sceneFlow);
                }

                if (nodeData.choices?.Count > 0)
                {
                    _pendingChoiceTarget = int.MinValue;
                    ShowChoicesForNode(currentNodeId);

                    // We're waiting for player to make a choice
                    sceneFlow?.ResetInterruptActivityTimer();
                    if (sceneFlow != null)
                    {
                        sceneFlow.InterruptIsWaitingForPlayerInput = true;
                    }

                    yield return new WaitUntil(() => _pendingChoiceTarget != int.MinValue);

                    // Player made a choice - reset timer and clear input flag
                    sceneFlow?.ResetInterruptActivityTimer();
                    if (sceneFlow != null)
                    {
                        sceneFlow.InterruptIsWaitingForPlayerInput = false;
                    }

                    currentNodeId = _pendingChoiceTarget;
                    ClearChoiceButtons();
                    continue;
                }

                currentNodeId = nodeData.nextTargetId;
            }

            _activeBranchingNodeId = int.MinValue;
            _activeBranchingLayer = null;
            _pendingChoiceTarget = int.MinValue;
        }

        private IEnumerator RunLinearConversation(
            Conversation conversation,
            Utilities.AbstractScripts.BattleSceneFlow sceneFlow
        )
        {
            for (int i = 0; i < conversation.Layers.Length; i++)
            {
                conversation.CurrentLayerIndex = i;
                yield return ProcessLayer(conversation.Layers[i], conversation, sceneFlow, i);
            }
        }

        private int FindEntryNode(Dictionary<int, NodeData> nodes)
        {
            foreach (var kv in nodes)
            {
                if (kv.Value?.node != null && kv.Value.incomingCount == 0)
                {
                    return kv.Key;
                }
            }

            foreach (var kv in nodes)
            {
                return kv.Key;
            }

            return int.MinValue;
        }

        private IEnumerator ProcessLayer(
            ConversationLayer layer,
            Conversation conversation,
            Utilities.AbstractScripts.BattleSceneFlow sceneFlow,
            int? layerIndex = null
        )
        {
            if (!layer.HasBeenParsed)
            {
                layer.ParseDialogue();
            }

            layer.StartLayer();
            var binding = layerIndex.HasValue
                ? _runningInstance?.GetEventsForLayer(layerIndex.Value)
                : null;
            binding?.OnLayerStart?.Invoke();

            if (conversation.BranchingConversation)
            {
                _activeBranchingLayer = layer;
            }

            UpdateUIForLayer(layer);

            // Waiting for player to advance this layer
            sceneFlow?.ResetInterruptActivityTimer();
            if (sceneFlow != null)
            {
                sceneFlow.InterruptIsWaitingForPlayerInput = true;
            }

            bool completed = false;
            void OnComplete() => completed = true;
            layer.OnLayerComplete.AddListener(OnComplete);
            yield return new WaitUntil(() => completed);
            layer.OnLayerComplete.RemoveListener(OnComplete);

            // Player advanced - reset timer and clear input flag
            sceneFlow?.ResetInterruptActivityTimer();
            if (sceneFlow != null)
            {
                sceneFlow.InterruptIsWaitingForPlayerInput = false;
            }

            binding?.OnLayerComplete?.Invoke();

            if (conversation.BranchingConversation)
            {
                _activeBranchingLayer = null;
            }
        }

        private void OnDestroy()
        {
            CleanupTweens();
        }

        private void CleanupTweens() => CancelActiveTweens();
    }
}
