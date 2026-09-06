using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Turnroot.AbstractScripts.Graphics2D;
using Turnroot.Characters;
using Turnroot.Conversations.Mermaid;
using Turnroot.Gameplay.Brain;
using Turnroot.UI;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;
using UnityEngine.UI;
using Ease = Turnroot.AbstractScripts.Graphics2D.Graphics2DUtils.Ease;

namespace Turnroot.Conversations
{
    /// <summary>
    /// Manages conversation playback, UI, and branching dialogue choices.
    /// </summary>
    public class ConversationController : MonoBehaviour
    {
        [Header("Audio")]
        public AudioSource _audioSource;

        [Header("Conversation UI")]
        public UIFade _uiFade;

        [Header("Dialogue UI")]
        public TextMeshProUGUI _dialogueText;

        public TextMeshProUGUI _speakerNameText;

        public Image _speakerPortraitImageActive;

        public Image _speakerPortraitImageInactive;

        [Header("Input")]
        public UiInputProvider InputProvider;

        [Header("Choice UI")]
        public GameObject _choiceButtonPrefab;

        public Transform _choiceButtonsContainer;

        public event Action OnAnyConversationStart;
        public event Action OnAnyConversationFinished;

        /// <summary>
        /// The conversation currently being played, or null if none is active.
        /// </summary>
        public Conversation ActiveConversation => _runningConversation;

        /// <summary>
        /// The id of the currently executing Mermaid node, or null if no conversation is active.
        /// </summary>
        public string ActiveNodeId => _activeBranchingNodeId;

        private Coroutine _conversationRoutine;
        private Coroutine _oneShotRoutine;
        private readonly List<Coroutine> _activeTweens = new();
        private Sprite _lastActiveSprite;
        private string _pendingChoiceTarget;
        private string _activeBranchingNodeId;
        private ConversationLayer _activeBranchingLayer;
        private ConversationLayer _activeOneShotLayer;
        private Conversation _runningConversation;
        private MermaidConversationGraph _currentGraph;
        private string _resolvedConditionTarget;
        private bool _waitingForAcknowledgment;
        private readonly Dictionary<string, ConversationLayer.ActiveSpeakerType> _speakerSlots =
            new();
        private int _tweenRunId;
        private bool _inputSubscribed;

        private UiChoice[] _activeChoiceButtons;
        private string[] _activeChoiceTargets;
        private int _currentChoiceIndex;

        private Brain _brain;
        private BattleSceneFlow _sceneFlow;

        private Graphics2DSettings GfxSettings => Graphics2DSettings.Instance;

        private const string ConversationResourcesPath = "Conversations";

        private void OnDisable()
        {
            UnsubscribeInput();
            UnsubscribeConversationConditions();
            UnsubscribePlayerAcknowledgment();
            CancelActiveTweens();
            StopRoutine(ref _conversationRoutine);
            StopRoutine(ref _oneShotRoutine);
        }

        private void StopRoutine(ref Coroutine routine)
        {
            if (routine == null)
            {
                return;
            }

            StopCoroutine(routine);
            routine = null;
        }

        private void SetInterruptWaiting(bool waiting)
        {
            if (_sceneFlow == null)
            {
                return;
            }

            _sceneFlow.ResetInterruptActivityTimer();
            _sceneFlow.InterruptIsWaitingForPlayerInput = waiting;
        }

        private static void SetTextIfNotNull(TextMeshProUGUI text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        private void HandleInput(string action)
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            if (_activeChoiceButtons != null && _activeChoiceButtons.Length > 0)
            {
                InputProvider.Navigate(
                    action,
                    _activeChoiceButtons,
                    ref _currentChoiceIndex,
                    _activeChoiceButtons.Length,
                    OnCurrentChoiceSelected
                );
                return;
            }

            if (_activeOneShotLayer == null && _activeBranchingLayer == null)
            {
                return;
            }

            if (
                action
                is InputActionConstants.Submit
                    or InputActionConstants.Select
                    or InputActionConstants.Start
                    or InputActionConstants.Confirm
            )
            {
                NextLayer();
            }
        }

        private void EnsureInputProvider()
        {
            if (InputProvider != null)
            {
                return;
            }

            InputProvider = GetAndCacheBrain.GetInputProvider();
            if (InputProvider != null)
            {
                return;
            }

            InputProvider = FindFirstObjectByType<UiInputProvider>();
            if (InputProvider != null)
            {
                return;
            }

            "ConversationController: no UiInputProvider found in scene. Add a UiInputProvider to the scene (e.g. on the HubManager or battle input rig) so conversation input is consistent with the rest of the UI.".LogError(
                "ConversationController"
            );
        }

        private void SubscribeInput()
        {
            if (_inputSubscribed)
            {
                return;
            }

            EnsureInputProvider();
            if (InputProvider == null)
            {
                "ConversationController: no UiInputProvider available; cannot handle input.".LogError(
                    "ConversationController"
                );
                return;
            }

            InputProvider.OnInput += HandleInput;
            _inputSubscribed = true;
        }

        private void UnsubscribeInput()
        {
            if (!_inputSubscribed)
            {
                return;
            }

            if (InputProvider != null)
            {
                InputProvider.OnInput -= HandleInput;
            }
            _inputSubscribed = false;
        }

        private void SubscribeConversationConditions()
        {
            if (_brain == null)
            {
                return;
            }

            _brain.OnConversationConditionMet += OnConversationConditionMet;
        }

        private void UnsubscribeConversationConditions()
        {
            if (_brain == null)
            {
                return;
            }

            _brain.OnConversationConditionMet -= OnConversationConditionMet;
        }

        private void SubscribePlayerAcknowledgment()
        {
            if (_brain == null)
            {
                return;
            }

            _brain.OnPlayerAcknowledgedConversationEvent += OnPlayerAcknowledgedConversationEvent;
        }

        private void UnsubscribePlayerAcknowledgment()
        {
            if (_brain == null)
            {
                return;
            }

            _brain.OnPlayerAcknowledgedConversationEvent -= OnPlayerAcknowledgedConversationEvent;
        }

        private void OnPlayerAcknowledgedConversationEvent() => _waitingForAcknowledgment = false;

        private void OnConversationConditionMet(string conversationName, string conditionName)
        {
            if (
                _runningConversation == null
                || !string.Equals(
                    _runningConversation.name,
                    conversationName,
                    StringComparison.Ordinal
                )
            )
            {
                return;
            }

            if (string.IsNullOrEmpty(conditionName) || _currentGraph == null)
            {
                return;
            }

            var resolved = ResolveConditionTarget(_activeBranchingNodeId, conditionName);
            if (!string.IsNullOrEmpty(resolved))
            {
                _resolvedConditionTarget = resolved;
            }
        }

        private string ResolveConditionTarget(string currentNodeId, string conditionName)
        {
            var currentNode = _currentGraph.GetNode(currentNodeId);
            if (currentNode == null)
            {
                return null;
            }

            var outgoing = _currentGraph.GetOutgoing(currentNodeId);

            // If we're standing on a condition node, check it first.
            if (currentNode.Kind == MermaidNodeKind.Condition)
            {
                if (
                    string.Equals(
                        currentNode.ConditionName,
                        conditionName,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return outgoing.Count > 0 ? outgoing[0].ToId : null;
                }

                // Otherwise, allow a more specific outgoing condition to match instead
                // (e.g. AttackBoss -> FelicityAttacked).
                return ResolveConditionTargetFromOutgoing(outgoing, conditionName);
            }

            // If we're on a non-condition node with outgoing condition branches, resolve
            // directly to the node after the matching condition.
            return ResolveConditionTargetFromOutgoing(outgoing, conditionName);
        }

        private string ResolveConditionTargetFromOutgoing(
            List<MermaidEdge> outgoing,
            string conditionName
        )
        {
            foreach (var edge in outgoing)
            {
                var target = _currentGraph.GetNode(edge.ToId);
                if (target?.Kind != MermaidNodeKind.Condition)
                {
                    continue;
                }

                if (
                    string.Equals(
                        target.ConditionName,
                        conditionName,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    var targetOutgoing = _currentGraph.GetOutgoing(target.Id);
                    return targetOutgoing.Count > 0 ? targetOutgoing[0].ToId : null;
                }
            }

            return null;
        }

        public void Advance() => NextLayer();

        /// <summary>
        /// Plays a conversation loaded from Resources/Conversations by its asset name.
        /// Use this from UnityEvents or code that only knows an id string.
        /// </summary>
        public void PlayConversationById(string conversationId, Action onFinished = null)
        {
            if (!TryLoadConversationById(conversationId, out var conversation))
            {
                return;
            }

            if (!CanBeginConversation(conversation))
            {
                return;
            }

            BeginConversation(conversation, onFinished, null, nameof(PlayConversationDirect));
        }

        /// <summary>
        /// Starts a conversation loaded from Resources/Conversations by its asset name,
        /// beginning at the specified Mermaid node id.
        /// </summary>
        public void StartConversationById(string conversationId, string nodeId = null)
        {
            if (!TryLoadConversationById(conversationId, out var conversation))
            {
                return;
            }

            if (!CanBeginConversation(conversation))
            {
                return;
            }

            BeginConversation(conversation, null, nodeId, nameof(StartConversationById));
        }

        private bool TryLoadConversationById(string conversationId, out Conversation conversation)
        {
            conversation = null;
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                return false;
            }

            conversation = Resources.Load<Conversation>(
                $"{ConversationResourcesPath}/{conversationId}"
            );
            if (conversation == null)
            {
                $"ConversationController: could not load conversation '{conversationId}'.".LogError(
                    "ConversationController"
                );
                return false;
            }

            return true;
        }

        private bool BeginConversation(
            Conversation conversation,
            Action onFinished,
            string startNodeId,
            string callerName
        )
        {
            if (conversation == null)
            {
                $"{callerName} called with null conversation.".LogInfo();
                return false;
            }

            if (conversation.MermaidSource == null)
            {
                $"Conversation '{conversation.name}' has no Mermaid source.".LogError(
                    "ConversationController"
                );
                return false;
            }

            StartConversationInternal(conversation, onFinished, startNodeId);
            return true;
        }

        private bool CanBeginConversation(Conversation conversation)
        {
            if (conversation == null)
            {
                return false;
            }

            var conversationalBrain =
                _brain?.conversationalBrain ?? GetAndCacheBrain.GetBrain()?.conversationalBrain;
            if (conversationalBrain == null)
            {
                return true;
            }

            if (!conversationalBrain.CanStartConversation(conversation))
            {
                $"ConversationController: cannot start '{conversation.name}' because it has already been played and CanRepeat is false.".LogInfo();
                return false;
            }

            return true;
        }

        public void NextLayer()
        {
            if (_activeOneShotLayer != null)
            {
                _activeOneShotLayer.CompleteLayer();
                return;
            }

            _activeBranchingLayer?.CompleteLayer();
        }

        public bool ChooseBranchTarget(string targetNodeId)
        {
            _pendingChoiceTarget = targetNodeId;
            ClearChoiceButtons();
            return true;
        }

        /// <summary>
        /// Plays a full <see cref="Conversation"/> asset immediately.
        /// Intended for runtime-selected conversations (e.g. hub chitchat).
        /// <paramref name="onFinished"/> is called once the conversation completes.
        /// </summary>
        public void PlayConversationDirect(Conversation conversation, Action onFinished = null)
        {
            if (!CanBeginConversation(conversation))
            {
                return;
            }

            BeginConversation(conversation, onFinished, null, nameof(PlayConversationDirect));
        }

        /// <summary>
        /// Plays a <see cref="Conversation"/> starting at a specific node id. Use this to resume
        /// sub-conversations split across scenes (e.g. PART2_Start after a scene transition).
        /// </summary>
        public void PlayConversationDirectFromNode(
            Conversation conversation,
            string startNodeId,
            Action onFinished = null
        )
        {
            if (!CanBeginConversation(conversation))
            {
                return;
            }

            BeginConversation(
                conversation,
                onFinished,
                startNodeId,
                nameof(PlayConversationDirectFromNode)
            );
        }

        private void StartConversationInternal(
            Conversation conversation,
            Action onFinished,
            string startNodeId = null
        )
        {
            CleanupPreviousConversation();
            ResetUI();
            ShowConversationUI();

            _runningConversation = conversation;
            _brain = GetAndCacheBrain.GetBrain();
            _sceneFlow = FindFirstObjectByType<BattleSceneFlow>();
            _speakerSlots.Clear();
            _resolvedConditionTarget = null;

            OnAnyConversationStart?.Invoke();
            _brain?.PublishConversationStarted(conversation);
            _brain?.conversationalBrain?.MarkConversationStarted(conversation);

            SubscribeInput();
            SubscribeConversationConditions();
            SubscribePlayerAcknowledgment();
            _conversationRoutine = StartCoroutine(
                RunMermaidGraph(conversation, onFinished, startNodeId)
            );
        }

        private void CleanupPreviousConversation()
        {
            StopRoutine(ref _conversationRoutine);
            CancelActiveTweens();
            UnsubscribeInput();
            ClearChoiceButtons();
            _tweenRunId++;
        }

        private void ResetUI()
        {
            Graphics2DUtils.ResetImage(_speakerPortraitImageActive);
            Graphics2DUtils.ResetImage(_speakerPortraitImageInactive);
            _lastActiveSprite = null;
            SetTextIfNotNull(_dialogueText, string.Empty);
            SetTextIfNotNull(_speakerNameText, string.Empty);
            ClearChoiceButtons();
        }

        private IEnumerator RunMermaidGraph(
            Conversation conversation,
            Action onFinished,
            string startNodeId = null
        )
        {
            _currentGraph = conversation.GetGraph();
            if (_currentGraph == null || _currentGraph.Nodes.Count == 0)
            {
                $"Conversation '{conversation.name}' has no nodes.".LogError(
                    "ConversationController.RunMermaidGraph"
                );
                ResetUI();
                yield break;
            }

            MermaidNode currentNode = null;
            if (!string.IsNullOrEmpty(startNodeId))
            {
                currentNode = _currentGraph.GetNode(startNodeId);
                if (currentNode == null)
                {
                    $"Conversation '{conversation.name}' has no node with id '{startNodeId}'. Falling back to entry node.".LogWarning();
                }
            }

            if (currentNode == null)
            {
                var entries = _currentGraph.GetEntryNodes();
                if (entries.Count == 0)
                {
                    $"Conversation '{conversation.name}' has no PART<N>_Start node. Add a Start node to define where the conversation begins.".LogError(
                        "ConversationController.RunMermaidGraph"
                    );
                    ResetUI();
                    yield break;
                }

                currentNode = entries[0];
            }

            while (currentNode != null)
            {
                _activeBranchingNodeId = currentNode.Id;

                switch (currentNode.Kind)
                {
                    case MermaidNodeKind.Dialogue:
                    {
                        var layer = BuildLayerFromNode(currentNode, conversation.People);
                        if (layer != null)
                        {
                            yield return ProcessLayer(layer);
                        }

                        break;
                    }

                    case MermaidNodeKind.Action:
                    {
                        var actionResult = ConversationActionExecutor.Execute(
                            currentNode,
                            conversation,
                            this
                        );

                        if (!actionResult.Success)
                        {
                            // The executor already logs the failure with OperationResult.
                            // Continue without blocking the conversation on a failed action.
                            break;
                        }

                        if (actionResult.Value)
                        {
                            _brain?.PublishWaitForPlayerAcknowledgment(currentNode.Id);
                            yield return WaitForPlayerAcknowledgment(currentNode.Id);
                        }

                        break;
                    }

                    case MermaidNodeKind.Condition:
                        yield return WaitForConditionNode();
                        currentNode = _currentGraph.GetNode(_resolvedConditionTarget);
                        continue;

                    case MermaidNodeKind.Start:
                    case MermaidNodeKind.Choice:
                        // Routing for Choice is handled below; Start is a pass-through marker.
                        break;
                }

                var outgoing = _currentGraph.GetOutgoing(currentNode.Id);
                if (outgoing.Count == 0)
                {
                    break;
                }

                if (AllTargetsAreChoices(outgoing))
                {
                    _pendingChoiceTarget = null;
                    ShowChoices(outgoing);

                    SetInterruptWaiting(true);
                    yield return new WaitUntil(() => !string.IsNullOrEmpty(_pendingChoiceTarget));
                    SetInterruptWaiting(false);

                    currentNode = _currentGraph.GetNode(_pendingChoiceTarget);
                    ClearChoiceButtons();
                    continue;
                }

                if (outgoing.Count == 1)
                {
                    currentNode = _currentGraph.GetNode(outgoing[0].ToId);
                    continue;
                }

                if (AllTargetsAreConditions(outgoing))
                {
                    var firstCondition = _currentGraph.GetNode(outgoing[0].ToId);
                    yield return WaitForConditionNode();
                    currentNode = _currentGraph.GetNode(_resolvedConditionTarget);
                    continue;
                }

                $"ConversationController: node '{currentNode.Id}' has multiple outgoing edges that are not choices or conditions. Picking first.".LogWarning();
                currentNode = _currentGraph.GetNode(outgoing[0].ToId);
            }

            _activeBranchingNodeId = null;
            _activeBranchingLayer = null;
            _pendingChoiceTarget = null;
            _resolvedConditionTarget = null;

            onFinished?.Invoke();
            UnsubscribeInput();
            UnsubscribeConversationConditions();
            UnsubscribePlayerAcknowledgment();
            OnAnyConversationFinished?.Invoke();
            _brain?.PublishConversationEnded(conversation);

            if (
                _sceneFlow != null
                && _sceneFlow.IsInterruptQueued
                && _sceneFlow.CurrentInterrupt == InterruptType.Conversation
            )
            {
                _sceneFlow.CompleteInterrupt();
            }

            _conversationRoutine = null;
            _runningConversation = null;
            _currentGraph = null;
        }

        private bool AllTargetsAreChoices(List<MermaidEdge> edges)
        {
            if (edges.Count == 0)
            {
                return false;
            }

            foreach (var edge in edges)
            {
                var node = _currentGraph.GetNode(edge.ToId);
                if (node?.Kind != MermaidNodeKind.Choice)
                {
                    return false;
                }
            }

            return true;
        }

        private bool AllTargetsAreConditions(List<MermaidEdge> edges)
        {
            if (edges.Count == 0)
            {
                return false;
            }

            foreach (var edge in edges)
            {
                var node = _currentGraph.GetNode(edge.ToId);
                if (node?.Kind != MermaidNodeKind.Condition)
                {
                    return false;
                }
            }

            return true;
        }

        private IEnumerator WaitForConditionNode()
        {
            _resolvedConditionTarget = null;

            SetInterruptWaiting(true);
            yield return new WaitUntil(() => !string.IsNullOrEmpty(_resolvedConditionTarget));
            SetInterruptWaiting(false);
        }

        private IEnumerator WaitForPlayerAcknowledgment(string nodeId)
        {
            if (_brain == null)
            {
                yield break;
            }

            _waitingForAcknowledgment = true;
            SetInterruptWaiting(true);
        }

        private ConversationLayer BuildLayerFromNode(
            MermaidNode node,
            List<ConversationPerson> people
        )
        {
            var person = people?.FirstOrDefault(p =>
                string.Equals(p.SpeakerName, node.Speaker, StringComparison.OrdinalIgnoreCase)
            );

            var character = person?.Character;
            var displayName = person?.ResolvedDisplayName ?? node.Speaker;

            if (!_speakerSlots.ContainsKey(node.Speaker))
            {
                var slot =
                    _speakerSlots.Count == 0
                        ? ConversationLayer.ActiveSpeakerType.Primary
                        : ConversationLayer.ActiveSpeakerType.Secondary;
                _speakerSlots[node.Speaker] = slot;
            }

            var activeType = _speakerSlots[node.Speaker];

            var layer = new ConversationLayer
            {
                Dialogue = node.Text,
                ParsePronouns = true,
                ReferringTo = character != null ? new[] { character } : null,
                ActiveSpeaker = activeType,
            };

            if (activeType == ConversationLayer.ActiveSpeakerType.Primary)
            {
                layer.Speaker = character;
                layer.SpeakerDisplayName = displayName;
                layer.SpeakerPortraitKey = ResolvePortraitKey(character, node.Emotion);
            }
            else
            {
                layer.SecondarySpeaker = character;
                layer.SecondarySpeakerDisplayName = displayName;
                layer.SecondarySpeakerPortraitKey = ResolvePortraitKey(character, node.Emotion);
            }

            return layer;
        }

        private string ResolvePortraitKey(CharacterData character, string emotion)
        {
            if (string.IsNullOrWhiteSpace(emotion))
            {
                return "default";
            }

            if (character == null)
            {
                return "default";
            }

            if (character.ContainsPortraitKey(emotion))
            {
                return emotion;
            }

            var caseInsensitiveKey = character
                .GetPortraitKeys()
                ?.FirstOrDefault(k =>
                    string.Equals(k, emotion, StringComparison.OrdinalIgnoreCase)
                );

            return !string.IsNullOrEmpty(caseInsensitiveKey) ? caseInsensitiveKey : "default";
        }

        private IEnumerator ProcessLayer(ConversationLayer layer)
        {
            if (!layer.HasBeenParsed)
            {
                layer.ParseDialogue();
            }

            layer.StartLayer();
            _brain?.PublishConversationLayerStarted(layer);

            _activeBranchingLayer = layer;
            UpdateUIForLayer(layer);

            SetInterruptWaiting(true);

            bool completed = false;
            void OnComplete() => completed = true;
            layer.OnLayerCompleted += OnComplete;
            yield return new WaitUntil(() => completed);
            layer.OnLayerCompleted -= OnComplete;

            SetInterruptWaiting(false);
            _brain?.PublishConversationLayerEnded(layer);
            _activeBranchingLayer = null;
        }

        private void ShowConversationUI() => _uiFade?.Show();

        private void HideConversationUI() => _uiFade?.Hide();

        /// <summary>
        /// Play a short, one‑layer conversation (e.g. a single NPC quip).
        /// This is intended for lightweight notifications or UI flavor.
        /// </summary>
        public void PlayOneShot(OneShot oneShot)
        {
            if (string.IsNullOrWhiteSpace(oneShot.Dialogue))
            {
                "PlayOneShot called with empty dialogue".LogInfo();
                return;
            }

            ShowConversationUI();

            if (oneShot.Audio != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(oneShot.Audio);
            }

            CleanupPreviousConversation();
            ResetUI();
            SubscribeInput();
            _oneShotRoutine = StartCoroutine(RunOneShot(oneShot));
        }

        private IEnumerator RunOneShot(OneShot oneShot)
        {
            _activeOneShotLayer = CreateOneShotLayer(oneShot);
            _brain = GetAndCacheBrain.GetBrain();
            _sceneFlow = FindFirstObjectByType<BattleSceneFlow>();

            OnAnyConversationStart?.Invoke();
            yield return ProcessLayer(_activeOneShotLayer);
            OnAnyConversationFinished?.Invoke();

            _activeOneShotLayer = null;
            _oneShotRoutine = null;
            UnsubscribeInput();
            HideConversationUI();
        }

        private ConversationLayer CreateOneShotLayer(OneShot oneShot)
        {
            var layer = new ConversationLayer
            {
                Dialogue = oneShot.Dialogue,
                SpeakerDisplayName = oneShot.SpeakerName ?? string.Empty,
                ParsePronouns = false,
            };
            layer.SetPrimaryPortraitSprite(oneShot.Portrait);
            return layer;
        }

        private void UpdateUIForLayer(ConversationLayer layer)
        {
            if (layer == null)
            {
                return;
            }

            SetTextIfNotNull(_dialogueText, layer.Dialogue);
            SetTextIfNotNull(_speakerNameText, GetSpeakerName(layer.GetActiveSlot()));

            var (activeSprite, _, _, _) = layer.GetActiveAndInactivePortraits();
            if (_lastActiveSprite != activeSprite)
            {
                ApplyPortraitForLayer(layer);
                _lastActiveSprite = activeSprite;
            }
        }

        private string GetSpeakerName(ConversationLayer.SpeakerSlot slot)
        {
            return slot == null ? "???"
                : !string.IsNullOrWhiteSpace(slot.DisplayName) ? slot.DisplayName
                : slot.Speaker != null && !string.IsNullOrWhiteSpace(slot.Speaker.DisplayName)
                    ? slot.Speaker.DisplayName
                : "???";
        }

        private void ApplyPortraitForLayer(ConversationLayer layer)
        {
            if (layer == null || _speakerPortraitImageActive == null)
            {
                return;
            }

            var (activeSprite, activeTint, inactiveSprite, inactiveTint) =
                layer.GetActiveAndInactivePortraits();

            if (activeSprite == null)
            {
                return;
            }

            if (_tweenRunId != 0)
            {
                CancelActiveTweens();
            }

            if (GfxSettings?.AnimatePortraitTransitions ?? true)
            {
                Graphics2DUtils.KillImageTweens(
                    _speakerPortraitImageActive,
                    _speakerPortraitImageInactive
                );
            }

            Graphics2DUtils.SetSprite(_speakerPortraitImageActive, activeSprite);
            Graphics2DUtils.SetSprite(_speakerPortraitImageInactive, inactiveSprite);

            _speakerPortraitImageActive.color = activeTint;
            if (_speakerPortraitImageInactive != null)
            {
                _speakerPortraitImageInactive.color = Color.white;
            }

            var duration = GfxSettings?.PortraitTransitionDuration ?? 0.4f;
            var ease = GfxSettings?.PortraitTransitionEase ?? Ease.OutCubic;
            var shouldTintInactive =
                GfxSettings?.SecondaryConversationPortraitInactiveBehavior
                == SecondaryConversationPortraitInactiveBehavior.Tint;

            var tween = shouldTintInactive
                ? Graphics2DUtils.TintCoroutine(
                    _speakerPortraitImageActive,
                    _speakerPortraitImageInactive,
                    activeTint,
                    inactiveTint,
                    duration,
                    ease,
                    _tweenRunId
                )
                : Graphics2DUtils.HideCoroutine(
                    _speakerPortraitImageInactive,
                    duration,
                    ease,
                    _tweenRunId
                );

            StartTween(tween);
        }

        private void ClearChoiceButtons()
        {
            if (_activeChoiceButtons != null)
            {
                foreach (var choice in _activeChoiceButtons)
                {
                    choice?.Deselect();
                }
            }

            _activeChoiceButtons = null;
            _activeChoiceTargets = null;
            _currentChoiceIndex = 0;

            if (_choiceButtonsContainer == null)
            {
                return;
            }

            for (int i = _choiceButtonsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_choiceButtonsContainer.GetChild(i).gameObject);
            }
        }

        private void ShowChoices(List<MermaidEdge> choiceEdges)
        {
            if (_choiceButtonPrefab == null || _choiceButtonsContainer == null)
            {
                return;
            }

            ClearChoiceButtons();

            var choices = new List<UiChoice>();
            var targets = new List<string>();

            foreach (var edge in choiceEdges)
            {
                var choiceNode = _currentGraph.GetNode(edge.ToId);
                if (choiceNode == null)
                {
                    continue;
                }

                var (choice, targetId) = CreateChoiceButton(choiceNode);
                if (choice != null)
                {
                    choices.Add(choice);
                    targets.Add(targetId);
                }
            }

            _activeChoiceButtons = choices.ToArray();
            _activeChoiceTargets = targets.ToArray();
            _currentChoiceIndex = 0;

            if (_activeChoiceButtons.Length > 0)
            {
                _activeChoiceButtons[0].Select();
            }
        }

        private (UiChoice Choice, string TargetId) CreateChoiceButton(MermaidNode choiceNode)
        {
            var go = Instantiate(_choiceButtonPrefab, _choiceButtonsContainer);
            if (go == null)
            {
                return (null, null);
            }

            go.SetActive(true);

            var btn = go.GetComponent<Button>();
            var img = go.GetComponent<Image>();
            var label = go.GetComponentInChildren<TextMeshProUGUI>(true);
            var choice = go.GetComponent<UiChoice>();

            if (label != null)
            {
                label.gameObject.SetActive(true);
                label.text = GetChoiceLabel(choiceNode);
            }

            if (img != null)
            {
                img.enabled = true;
            }

            string targetId = null;
            if (btn != null)
            {
                btn.gameObject.SetActive(true);
                btn.interactable = true;
                var outgoing = _currentGraph.GetOutgoing(choiceNode.Id);
                targetId = outgoing.Count > 0 ? outgoing[0].ToId : null;
                btn.onClick.AddListener(() => OnChoiceSelected(targetId));
            }

            return (choice, targetId);
        }

        private string GetChoiceLabel(MermaidNode choiceNode) =>
            !string.IsNullOrEmpty(choiceNode?.Text) ? choiceNode.Text : "Choice";

        private void OnChoiceSelected(string targetId)
        {
            _pendingChoiceTarget = targetId;
            ClearChoiceButtons();
        }

        private void OnCurrentChoiceSelected() =>
            OnChoiceSelected(_activeChoiceTargets[_currentChoiceIndex]);

        private Coroutine StartTween(IEnumerator routine)
        {
            if (routine == null)
            {
                return null;
            }

            var c = StartCoroutine(routine);
            _activeTweens.Add(c);
            return c;
        }

        private void CancelActiveTweens()
        {
            foreach (var c in _activeTweens)
            {
                if (c != null)
                {
                    StopCoroutine(c);
                }
            }
            _activeTweens.Clear();
            Graphics2DUtils.ClearTransientImages();
        }

        private void OnDestroy() => CancelActiveTweens();
    }
}
