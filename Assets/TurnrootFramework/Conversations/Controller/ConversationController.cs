using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Turnroot.AbstractScripts.Graphics2D;
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

        private Coroutine _conversationRoutine;
        private Coroutine _oneShotRoutine;
        private readonly List<Coroutine> _activeTweens = new();
        private Sprite _lastActiveSprite;
        private int _pendingChoiceTarget = int.MinValue;
        private int _activeBranchingNodeId = int.MinValue;
        private ConversationLayer _activeBranchingLayer;
        private ConversationLayer _activeOneShotLayer;
        private ConversationInstance _runningInstance;
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

            if (SelectedConversation.ConversationGraph == null)
            {
                ResetUI();
                $"Conversation '{SelectedConversation.name}' has no graph.".LogError(
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

        public bool ChooseBranchTarget(int targetNodeId)
        {
            _pendingChoiceTarget = targetNodeId;
            ClearChoiceButtons();
            return true;
        }

        public List<ChoiceData> GetCurrentChoices()
        {
            if (_activeBranchingNodeId == int.MinValue)
            {
                return null;
            }

            var nodes = SelectedConversation?.GetGraphNodes();
            return nodes?.TryGetValue(_activeBranchingNodeId, out var node) == true
                ? node.choices
                : null;
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

            if (conversation.ConversationGraph == null)
            {
                $"Conversation '{conversation.name}' has no graph.".LogError(
                    "ConversationController"
                );
                return;
            }

            StartConversationInternal(conversation, null, onFinished);
        }

        private void StartConversationInternal(
            Conversation conversation,
            ConversationInstance instance,
            Action onFinished
        )
        {
            CleanupPreviousConversation();
            ResetUI();
            ShowConversationUI();

            _runningInstance = instance;
            _brain = GetAndCacheBrain.GetBrain();
            _sceneFlow = FindFirstObjectByType<BattleSceneFlow>();

            OnAnyConversationStart?.Invoke();
            _brain?.PublishConversationStarted(conversation);

            SubscribeAdvanceInput();
            _conversationRoutine = StartCoroutine(RunConversationGraph(conversation, onFinished));
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

        private IEnumerator RunConversationGraph(Conversation conversation, Action onFinished)
        {
            var nodes = conversation.GetGraphNodes();
            if (nodes == null || nodes.Count == 0)
            {
                $"Conversation '{conversation.name}' has no nodes.".LogError(
                    "ConversationController.RunConversationGraph"
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

                if (nodeData.node is Branching.ConversationActionNode actionNode)
                {
                    actionNode.Execute(this);
                    currentNodeId = nodeData.nextTargetId;
                    continue;
                }

                if (nodeData.conversationLayer != null)
                {
                    yield return ProcessLayer(nodeData.conversationLayer);
                }

                if (nodeData.choices?.Count > 0)
                {
                    _pendingChoiceTarget = int.MinValue;
                    ShowChoicesForNode(currentNodeId);

                    _sceneFlow?.ResetInterruptActivityTimer();
                    if (_sceneFlow != null)
                    {
                        _sceneFlow.InterruptIsWaitingForPlayerInput = true;
                    }

                    yield return new WaitUntil(() => _pendingChoiceTarget != int.MinValue);

                    _sceneFlow?.ResetInterruptActivityTimer();
                    if (_sceneFlow != null)
                    {
                        _sceneFlow.InterruptIsWaitingForPlayerInput = false;
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

            onFinished?.Invoke();
            UnsubscribeAdvanceInput();
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

        private void ShowChoicesForNode(int nodeId)
        {
            if (_choiceButtonPrefab == null || _choiceButtonsContainer == null)
            {
                return;
            }

            var nodes = SelectedConversation?.GetGraphNodes();
            if (nodes?.TryGetValue(nodeId, out var nodeData) != true)
            {
                return;
            }

            ClearChoiceButtons();

            foreach (var choice in nodeData.choices)
            {
                CreateChoiceButton(choice);
            }
        }

        private void CreateChoiceButton(ChoiceData choice)
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
                label.text = GetChoiceLabel(choice);
            }

            if (img != null)
            {
                img.enabled = true;
            }

            if (btn != null)
            {
                btn.gameObject.SetActive(true);
                btn.interactable = true;
                int targetId = choice.targetNodeId;
                btn.onClick.AddListener(() => _pendingChoiceTarget = targetId);
            }
        }

        private string GetChoiceLabel(ChoiceData choice) =>
            !string.IsNullOrEmpty(choice?.label) ? choice.label
            : !string.IsNullOrEmpty(choice?.choiceText) ? choice.choiceText
            : "Choice";

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
