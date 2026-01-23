using DG.Tweening;
using TMPro;
using Turnroot.AbstractScripts.Graphics2D;
using UnityEngine;
using UnityEngine.UI;

namespace Turnroot.Conversations
{
    public partial class ConversationController : MonoBehaviour
    {
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
    }
}
