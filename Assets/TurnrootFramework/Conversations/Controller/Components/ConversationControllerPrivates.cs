using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
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
            TurnrootLogger.Log(message, TurnrootLogger.LogLevel.Error);
            return false;
        }

        private void CleanupPreviousConversation()
        {
            if (_conversationRoutine != null)
            {
                StopCoroutine(_conversationRoutine);
                _conversationRoutine = null;
            }

            if (_tweenRunId != 0)
            {
                DOTween.Kill(_tweenRunId);
            }

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

            yield return conversation.BranchingConversation
                ? RunBranchingConversation(conversation)
                : RunLinearConversation(conversation);

            instance?.OnConversationFinished?.Invoke();
            OnAnyConversationFinished?.Invoke();
            _conversationRoutine = null;
        }

        private IEnumerator RunBranchingConversation(Conversation conversation)
        {
            var nodes = conversation.GetGraphNodes();
            if (nodes == null || nodes.Count == 0)
            {
                TurnrootLogger.Log(
                    $"Branching conversation '{conversation.name}' has no nodes.",
                    TurnrootLogger.LogLevel.Error
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
                    yield return ProcessLayer(nodeData.conversationLayer, conversation);
                }

                if (nodeData.choices?.Count > 0)
                {
                    _pendingChoiceTarget = int.MinValue;
                    ShowChoicesForNode(currentNodeId);
                    yield return new WaitUntil(() => _pendingChoiceTarget != int.MinValue);
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

        private IEnumerator RunLinearConversation(Conversation conversation)
        {
            for (int i = 0; i < conversation.Layers.Length; i++)
            {
                conversation.CurrentLayerIndex = i;
                yield return ProcessLayer(conversation.Layers[i], conversation, i);
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

            bool completed = false;
            void OnComplete() => completed = true;
            layer.OnLayerComplete.AddListener(OnComplete);
            yield return new WaitUntil(() => completed);
            layer.OnLayerComplete.RemoveListener(OnComplete);

            binding?.OnLayerComplete?.Invoke();

            if (conversation.BranchingConversation)
            {
                _activeBranchingLayer = null;
            }
        }

        private void OnDisable()
        {
            CleanupTweens();

            if (_conversationRoutine != null)
            {
                StopCoroutine(_conversationRoutine);
                _conversationRoutine = null;
            }
        }

        private void OnDestroy() => CleanupTweens();

        private void CleanupTweens()
        {
            if (_tweenRunId != 0)
            {
                DOTween.Kill(_tweenRunId);
            }
        }
    }
}
