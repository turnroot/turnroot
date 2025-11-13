using System.Collections;
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

        // Run id used to tag tweens/callbacks for a single conversation run.
        // Incremented each time StartConversation() is called so we can cancel or ignore stale callbacks.
        private int _tweenRunId;

        [SerializeField]
        private SimpleConversation _currentConversation;

        [Header("UI References")]
        [SerializeField]
        private TextMeshProUGUI _dialogueText;

        [SerializeField]
        private TextMeshProUGUI _speakerNameText;

        [SerializeField]
        private Image _speakerPortraitImageActive;

        [SerializeField]
        private Image _speakerPortraitImageInactive;

        [Header("Events")]
        [SerializeField]
        private UnityEvent _onConversationFinished;

        [SerializeField]
        private UnityEvent _onConversationStart;

        [Button("Start Conversation")]
        public void StartConversation()
        {
            if (_currentConversation == null)
            {
                Debug.LogError("Cannot start a null conversation.");
                return;
            }
            // If a conversation is already running, stop it and cancel any tweens
            if (_conversationRoutine != null)
            {
                StopCoroutine(_conversationRoutine);
                _conversationRoutine = null;
            }
            // Kill any tweens for the previous run id (if any)
            if (_tweenRunId != 0)
                DOTween.Kill(_tweenRunId);
            // start a new run id for this conversation
            _tweenRunId++;
            Debug.Log(
                $"ConversationController.StartConversation: start runId={_tweenRunId} (conversation='{_currentConversation?.name ?? "null"}')"
            );

            Graphics2DUtils.ResetImage(_speakerPortraitImageActive);
            Graphics2DUtils.ResetImage(_speakerPortraitImageInactive);

            _onConversationStart?.Invoke();
            _conversationRoutine = StartCoroutine(RunConversation(_currentConversation));
        }

        [Button("Next Layer")]
        public void NextLayer()
        {
            if (_currentConversation?.CurrentLayer != null)
            {
                _currentConversation.CurrentLayer.CompleteLayer();
            }
        }

        public void Proceed()
        {
            NextLayer();
        }

        private IEnumerator RunConversation(SimpleConversation conversation)
        {
            if (conversation == null)
                yield break;

            _currentConversation = conversation;
            // starting conversation

            for (int i = 0; i < conversation.Layers.Length; i++)
            {
                conversation.CurrentLayerIndex = i;
                var layer = conversation.Layers[i];

                if (!layer.HasBeenParsed)
                {
                    layer.ParseDialogue();
                }

                layer.StartLayer();

                // Update UI
                _dialogueText.text = layer.Dialogue;
                // Show the active speaker's name (uses the active slot)
                var activeSlot = layer.GetActiveSlot();
                _speakerNameText.text = !string.IsNullOrWhiteSpace(activeSlot.DisplayName)
                    ? activeSlot.DisplayName
                    : (
                        activeSlot.Speaker != null
                        && !string.IsNullOrWhiteSpace(activeSlot.Speaker.DisplayName)
                            ? activeSlot.Speaker.DisplayName
                            : "???"
                    );

                // assign sprites from the layer's slots but respect the layer's active speaker
                // Active slot should appear in the Active image; the other slot is inactive
                var activeIsPrimary =
                    layer.ActiveSpeaker == ConversationLayer.ActiveSpeakerType.Primary;

                // Determine sprites for primary/secondary slots and for the active portrait
                var primarySprite = layer.PortraitSprite;
                var secondarySprite = layer.SecondaryPortraitSprite;
                var activeSprite = layer.ActivePortrait?.SavedSprite;
                var inactiveSprite = activeIsPrimary ? secondarySprite : primarySprite;

                // Cancel any in-flight portrait tweens/sequences from previous layers (for this run)
                if (_tweenRunId != 0)
                    DOTween.Kill(_tweenRunId);
                // kill any running tweens on the images to avoid stacking
                KillImageTweens(_speakerPortraitImageActive, _speakerPortraitImageInactive);

                var gfxSettings = Graphics2DSettings.Instance;
                var animatePortraits = gfxSettings?.AnimatePortraitTransitions ?? true;
                var portraitDuration = animatePortraits
                    ? (gfxSettings?.PortraitTransitionDuration ?? 0.4f)
                    : 0f;
                var swapCrossfade = gfxSettings?.SwapCrossfade ?? 0.4f;
                var secondaryBehavior =
                    gfxSettings != null
                        ? gfxSettings.SecondaryConversationPortraitInactiveBehavior
                        : SecondaryConversationPortraitInactiveBehavior.Hide;

                Graphics2DUtils.SetSprite(_speakerPortraitImageActive, activeSprite);

                Graphics2DUtils.SetSprite(_speakerPortraitImageInactive, inactiveSprite);
                var willSwap =
                    secondaryBehavior == SecondaryConversationPortraitInactiveBehavior.Swap
                    || secondaryBehavior
                        == SecondaryConversationPortraitInactiveBehavior.TintAndSwap
                    || secondaryBehavior
                        == SecondaryConversationPortraitInactiveBehavior.SwapAndHide;

                Debug.Log(
                    $"[Conversation] Layer {i}: assigning sprites active='{activeSprite?.name ?? "null"}' inactive='{inactiveSprite?.name ?? "null"}' willSwap={willSwap} activeEnabled={_speakerPortraitImageActive.enabled} inactiveEnabled={_speakerPortraitImageInactive.enabled}"
                );

                if (willSwap)
                {
                    // Active/inactive images follow the active speaker (may swap positions)
                    Graphics2DUtils.SetSprite(_speakerPortraitImageActive, activeSprite);

                    Graphics2DUtils.SetSprite(_speakerPortraitImageInactive, inactiveSprite);
                }
                else
                {
                    // Keep sprites fixed by slot: Active image shows primary slot, Inactive shows secondary slot
                    Graphics2DUtils.SetSprite(_speakerPortraitImageActive, primarySprite);

                    Graphics2DUtils.SetSprite(_speakerPortraitImageInactive, secondarySprite);
                }
                var imageForActive = willSwap
                    ? _speakerPortraitImageActive
                    : (
                        activeIsPrimary
                            ? _speakerPortraitImageActive
                            : _speakerPortraitImageInactive
                    );
                var imageForInactive = willSwap
                    ? _speakerPortraitImageInactive
                    : (
                        activeIsPrimary
                            ? _speakerPortraitImageInactive
                            : _speakerPortraitImageActive
                    );
                // When enabling images, ensure alpha is reset (previous Hide fades may leave alpha at 0)
                if (_speakerPortraitImageActive.enabled)
                {
                    var c = _speakerPortraitImageActive.color;
                    c.a = 1f;
                    _speakerPortraitImageActive.color = Color.white; // reset any previous tint
                }
                if (_speakerPortraitImageInactive.enabled)
                {
                    var c2 = _speakerPortraitImageInactive.color;
                    c2.a = 1f;
                    _speakerPortraitImageInactive.color = Color.white; // reset any previous tint
                }

                if (activeSprite == null)
                {
                    Debug.LogWarning(
                        $"No portrait found for active speaker '{layer.SpeakerDisplayName}' in layer {i} of conversation '{conversation.name}'"
                    );
                }
                else
                {
                    // tints from the layer (already mix color, not alpha)
                    var primaryTint = layer.PrimaryPortraitTint;
                    var secondaryTint = layer.SecondaryPortraitTint;

                    // Determine which slot is active to pick target colors
                    var targetActiveColor = activeIsPrimary ? primaryTint : secondaryTint;
                    var targetInactiveColor = activeIsPrimary ? secondaryTint : primaryTint;

                    // apply behavior for inactive portrait

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
                                        // instant swap
                                        var t = _speakerPortraitImageActive.sprite;
                                        _speakerPortraitImageActive.sprite =
                                            _speakerPortraitImageInactive.sprite;
                                        _speakerPortraitImageInactive.sprite = t;
                                        Debug.Log(
                                            $"TintAndSwap: performed immediate swap run={runId}"
                                        );
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
                                        // instant swap
                                        var t = _speakerPortraitImageActive.sprite;
                                        _speakerPortraitImageActive.sprite =
                                            _speakerPortraitImageInactive.sprite;
                                        _speakerPortraitImageInactive.sprite = t;
                                        Debug.Log(
                                            $"SwapAndHide: performed immediate swap run={runId}"
                                        );
                                    })
                                    .Append(
                                        CreateHideTween(
                                            _speakerPortraitImageInactive,
                                            portraitDuration
                                        )
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

                bool completed = false;
                void onComplete() => completed = true;
                layer.OnLayerComplete.AddListener(onComplete);

                yield return new WaitUntil(() => completed);

                layer.OnLayerComplete.RemoveListener(onComplete);
            }
            // Current conversation finished — fire finished event before any auto-advance
            _onConversationFinished?.Invoke();
            // conversation completed

            // clear running coroutine handle
            _conversationRoutine = null;
        }

        // --- Portrait animation helpers ---
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
