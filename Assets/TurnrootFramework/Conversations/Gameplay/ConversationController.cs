using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using NaughtyAttributes;
using TMPro;
using Turnroot.AbstractScripts.Graphics2D;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Turnroot.Conversations
{
    public class ConversationController : MonoBehaviour
    {
        private Coroutine _conversationRoutine;
        private int _tweenRunId;
        private Sprite _lastActiveSprite;
        private int _pendingChoiceTarget = int.MinValue;
        private int _activeBranchingNodeId = int.MinValue;
        private ConversationLayer _activeBranchingLayer;
        private ConversationInstance _runningInstance;

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

        [Header("Controller Events")]
        public UnityEvent OnAwake;
        public UnityEvent OnAnyConversationStart;
        public UnityEvent OnAnyConversationFinished;

        private ConversationInstance SelectedInstance =>
            _conversationInstances != null
            && _currentConversation >= 0
            && _currentConversation < _conversationInstances.Count
                ? _conversationInstances[_currentConversation]
                : null;

        private void Awake() => OnAwake?.Invoke();

        private Conversation SelectedConversation => SelectedInstance?.Conversation;

        // Backwards-compatible helper used by unit tests via reflection.
        private ConversationInstance SelectedConversationInstance => SelectedInstance;

        // Backwards-compatible helper used by unit tests via reflection.
        private string[] GetConversationInstanceNames()
        {
            if (_conversationInstances == null)
            {
                return new string[0];
            }

            var names = new List<string>();
            foreach (var inst in _conversationInstances)
            {
                if (inst == null)
                {
                    names.Add("(null)");
                    continue;
                }
                var c = inst.Conversation;
                names.Add(c != null ? c.name : "(no conversation)");
            }
            return names.ToArray();
        }

        private Graphics2DSettings GfxSettings => Graphics2DSettings.Instance;

        public void Advance() => NextLayer();

        public void Proceed() => NextLayer();

        public bool ChooseBranchTarget(int targetNodeId)
        {
            if (SelectedConversation?.BranchingConversation != true)
            {
                return false;
            }

            _pendingChoiceTarget = targetNodeId;
            ClearChoiceButtons();
            return true;
        }

        public List<ChoiceData> GetCurrentChoices()
        {
            if (
                SelectedConversation?.BranchingConversation != true
                || _activeBranchingNodeId == int.MinValue
            )
            {
                return null;
            }

            var nodes = SelectedConversation.GetGraphNodes();
            return nodes?.TryGetValue(_activeBranchingNodeId, out var node) == true
                ? node.choices
                : null;
        }

        [Button("Start Conversation")]
        public void StartConversation()
        {
            if (!ValidateConversationStart())
            {
                return;
            }

            CleanupPreviousConversation();
            ResetUI();

            var instance = SelectedInstance;
            SelectedConversation?.StartConversation();
            SelectedConversation?.OnConversationStart?.Invoke();
            instance?.OnConversationStart?.Invoke();
            OnAnyConversationStart?.Invoke();

            _runningInstance = instance;
            _conversationRoutine = StartCoroutine(RunConversation(instance));
        }

        [Button("Next Layer")]
        public void NextLayer()
        {
            if (_activeBranchingLayer != null)
            {
                _activeBranchingLayer.CompleteLayer();
                return;
            }
            SelectedConversation?.CurrentLayer?.CompleteLayer();
        }

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
            Debug.LogError(message);
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
                Debug.LogError($"Branching conversation '{conversation.name}' has no nodes.");
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

        private void UpdateUIForLayer(ConversationLayer layer)
        {
            if (_dialogueText != null)
            {
                _dialogueText.text = layer.Dialogue;
            }

            var activeSlot = layer.GetActiveSlot();
            if (_speakerNameText != null)
            {
                _speakerNameText.text = GetSpeakerName(activeSlot);
            }

            var currentActiveSprite = layer.ActivePortrait?.SavedSprite;
            if (_lastActiveSprite != currentActiveSprite)
            {
                ApplyPortraitForLayer(layer);
                _lastActiveSprite = currentActiveSprite;
            }
        }

        private string GetSpeakerName(ConversationLayer.SpeakerSlot slot)
        {
            return !string.IsNullOrWhiteSpace(slot.DisplayName) ? slot.DisplayName
                : slot.Speaker != null && !string.IsNullOrWhiteSpace(slot.Speaker.DisplayName)
                    ? slot.Speaker.DisplayName
                : "???";
        }

        private void ApplyPortraitForLayer(ConversationLayer layer)
        {
            if (layer == null)
            {
                return;
            }

            var activeIsPrimary =
                layer.ActiveSpeaker == ConversationLayer.ActiveSpeakerType.Primary;
            var activeSprite = layer.ActivePortrait?.SavedSprite;
            var inactiveSprite = activeIsPrimary
                ? layer.SecondaryPortraitSprite
                : layer.PortraitSprite;

            if (_tweenRunId != 0)
            {
                DOTween.Kill(_tweenRunId);
            }

            KillImageTweens(_speakerPortraitImageActive, _speakerPortraitImageInactive);

            var animatePortraits = GfxSettings?.AnimatePortraitTransitions ?? true;
            var duration = animatePortraits
                ? (GfxSettings?.PortraitTransitionDuration ?? 0.4f)
                : 0f;
            var behavior =
                GfxSettings?.SecondaryConversationPortraitInactiveBehavior
                ?? SecondaryConversationPortraitInactiveBehavior.Hide;

            SetupPortraitImages(layer, behavior);
            var (activeImg, inactiveImg) = GetPortraitImages(activeIsPrimary, behavior);

            ResetPortraitColors();

            if (activeSprite != null)
            {
                ApplyPortraitBehavior(
                    layer,
                    activeIsPrimary,
                    activeImg,
                    inactiveImg,
                    behavior,
                    duration
                );
            }
        }

        private static bool WillSwapForBehavior(
            SecondaryConversationPortraitInactiveBehavior behavior
        ) =>
            behavior
                is SecondaryConversationPortraitInactiveBehavior.Swap
                    or SecondaryConversationPortraitInactiveBehavior.TintAndSwap
                    or SecondaryConversationPortraitInactiveBehavior.SwapAndHide;

        private void SetupPortraitImages(
            ConversationLayer layer,
            SecondaryConversationPortraitInactiveBehavior behavior
        )
        {
            var activeIsPrimary =
                layer.ActiveSpeaker == ConversationLayer.ActiveSpeakerType.Primary;
            var activeSprite = layer.ActivePortrait?.SavedSprite;
            var inactiveSprite = activeIsPrimary
                ? layer.SecondaryPortraitSprite
                : layer.PortraitSprite;

            if (WillSwapForBehavior(behavior))
            {
                Graphics2DUtils.SetSprite(_speakerPortraitImageActive, activeSprite);
                Graphics2DUtils.SetSprite(_speakerPortraitImageInactive, inactiveSprite);
            }
            else
            {
                Graphics2DUtils.SetSprite(_speakerPortraitImageActive, layer.PortraitSprite);
                Graphics2DUtils.SetSprite(
                    _speakerPortraitImageInactive,
                    layer.SecondaryPortraitSprite
                );
            }
        }

        private (Image active, Image inactive) GetPortraitImages(
            bool activeIsPrimary,
            SecondaryConversationPortraitInactiveBehavior behavior
        )
        {
            var willSwap = WillSwapForBehavior(behavior);

            var active = willSwap
                ? _speakerPortraitImageActive
                : (activeIsPrimary ? _speakerPortraitImageActive : _speakerPortraitImageInactive);
            var inactive = willSwap
                ? _speakerPortraitImageInactive
                : (activeIsPrimary ? _speakerPortraitImageInactive : _speakerPortraitImageActive);

            return (active, inactive);
        }

        private void ResetPortraitColors()
        {
            if (_speakerPortraitImageActive.enabled)
            {
                _speakerPortraitImageActive.color = Color.white;
            }

            if (_speakerPortraitImageInactive.enabled)
            {
                _speakerPortraitImageInactive.color = Color.white;
            }
        }

        private void ApplyPortraitBehavior(
            ConversationLayer layer,
            bool activeIsPrimary,
            Image activeImg,
            Image inactiveImg,
            SecondaryConversationPortraitInactiveBehavior behavior,
            float duration
        )
        {
            var targetActiveColor = activeIsPrimary
                ? layer.PrimaryPortraitTint
                : layer.SecondaryPortraitTint;
            var targetInactiveColor = activeIsPrimary
                ? layer.SecondaryPortraitTint
                : layer.PrimaryPortraitTint;

            switch (behavior)
            {
                case SecondaryConversationPortraitInactiveBehavior.Hide:
                    CreateHideTween(inactiveImg, duration).Play();
                    break;
                case SecondaryConversationPortraitInactiveBehavior.Tint:
                    CreateTintSequence(
                            activeImg,
                            inactiveImg,
                            targetActiveColor,
                            targetInactiveColor,
                            duration
                        )
                        .Play();
                    break;
                case SecondaryConversationPortraitInactiveBehavior.Swap:
                    CreateSwapSequence(
                            _speakerPortraitImageActive,
                            _speakerPortraitImageInactive,
                            duration
                        )
                        .Play();
                    break;
                case SecondaryConversationPortraitInactiveBehavior.TintAndSwap:
                    CreateTintAndSwapSequence(
                        activeImg,
                        inactiveImg,
                        targetActiveColor,
                        targetInactiveColor,
                        duration
                    );
                    break;
                case SecondaryConversationPortraitInactiveBehavior.SwapAndHide:
                    CreateSwapAndHideSequence(duration);
                    break;
                case SecondaryConversationPortraitInactiveBehavior.None:
                    CreateTintSequence(activeImg, inactiveImg, Color.white, Color.white, duration)
                        .Play();
                    break;
            }
        }

        private void CreateSwapSequenceWithFollowUp(Tween followUp)
        {
            var runId = _tweenRunId;
            DOTween
                .Sequence()
                .AppendCallback(() =>
                {
                    if (runId != _tweenRunId)
                    {
                        return;
                    }

                    (_speakerPortraitImageActive.sprite, _speakerPortraitImageInactive.sprite) = (
                        _speakerPortraitImageInactive.sprite,
                        _speakerPortraitImageActive.sprite
                    );
                })
                .Append(followUp)
                .SetId(_tweenRunId)
                .Play();
        }

        private void CreateTintAndSwapSequence(
            Image activeImg,
            Image inactiveImg,
            Color activeColor,
            Color inactiveColor,
            float duration
        ) =>
            CreateSwapSequenceWithFollowUp(
                CreateTintSequence(activeImg, inactiveImg, activeColor, inactiveColor, duration)
            );

        private void CreateSwapAndHideSequence(float duration) =>
            CreateSwapSequenceWithFollowUp(
                CreateHideTween(_speakerPortraitImageInactive, duration)
            );

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

        private void KillImageTweens(params Image[] images)
        {
            if (GfxSettings?.AnimatePortraitTransitions ?? true)
            {
                Graphics2DUtils.KillImageTweens(images);
            }
        }

        private Tween CreateTintSequence(
            Image activeImg,
            Image inactiveImg,
            Color activeColor,
            Color inactiveColor,
            float duration
        )
        {
            var ease = GfxSettings?.PortraitTransitionEase ?? Ease.OutCubic;
            return Graphics2DUtils.CreateTintSequence(
                activeImg,
                inactiveImg,
                activeColor,
                inactiveColor,
                duration,
                ease,
                _tweenRunId
            );
        }

        private Tween CreateSwapSequence(Image a, Image b, float duration)
        {
            var swapCross = GfxSettings?.SwapCrossfade ?? 0.4f;
            var ease = GfxSettings?.PortraitTransitionEase ?? Ease.OutCubic;
            return Graphics2DUtils.CrossfadeSwap(a, b, swapCross, ease, _tweenRunId);
        }

        private Tween CreateHideTween(Image img, float duration)
        {
            var ease = GfxSettings?.PortraitTransitionEase ?? Ease.OutCubic;
            return Graphics2DUtils.CreateHideTween(img, duration, ease, _tweenRunId);
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
