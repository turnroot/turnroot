using System.Collections;
using System.Collections.Generic;
using TMPro;
using Turnroot.AbstractScripts.Graphics2D;
using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.UI;
using Ease = Turnroot.AbstractScripts.Graphics2D.Graphics2DUtils.Ease;

namespace Turnroot.Conversations
{
    /// <summary>
    /// Partial class handling UI updates, portrait management, and choice button rendering for the conversation controller.
    /// </summary>
    public partial class ConversationController : MonoBehaviour
    {
        private void UpdateUIForLayer(ConversationLayer layer)
        {
            if (layer == null)
            {
                "ConversationControllerUI: UpdateUIForLayer called with null layer.".LogWarning();
                return;
            }

            if (_dialogueText != null)
            {
                _dialogueText.text = layer.Dialogue;
            }
            else
            {
                "ConversationControllerUI: _dialogueText is not assigned.".LogWarning();
            }

            var activeSlot = layer.GetActiveSlot();
            if (activeSlot == null)
            {
                "ConversationControllerUI: Layer has no active speaker slot; skipping speaker name update.".LogWarning();
            }

            if (_speakerNameText != null)
            {
                _speakerNameText.text = GetSpeakerName(activeSlot);
            }
            else
            {
                "ConversationControllerUI: _speakerNameText is not assigned.".LogWarning();
            }

            var currentActiveSprite = layer.ActivePortrait?.SavedSprite ?? layer.PortraitSprite;
            if (_lastActiveSprite != currentActiveSprite)
            {
                $"ConversationControllerUI: Setting portrait sprite to {currentActiveSprite?.name ?? "null"}.".LogInfo();
                ApplyPortraitForLayer(layer);
                _lastActiveSprite = currentActiveSprite;
            }
        }

        private string GetSpeakerName(ConversationLayer.SpeakerSlot slot)
        {
            return slot == null
                ? "???"
                : !string.IsNullOrWhiteSpace(slot.DisplayName) ? slot.DisplayName
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

            if (_speakerPortraitImageActive == null)
            {
                "ConversationControllerUI: _speakerPortraitImageActive is not assigned; cannot apply portrait.".LogWarning();
                return;
            }

            var activeIsPrimary =
                layer.ActiveSpeaker == ConversationLayer.ActiveSpeakerType.Primary;
            var activeSprite = layer.ActivePortrait?.SavedSprite ?? layer.PortraitSprite;
            var inactiveSprite = activeIsPrimary
                ? layer.SecondaryPortraitSprite
                : layer.PortraitSprite;

            if (_tweenRunId != 0)
            {
                CancelActiveTweens();
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
            var activeSprite = layer.ActivePortrait?.SavedSprite ?? layer.PortraitSprite;
            var inactiveSprite = activeIsPrimary
                ? layer.SecondaryPortraitSprite
                : layer.PortraitSprite;

            if (WillSwapForBehavior(behavior))
            {
                if (_speakerPortraitImageActive != null)
                {
                    Graphics2DUtils.SetSprite(_speakerPortraitImageActive, activeSprite);
                }

                if (_speakerPortraitImageInactive != null)
                {
                    Graphics2DUtils.SetSprite(_speakerPortraitImageInactive, inactiveSprite);
                }
            }
            else
            {
                if (_speakerPortraitImageActive != null)
                {
                    Graphics2DUtils.SetSprite(_speakerPortraitImageActive, layer.PortraitSprite);
                }

                if (_speakerPortraitImageInactive != null)
                {
                    Graphics2DUtils.SetSprite(
                        _speakerPortraitImageInactive,
                        layer.SecondaryPortraitSprite
                    );
                }
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

            // If only one portrait image is assigned, fall back to it for both roles.
            if (active == null && inactive != null)
            {
                active = inactive;
            }

            if (inactive == null && active != null)
            {
                inactive = active;
            }

            return (active, inactive);
        }

        private void ResetPortraitColors()
        {
            if (_speakerPortraitImageActive != null && _speakerPortraitImageActive.enabled)
            {
                _speakerPortraitImageActive.color = Color.white;
            }
            else if (_speakerPortraitImageActive == null)
            {
                "ConversationControllerUI: _speakerPortraitImageActive is not assigned.".LogWarning();
            }

            if (_speakerPortraitImageInactive != null && _speakerPortraitImageInactive.enabled)
            {
                _speakerPortraitImageInactive.color = Color.white;
            }
            else if (_speakerPortraitImageInactive == null)
            {
                "ConversationControllerUI: _speakerPortraitImageInactive is not assigned (this is valid for single‑portrait setups).".LogInfo();
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
            if (activeImg == null)
            {
                return;
            }

            // If only one portrait image is assigned, use it for both roles so we don't accidentally
            // attempt to operate on a missing object.
            if (inactiveImg == null)
            {
                inactiveImg = activeImg;
            }

            var targetActiveColor = activeIsPrimary
                ? layer.PrimaryPortraitTint
                : layer.SecondaryPortraitTint;
            var targetInactiveColor = activeIsPrimary
                ? layer.SecondaryPortraitTint
                : layer.PrimaryPortraitTint;

            var ease = GfxSettings?.PortraitTransitionEase ?? Ease.OutCubic;

            // When only a single portrait image is present, behaviors like swapping or hiding
            // don't make sense since there is no second image to act upon.
            var effectiveBehavior = behavior;
            if (
                inactiveImg == activeImg
                && behavior != SecondaryConversationPortraitInactiveBehavior.None
            )
            {
                effectiveBehavior = SecondaryConversationPortraitInactiveBehavior.None;
            }

            switch (effectiveBehavior)
            {
                case SecondaryConversationPortraitInactiveBehavior.Hide:
                    StartTween(
                        Graphics2DUtils.HideCoroutine(inactiveImg, duration, ease, _tweenRunId)
                    );
                    break;
                case SecondaryConversationPortraitInactiveBehavior.Tint:
                    StartTween(
                        Graphics2DUtils.TintCoroutine(
                            activeImg,
                            inactiveImg,
                            targetActiveColor,
                            targetInactiveColor,
                            duration,
                            ease,
                            _tweenRunId
                        )
                    );
                    break;
                case SecondaryConversationPortraitInactiveBehavior.Swap:
                    StartTween(
                        Graphics2DUtils.CrossfadeSwapCoroutine(
                            _speakerPortraitImageActive,
                            _speakerPortraitImageInactive,
                            GfxSettings?.SwapCrossfade ?? 0.4f,
                            ease,
                            _tweenRunId
                        )
                    );
                    break;
                case SecondaryConversationPortraitInactiveBehavior.TintAndSwap:
                    StartTween(
                        TintAndSwapRoutine(
                            activeImg,
                            inactiveImg,
                            targetActiveColor,
                            targetInactiveColor,
                            duration,
                            ease
                        )
                    );
                    break;
                case SecondaryConversationPortraitInactiveBehavior.SwapAndHide:
                    StartTween(SwapAndHideRoutine(duration, ease));
                    break;
                case SecondaryConversationPortraitInactiveBehavior.None:
                    StartTween(
                        Graphics2DUtils.TintCoroutine(
                            activeImg,
                            inactiveImg,
                            Color.white,
                            Color.white,
                            duration,
                            ease,
                            _tweenRunId
                        )
                    );
                    break;
            }
        }

        private IEnumerator TintAndSwapRoutine(
            Image activeImg,
            Image inactiveImg,
            Color activeColor,
            Color inactiveColor,
            float duration,
            Ease ease
        )
        {
            // perform swap immediately
            (_speakerPortraitImageActive.sprite, _speakerPortraitImageInactive.sprite) = (
                _speakerPortraitImageInactive.sprite,
                _speakerPortraitImageActive.sprite
            );

            // then run tint animation on provided images
            yield return Graphics2DUtils.TintCoroutine(
                activeImg,
                inactiveImg,
                activeColor,
                inactiveColor,
                duration,
                ease,
                _tweenRunId
            );
        }

        private IEnumerator SwapAndHideRoutine(float duration, Ease ease)
        {
            (_speakerPortraitImageActive.sprite, _speakerPortraitImageInactive.sprite) = (
                _speakerPortraitImageInactive.sprite,
                _speakerPortraitImageActive.sprite
            );

            yield return Graphics2DUtils.HideCoroutine(
                _speakerPortraitImageInactive,
                duration,
                ease,
                _tweenRunId
            );
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

        private void KillImageTweens(params Image[] images)
        {
            if (!(GfxSettings?.AnimatePortraitTransitions ?? true))
            {
                return;
            }

            var nonNullImages = new List<Image>();
            foreach (var img in images)
            {
                if (img != null)
                {
                    nonNullImages.Add(img);
                }
            }

            if (nonNullImages.Count > 0)
            {
                Graphics2DUtils.KillImageTweens(nonNullImages.ToArray());
            }
        }
    }
}
