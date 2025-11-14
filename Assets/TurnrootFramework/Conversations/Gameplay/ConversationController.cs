using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using NaughtyAttributes;
using TMPro;
using Turnroot.AbstractScripts.Graphics2D;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using XNode;

namespace Turnroot.Conversations
{
    public class ConversationController : MonoBehaviour
    {
        private Coroutine _conversationRoutine;
        private int _tweenRunId;

        [SerializeField]
        private Conversation _currentConversation;

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

        private int _pendingChoiceTarget = int.MinValue;
        private int _activeBranchingNodeId = int.MinValue;
        private ConversationLayer _activeBranchingLayer;

        // Track last active sprite to skip animations when the same speaker speaks consecutively
        private Sprite _lastActiveSprite;

        public void Advance() => NextLayer();

        public bool ChooseBranchTarget(int targetNodeId)
        {
            if (_currentConversation == null || !_currentConversation.BranchingConversation)
                return false;
            _pendingChoiceTarget = targetNodeId;
            ClearChoiceButtons();
            return true;
        }

        public List<ChoiceData> GetCurrentChoices()
        {
            if (_currentConversation == null || !_currentConversation.BranchingConversation)
                return null;
            var nodes = _currentConversation.GetGraphNodes();
            if (nodes == null)
                return null;
            if (_activeBranchingNodeId == int.MinValue)
                return null;
            if (!nodes.TryGetValue(_activeBranchingNodeId, out var nd) || nd == null)
                return null;
            return nd.choices;
        }

        [Header("Events")]
        [SerializeField]
        private UnityEvent _onConversationFinished;

        [SerializeField]
        private UnityEvent _onConversationStart;

        [Button("Start Conversation")]
        public void StartConversation()
        {
            if (!Application.isPlaying)
            {
                Debug.LogError(
                    "StartConversation must be run in Play Mode. Aborting to avoid editor-state changes."
                );
                return;
            }

            if (_currentConversation == null)
            {
                Debug.LogError("Cannot start a null conversation.");
                return;
            }

            if (
                _currentConversation.BranchingConversation
                && _currentConversation.ConversationGraph == null
            )
            {
                Debug.LogError(
                    $"Conversation '{_currentConversation.name}' is marked as branching but has no ConversationGraph assigned. Aborting."
                );
                if (_dialogueText != null)
                    _dialogueText.text = string.Empty;
                if (_speakerNameText != null)
                    _speakerNameText.text = string.Empty;
                ClearChoiceButtons();
                Graphics2DUtils.ResetImage(_speakerPortraitImageActive);
                Graphics2DUtils.ResetImage(_speakerPortraitImageInactive);
                return;
            }

            if (_conversationRoutine != null)
            {
                StopCoroutine(_conversationRoutine);
                _conversationRoutine = null;
            }

            if (_tweenRunId != 0)
                DOTween.Kill(_tweenRunId);
            _tweenRunId++;

            Graphics2DUtils.ResetImage(_speakerPortraitImageActive);
            Graphics2DUtils.ResetImage(_speakerPortraitImageInactive);
            // reset last active sprite when starting a conversation
            _lastActiveSprite = null;

            _onConversationStart?.Invoke();
            _conversationRoutine = StartCoroutine(RunConversation(_currentConversation));
        }

        [Button("Next Layer")]
        public void NextLayer()
        {
            if (_activeBranchingLayer != null)
            {
                Debug.Log("ConversationController.NextLayer: completing active branching layer");
                _activeBranchingLayer.CompleteLayer();
                return;
            }

            if (_currentConversation?.CurrentLayer != null)
            {
                Debug.Log("ConversationController.NextLayer: completing linear current layer");
                _currentConversation.CurrentLayer.CompleteLayer();
            }
        }

        public void Proceed() => NextLayer();

        private IEnumerator RunConversation(Conversation conversation)
        {
            if (conversation == null)
                yield break;
            _currentConversation = conversation;

            // Branching flow
            if (conversation.BranchingConversation)
            {
                var nodes = conversation.GetGraphNodes();
                if (nodes == null || nodes.Count == 0)
                {
                    Debug.LogError(
                        $"Branching conversation '{conversation.name}' has no nodes or graph is empty. Nothing to show."
                    );
                    if (_dialogueText != null)
                        _dialogueText.text = string.Empty;
                    if (_speakerNameText != null)
                        _speakerNameText.text = string.Empty;
                    ClearChoiceButtons();
                    Graphics2DUtils.ResetImage(_speakerPortraitImageActive);
                    Graphics2DUtils.ResetImage(_speakerPortraitImageInactive);
                    yield break;
                }

                int currentNodeId = int.MinValue;
                foreach (var kv in nodes)
                {
                    var nd = kv.Value;
                    if (nd == null || nd.node == null)
                        continue;
                    if (nd.incomingCount == 0)
                    {
                        currentNodeId = kv.Key;
                        break;
                    }
                }

                if (currentNodeId == int.MinValue)
                {
                    foreach (var kv in nodes)
                    {
                        currentNodeId = kv.Key;
                        break;
                    }
                }

                while (currentNodeId != int.MinValue)
                {
                    if (!nodes.TryGetValue(currentNodeId, out var nodeData) || nodeData == null)
                        break;
                    _activeBranchingNodeId = currentNodeId;

                    var layer = nodeData.conversationLayer;
                    if (layer != null)
                    {
                        if (!layer.HasBeenParsed)
                            layer.ParseDialogue();
                        layer.StartLayer();
                        _activeBranchingLayer = layer;

                        // Update UI
                        if (_dialogueText != null)
                            _dialogueText.text = layer.Dialogue;
                        var activeSlot = layer.GetActiveSlot();
                        _speakerNameText.text = !string.IsNullOrWhiteSpace(activeSlot.DisplayName)
                            ? activeSlot.DisplayName
                            : (
                                activeSlot.Speaker != null
                                && !string.IsNullOrWhiteSpace(activeSlot.Speaker.DisplayName)
                                    ? activeSlot.Speaker.DisplayName
                                    : "???"
                            );

                        // apply portrait transitions, but skip if same active sprite as last time
                        var currentActiveSprite = layer.ActivePortrait?.SavedSprite;
                        if (_lastActiveSprite != currentActiveSprite)
                        {
                            ApplyPortraitForLayer(layer);
                            _lastActiveSprite = currentActiveSprite;
                        }

                        // wait for layer completion
                        bool completed = false;
                        void onComplete() => completed = true;
                        layer.OnLayerComplete.AddListener(onComplete);
                        yield return new WaitUntil(() => completed);
                        layer.OnLayerComplete.RemoveListener(onComplete);

                        _activeBranchingLayer = null;
                    }

                    // choices
                    if (nodeData.choices != null && nodeData.choices.Count > 0)
                    {
                        _pendingChoiceTarget = int.MinValue;
                        ShowChoicesForNode(currentNodeId);
                        yield return new WaitUntil(() => _pendingChoiceTarget != int.MinValue);
                        currentNodeId = _pendingChoiceTarget;
                        ClearChoiceButtons();
                        continue;
                    }

                    if (nodeData.nextTargetId != int.MinValue)
                    {
                        currentNodeId = nodeData.nextTargetId;
                        continue;
                    }

                    break;
                }

                _activeBranchingNodeId = int.MinValue;
                _activeBranchingLayer = null;
                _pendingChoiceTarget = int.MinValue;
                _onConversationFinished?.Invoke();
                _conversationRoutine = null;
                yield break;
            }

            // Linear flow
            for (int i = 0; i < conversation.Layers.Length; i++)
            {
                conversation.CurrentLayerIndex = i;
                var layer = conversation.Layers[i];

                if (!layer.HasBeenParsed)
                    layer.ParseDialogue();
                layer.StartLayer();

                // Update UI
                if (_dialogueText != null)
                    _dialogueText.text = layer.Dialogue;
                var activeSlot = layer.GetActiveSlot();
                _speakerNameText.text = !string.IsNullOrWhiteSpace(activeSlot.DisplayName)
                    ? activeSlot.DisplayName
                    : (
                        activeSlot.Speaker != null
                        && !string.IsNullOrWhiteSpace(activeSlot.Speaker.DisplayName)
                            ? activeSlot.Speaker.DisplayName
                            : "???"
                    );

                // apply portrait transitions
                // apply portrait transitions, but skip if same active sprite as last time
                var currentActiveSprite = layer.ActivePortrait?.SavedSprite;
                if (_lastActiveSprite != currentActiveSprite)
                {
                    ApplyPortraitForLayer(layer);
                    _lastActiveSprite = currentActiveSprite;
                }

                // wait for completion
                bool completed = false;
                void onComplete() => completed = true;
                layer.OnLayerComplete.AddListener(onComplete);
                yield return new WaitUntil(() => completed);
                layer.OnLayerComplete.RemoveListener(onComplete);
            }

            _onConversationFinished?.Invoke();
            _conversationRoutine = null;
        }

        // --- Portrait animation helpers ---
        private void ApplyPortraitForLayer(ConversationLayer layer)
        {
            if (layer == null)
                return;

            var activeIsPrimary =
                layer.ActiveSpeaker == ConversationLayer.ActiveSpeakerType.Primary;
            var primarySprite = layer.PortraitSprite;
            var secondarySprite = layer.SecondaryPortraitSprite;
            var activeSprite = layer.ActivePortrait?.SavedSprite;
            var inactiveSprite = activeIsPrimary ? secondarySprite : primarySprite;

            if (_tweenRunId != 0)
                DOTween.Kill(_tweenRunId);
            KillImageTweens(_speakerPortraitImageActive, _speakerPortraitImageInactive);

            var gfxSettings = Graphics2DSettings.Instance;
            var animatePortraits = gfxSettings?.AnimatePortraitTransitions ?? true;
            var portraitDuration = animatePortraits
                ? (gfxSettings?.PortraitTransitionDuration ?? 0.4f)
                : 0f;
            var secondaryBehavior =
                gfxSettings != null
                    ? gfxSettings.SecondaryConversationPortraitInactiveBehavior
                    : SecondaryConversationPortraitInactiveBehavior.Hide;

            Graphics2DUtils.SetSprite(_speakerPortraitImageActive, activeSprite);
            Graphics2DUtils.SetSprite(_speakerPortraitImageInactive, inactiveSprite);

            var willSwap =
                secondaryBehavior == SecondaryConversationPortraitInactiveBehavior.Swap
                || secondaryBehavior == SecondaryConversationPortraitInactiveBehavior.TintAndSwap
                || secondaryBehavior == SecondaryConversationPortraitInactiveBehavior.SwapAndHide;

            if (willSwap)
            {
                Graphics2DUtils.SetSprite(_speakerPortraitImageActive, activeSprite);
                Graphics2DUtils.SetSprite(_speakerPortraitImageInactive, inactiveSprite);
            }
            else
            {
                Graphics2DUtils.SetSprite(_speakerPortraitImageActive, primarySprite);
                Graphics2DUtils.SetSprite(_speakerPortraitImageInactive, secondarySprite);
            }

            var imageForActive = willSwap
                ? _speakerPortraitImageActive
                : (activeIsPrimary ? _speakerPortraitImageActive : _speakerPortraitImageInactive);
            var imageForInactive = willSwap
                ? _speakerPortraitImageInactive
                : (activeIsPrimary ? _speakerPortraitImageInactive : _speakerPortraitImageActive);

            if (_speakerPortraitImageActive.enabled)
                _speakerPortraitImageActive.color = Color.white;
            if (_speakerPortraitImageInactive.enabled)
                _speakerPortraitImageInactive.color = Color.white;

            if (activeSprite != null)
            {
                var primaryTint = layer.PrimaryPortraitTint;
                var secondaryTint = layer.SecondaryPortraitTint;

                var targetActiveColor = activeIsPrimary ? primaryTint : secondaryTint;
                var targetInactiveColor = activeIsPrimary ? secondaryTint : primaryTint;

                switch (secondaryBehavior)
                {
                    case SecondaryConversationPortraitInactiveBehavior.Hide:
                        CreateHideTween(imageForInactive, portraitDuration).Play();
                        break;
                    case SecondaryConversationPortraitInactiveBehavior.Tint:
                        CreateTintSequence(
                                imageForActive,
                                imageForInactive,
                                targetActiveColor,
                                targetInactiveColor,
                                portraitDuration
                            )
                            .Play();
                        break;
                    case SecondaryConversationPortraitInactiveBehavior.Swap:
                        CreateSwapSequence(
                                _speakerPortraitImageActive,
                                _speakerPortraitImageInactive,
                                portraitDuration
                            )
                            .Play();
                        break;
                    case SecondaryConversationPortraitInactiveBehavior.TintAndSwap:
                        {
                            var runId = _tweenRunId;
                            DOTween
                                .Sequence()
                                .AppendCallback(() =>
                                {
                                    if (runId != _tweenRunId)
                                        return;
                                    var t = _speakerPortraitImageActive.sprite;
                                    _speakerPortraitImageActive.sprite =
                                        _speakerPortraitImageInactive.sprite;
                                    _speakerPortraitImageInactive.sprite = t;
                                })
                                .Append(
                                    CreateTintSequence(
                                        imageForActive,
                                        imageForInactive,
                                        targetActiveColor,
                                        targetInactiveColor,
                                        portraitDuration
                                    )
                                )
                                .SetId(_tweenRunId)
                                .Play();
                        }
                        break;
                    case SecondaryConversationPortraitInactiveBehavior.SwapAndHide:
                        {
                            var runId = _tweenRunId;
                            DOTween
                                .Sequence()
                                .AppendCallback(() =>
                                {
                                    if (runId != _tweenRunId)
                                        return;
                                    var t = _speakerPortraitImageActive.sprite;
                                    _speakerPortraitImageActive.sprite =
                                        _speakerPortraitImageInactive.sprite;
                                    _speakerPortraitImageInactive.sprite = t;
                                })
                                .Append(
                                    CreateHideTween(_speakerPortraitImageInactive, portraitDuration)
                                )
                                .SetId(_tweenRunId)
                                .Play();
                        }
                        break;
                    case SecondaryConversationPortraitInactiveBehavior.None:
                        CreateTintSequence(
                                imageForActive,
                                imageForInactive,
                                Color.white,
                                Color.white,
                                portraitDuration
                            )
                            .Play();
                        break;
                }
            }
        }

        private void KillImageTweens(params Image[] images)
        {
            var animate = Graphics2DSettings.Instance?.AnimatePortraitTransitions ?? true;
            if (!animate)
                return;
            Graphics2DUtils.KillImageTweens(images);
        }

        private void OnDisable()
        {
            if (_tweenRunId != 0)
                DOTween.Kill(_tweenRunId);
            if (_conversationRoutine != null)
            {
                StopCoroutine(_conversationRoutine);
                _conversationRoutine = null;
            }
        }

        private void OnDestroy()
        {
            if (_tweenRunId != 0)
                DOTween.Kill(_tweenRunId);
        }

        private void ClearChoiceButtons()
        {
            if (_choiceButtonsContainer == null)
                return;
            for (int i = _choiceButtonsContainer.childCount - 1; i >= 0; i--)
            {
                var c = _choiceButtonsContainer.GetChild(i);
                if (c != null)
                    Destroy(c.gameObject);
            }
        }

        private void ShowChoicesForNode(int nodeId)
        {
            if (_choiceButtonPrefab == null || _choiceButtonsContainer == null)
            {
                Debug.LogWarning("Choice button prefab or container not assigned.");
                return;
            }

            var nodes = _currentConversation?.GetGraphNodes();
            if (nodes == null || !nodes.TryGetValue(nodeId, out var nodeData) || nodeData == null)
                return;

            ClearChoiceButtons();

            foreach (var c in nodeData.choices)
            {
                var go = Instantiate(_choiceButtonPrefab, _choiceButtonsContainer);
                if (go == null)
                    continue;
                if (!go.activeSelf)
                    go.SetActive(true);

                var btn = go.GetComponent<Button>();
                var img = go.GetComponent<Image>();
                var label = go.GetComponentInChildren<TextMeshProUGUI>(true);

                var labelText = ResolveChoiceLabel(c) ?? "Choice";
                if (label != null)
                {
                    if (!label.gameObject.activeSelf)
                        label.gameObject.SetActive(true);
                    label.text = labelText;
                }

                if (img != null)
                    img.enabled = true;
                if (btn != null)
                {
                    if (!btn.gameObject.activeSelf)
                        btn.gameObject.SetActive(true);
                    btn.interactable = true;
                    int targetId = c.targetNodeId;
                    btn.onClick.AddListener(() => OnChoiceClicked(targetId));
                }
            }
        }

        private void OnChoiceClicked(int targetNodeId)
        {
            var targetName = "(unknown)";
            var nodes = _currentConversation?.GetGraphNodes();
            if (nodes != null && nodes.TryGetValue(targetNodeId, out var nd) && nd != null)
                targetName = nd.name;
            Debug.Log(
                $"[ConversationController] Choice selected -> targetId={targetNodeId} name={targetName}"
            );
            _pendingChoiceTarget = targetNodeId;
        }

        private string ResolveChoiceLabel(ChoiceData c)
        {
            if (c == null)
                return "Choice";
            if (!string.IsNullOrEmpty(c.label))
                return c.label;
            if (!string.IsNullOrEmpty(c.choiceText))
                return c.choiceText;
            return "Choice";
        }

        private Tween CreateTintSequence(
            Image activeImg,
            Image inactiveImg,
            Color activeColor,
            Color inactiveColor,
            float duration
        )
        {
            var ease =
                Graphics2DSettings.Instance?.PortraitTransitionEase ?? DG.Tweening.Ease.OutCubic;
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
            var swapCross = Graphics2DSettings.Instance?.SwapCrossfade ?? 0.4f;
            var ease =
                Graphics2DSettings.Instance?.PortraitTransitionEase ?? DG.Tweening.Ease.OutCubic;
            return Graphics2DUtils.CrossfadeSwap(a, b, swapCross, ease, _tweenRunId);
        }

        private Tween CreateHideTween(Image img, float duration)
        {
            var ease =
                Graphics2DSettings.Instance?.PortraitTransitionEase ?? DG.Tweening.Ease.OutCubic;
            return Graphics2DUtils.CreateHideTween(img, duration, ease, _tweenRunId);
        }
    }
}
