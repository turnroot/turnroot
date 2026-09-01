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
using UnityEngine.InputSystem;
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
        [SerializeField]
        private AudioSource _audioSource;

        [Header("UI")]
        [SerializeField]
        private UIFade _uiFade;

        [Header("Available Conversations")]
        [SerializeField]
        private List<ConversationInstance> _conversationInstances = new();

        [SerializeField]
        private int _currentConversation;

        [Header("UI References")]
        [SerializeField]
        private TextMeshProUGUI _dialogueText;

        [SerializeField]
        private TextMeshProUGUI _speakerNameText;

        [SerializeField]
        private Image _speakerPortraitImageActive;

        [SerializeField]
        private Image _speakerPortraitImageInactive;

        [Header("Choice UI")]
        [SerializeField]
        private GameObject _choiceButtonPrefab;

        [SerializeField]
        private Transform _choiceButtonsContainer;

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
        private readonly Dictionary<string, ConversationLayer.ActiveSpeakerType> _speakerSlots =
            new();
        private int _tweenRunId;
        private bool _inputSubscribed;

        private Brain _brain;
        private BattleSceneFlow _sceneFlow;

        private Graphics2DSettings GfxSettings => Graphics2DSettings.Instance;

        private ConversationInstance SelectedInstance =>
            _conversationInstances != null
            && _currentConversation >= 0
            && _currentConversation < _conversationInstances.Count
                ? _conversationInstances[_currentConversation]
                : null;

        private Conversation SelectedConversation => SelectedInstance?.Conversation;

        private void Awake() => EnsureAudioSource();

        private void OnDisable()
        {
            UnsubscribeAdvanceInput();
            UnsubscribeConversationConditions();
            CancelActiveTweens();

            if (_conversationRoutine != null)
            {
                StopCoroutine(_conversationRoutine);
                _conversationRoutine = null;
            }

            if (_oneShotRoutine != null)
            {
                StopCoroutine(_oneShotRoutine);
                _oneShotRoutine = null;
            }
        }

        private void OnAdvanceInputPerformed(InputAction.CallbackContext context)
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            if (_activeOneShotLayer == null && _activeBranchingLayer == null)
            {
                return;
            }

            NextLayer();
        }

        private void SubscribeAdvanceInput()
        {
            if (_inputSubscribed)
            {
                return;
            }

            var action = UIInputActionDefaults.Select;
            if (action != null)
            {
                action.performed += OnAdvanceInputPerformed;
                _inputSubscribed = true;
            }
        }

        private void UnsubscribeAdvanceInput()
        {
            if (!_inputSubscribed)
            {
                return;
            }

            var action = UIInputActionDefaults.Select;
            if (action != null)
            {
                action.performed -= OnAdvanceInputPerformed;
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

        private void EnsureAudioSource()
        {
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
            }

            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
            }
        }

        public void Advance() => NextLayer();

        public void StartCurrentConversation() => StartConversation();

        public void StartConversationAtIndex(int index)
        {
            if (index < 0 || index >= _conversationInstances.Count)
            {
                return;
            }

            _currentConversation = index;
            StartConversation();
        }

        /// <summary>
        /// Starts the current registered conversation at a specific Mermaid node id.
        /// Use this to resume a sub-conversation (e.g. PART2_Start) after a scene transition.
        /// </summary>
        public void StartConversationFromNode(string nodeId)
        {
            var instance = SelectedInstance;
            if (instance == null)
            {
                $"No ConversationInstance selected at index {_currentConversation}".LogError(
                    "ConversationController"
                );
                return;
            }

            if (SelectedConversation == null)
            {
                $"Instance '{instance.name}' has no Conversation assigned.".LogError(
                    "ConversationController"
                );
                return;
            }

            if (SelectedConversation.MermaidSource == null)
            {
                ResetUI();
                $"Conversation '{SelectedConversation.name}' has no Mermaid source.".LogError(
                    "ConversationController"
                );
                return;
            }

            StartConversationInternal(SelectedConversation, instance, null, nodeId);
        }

        public void IncrementConversationIndex()
        {
            if (_conversationInstances == null || _conversationInstances.Count == 0)
            {
                return;
            }

            _currentConversation = (_currentConversation + 1) % _conversationInstances.Count;
        }

        public void DecrementConversationIndex()
        {
            if (_conversationInstances == null || _conversationInstances.Count == 0)
            {
                return;
            }

            _currentConversation =
                (_currentConversation - 1 + _conversationInstances.Count)
                % _conversationInstances.Count;
        }

        public void StartConversation()
        {
            var instance = SelectedInstance;
            if (instance == null)
            {
                $"No ConversationInstance selected at index {_currentConversation}".LogError(
                    "ConversationController"
                );
                return;
            }

            if (SelectedConversation == null)
            {
                $"Instance '{instance.name}' has no Conversation assigned.".LogError(
                    "ConversationController"
                );
                return;
            }

            if (SelectedConversation.MermaidSource == null)
            {
                ResetUI();
                $"Conversation '{SelectedConversation.name}' has no Mermaid source.".LogError(
                    "ConversationController"
                );
                return;
            }

            StartConversationInternal(SelectedConversation, instance, null);
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

        public List<MermaidEdge> GetCurrentChoices()
        {
            if (string.IsNullOrEmpty(_activeBranchingNodeId) || _currentGraph == null)
            {
                return null;
            }

            return _currentGraph.GetOutgoing(_activeBranchingNodeId);
        }

        /// <summary>
        /// Plays a full <see cref="Conversation"/> asset immediately without requiring it to be
        /// pre-registered in the <c>ConversationInstances</c> list.
        /// Intended for runtime-selected conversations (e.g. hub chitchat).
        /// <paramref name="onFinished"/> is called once the conversation completes.
        /// </summary>
        public void PlayConversationDirect(Conversation conversation, Action onFinished = null)
        {
            if (conversation == null)
            {
                "PlayConversationDirect called with null conversation.".LogInfo();
                return;
            }

            if (conversation.MermaidSource == null)
            {
                $"Conversation '{conversation.name}' has no Mermaid source.".LogError(
                    "ConversationController"
                );
                return;
            }

            StartConversationInternal(conversation, null, onFinished, null);
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
            if (conversation == null)
            {
                "PlayConversationDirectFromNode called with null conversation.".LogInfo();
                return;
            }

            if (conversation.MermaidSource == null)
            {
                $"Conversation '{conversation.name}' has no Mermaid source.".LogError(
                    "ConversationController"
                );
                return;
            }

            StartConversationInternal(conversation, null, onFinished, startNodeId);
        }

        private void StartConversationInternal(
            Conversation conversation,
            ConversationInstance instance,
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

            SubscribeAdvanceInput();
            SubscribeConversationConditions();
            _conversationRoutine = StartCoroutine(
                RunMermaidGraph(conversation, onFinished, startNodeId)
            );
        }

        private void CleanupPreviousConversation()
        {
            if (_conversationRoutine != null)
            {
                StopCoroutine(_conversationRoutine);
                _conversationRoutine = null;
            }

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
                currentNode = entries.Count > 0 ? entries[0] : _currentGraph.Nodes[0];
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
                        ConversationActionExecutor.Execute(currentNode, conversation, this);
                        break;

                    case MermaidNodeKind.Signal:
                        ConversationSignalEmitter.Emit(currentNode, conversation);
                        break;

                    case MermaidNodeKind.Condition:
                        yield return WaitForConditionNode(currentNode);
                        currentNode = _currentGraph.GetNode(_resolvedConditionTarget);
                        continue;

                    case MermaidNodeKind.Anchor:
                    case MermaidNodeKind.Choice:
                        // Routing for Choice is handled below; Anchor is a pass-through marker.
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

                    _sceneFlow?.ResetInterruptActivityTimer();
                    if (_sceneFlow != null)
                    {
                        _sceneFlow.InterruptIsWaitingForPlayerInput = true;
                    }

                    yield return new WaitUntil(() => !string.IsNullOrEmpty(_pendingChoiceTarget));

                    _sceneFlow?.ResetInterruptActivityTimer();
                    if (_sceneFlow != null)
                    {
                        _sceneFlow.InterruptIsWaitingForPlayerInput = false;
                    }

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
                    yield return WaitForConditionNode(firstCondition);
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
            UnsubscribeAdvanceInput();
            UnsubscribeConversationConditions();
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

        private IEnumerator WaitForConditionNode(MermaidNode conditionNode)
        {
            _resolvedConditionTarget = null;

            _sceneFlow?.ResetInterruptActivityTimer();
            if (_sceneFlow != null)
            {
                _sceneFlow.InterruptIsWaitingForPlayerInput = true;
            }

            yield return new WaitUntil(() => !string.IsNullOrEmpty(_resolvedConditionTarget));

            _sceneFlow?.ResetInterruptActivityTimer();
            if (_sceneFlow != null)
            {
                _sceneFlow.InterruptIsWaitingForPlayerInput = false;
            }
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

            _sceneFlow?.ResetInterruptActivityTimer();
            if (_sceneFlow != null)
            {
                _sceneFlow.InterruptIsWaitingForPlayerInput = true;
            }

            bool completed = false;
            void OnComplete() => completed = true;
            layer.OnLayerCompleted += OnComplete;
            yield return new WaitUntil(() => completed);
            layer.OnLayerCompleted -= OnComplete;

            _sceneFlow?.ResetInterruptActivityTimer();
            if (_sceneFlow != null)
            {
                _sceneFlow.InterruptIsWaitingForPlayerInput = false;
            }

            _brain?.PublishConversationLayerEnded(layer);
            _activeBranchingLayer = null;
        }

        private void ShowConversationUI()
        {
            if (_uiFade != null)
            {
                _uiFade.Show();
            }
            else if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
            }
        }

        private void HideConversationUI()
        {
            if (_uiFade != null)
            {
                _uiFade.Hide();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

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
            else if (oneShot.Audio != null)
            {
                EnsureAudioSource();
                if (_audioSource != null)
                {
                    _audioSource.PlayOneShot(oneShot.Audio);
                }
            }

            CleanupPreviousConversation();
            ResetUI();
            SubscribeAdvanceInput();
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

            if (_dialogueText != null)
            {
                _dialogueText.text = layer.Dialogue;
            }

            if (_speakerNameText != null)
            {
                _speakerNameText.text = GetSpeakerName(layer.GetActiveSlot());
            }

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

            if (activeSprite == null || _speakerPortraitImageActive == null)
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

            var activeImg = _speakerPortraitImageActive;
            var inactiveImg = _speakerPortraitImageInactive;

            Graphics2DUtils.SetSprite(activeImg, activeSprite);
            Graphics2DUtils.SetSprite(inactiveImg, inactiveSprite);

            activeImg.color = activeTint;
            if (inactiveImg != null)
            {
                inactiveImg.color = Color.white;
            }

            var duration = GfxSettings?.PortraitTransitionDuration ?? 0.4f;
            var ease = GfxSettings?.PortraitTransitionEase ?? Ease.OutCubic;

            if (
                GfxSettings?.SecondaryConversationPortraitInactiveBehavior
                == SecondaryConversationPortraitInactiveBehavior.Tint
            )
            {
                StartTween(
                    Graphics2DUtils.TintCoroutine(
                        activeImg,
                        inactiveImg,
                        activeTint,
                        inactiveTint,
                        duration,
                        ease,
                        _tweenRunId
                    )
                );
            }
            else
            {
                StartTween(Graphics2DUtils.HideCoroutine(inactiveImg, duration, ease, _tweenRunId));
            }
        }

        private void ClearChoiceButtons()
        {
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

            foreach (var edge in choiceEdges)
            {
                var choiceNode = _currentGraph.GetNode(edge.ToId);
                if (choiceNode == null)
                {
                    continue;
                }

                CreateChoiceButton(choiceNode);
            }
        }

        private void CreateChoiceButton(MermaidNode choiceNode)
        {
            var go = Instantiate(_choiceButtonPrefab, _choiceButtonsContainer);
            if (go == null)
            {
                return;
            }

            go.SetActive(true);

            var btn = go.GetComponent<Button>();
            var img = go.GetComponent<Image>();
            var label = go.GetComponentInChildren<TextMeshProUGUI>(true);

            if (label != null)
            {
                label.gameObject.SetActive(true);
                label.text = GetChoiceLabel(choiceNode);
            }

            if (img != null)
            {
                img.enabled = true;
            }

            if (btn != null)
            {
                btn.gameObject.SetActive(true);
                btn.interactable = true;
                var outgoing = _currentGraph.GetOutgoing(choiceNode.Id);
                var targetId = outgoing.Count > 0 ? outgoing[0].ToId : null;
                btn.onClick.AddListener(() => _pendingChoiceTarget = targetId);
            }
        }

        private string GetChoiceLabel(MermaidNode choiceNode) =>
            !string.IsNullOrEmpty(choiceNode?.Text) ? choiceNode.Text : "Choice";

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

            foreach (var img in FindObjectsByType<Image>(FindObjectsSortMode.None))
            {
                if (img.gameObject.name.StartsWith("swap_overlay_"))
                {
                    Destroy(img.gameObject);
                }
            }
        }

        private void OnDestroy() => CancelActiveTweens();
    }
}
