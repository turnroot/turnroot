using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;

namespace Turnroot.AbstractScripts.Graphics2D
{
    public enum SecondaryConversationPortraitInactiveBehavior
    {
        Hide,
        Tint,
        Swap,
        TintAndSwap,
        SwapAndHide,
        None,
    }

    [CreateAssetMenu(
        fileName = "Graphics2DSettings",
        menuName = "Turnroot/Game Settings/Graphics/Graphics2D Settings"
    )]
    public class Graphics2DSettings : SingletonScriptableObject<Graphics2DSettings>
    {
        [SerializeField, BoxGroup("Conversations"), HorizontalLine(color: EColor.Blue)]
        private SecondaryConversationPortraitInactiveBehavior _secondaryConversationPortraitInactiveBehavior =
            SecondaryConversationPortraitInactiveBehavior.Hide;

        [SerializeField, BoxGroup("Conversations")]
        private bool _animatePortraitTransitions = true;

        [SerializeField, BoxGroup("Conversations"), Range(0f, 2f)]
        private float _portraitTransitionDuration = 0.4f;

        [SerializeField, BoxGroup("Conversations"), Range(0f, 2f)]
        private float _swapCrossfade = 0.4f;

        [SerializeField, BoxGroup("Conversations")]
        private Color _inactiveTintColor = new(0.5f, 0.5f, 0.5f, 1f);

        [SerializeField, BoxGroup("Conversations"), Range(0f, 1f)]
        private float _inactiveTintMix = 0.5f;

        [SerializeField, BoxGroup("Conversations")]
        private Ease _portraitTransitionEase = Ease.InOutSine;

        // Public accessors
        public SecondaryConversationPortraitInactiveBehavior SecondaryConversationPortraitInactiveBehavior =>
            _secondaryConversationPortraitInactiveBehavior;
        public bool AnimatePortraitTransitions => _animatePortraitTransitions;
        public float PortraitTransitionDuration => _portraitTransitionDuration;
        public Ease PortraitTransitionEase => _portraitTransitionEase;
        public float SwapCrossfade => _swapCrossfade;
        public Color InactiveTintColor => _inactiveTintColor;
        public float InactiveTintMix => _inactiveTintMix;

        [Header("Portrait Render Settings")]
        public int portraitRenderWidth = 512;

        [Header("Portrait Render Settings")]
        public int portraitRenderHeight = 512;
    }
}
