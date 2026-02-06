using DG.Tweening;
using NaughtyAttributes;
using Turnroot.Utilities.AbstractScripts;
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
        [SerializeField, BoxGroup("Conversations")]
        private Color _inactiveTintColor = new(0.5f, 0.5f, 0.5f, 1f);

        // Public accessors
        [field: SerializeField, BoxGroup("Conversations"), HorizontalLine(color: EColor.Blue)]
        public SecondaryConversationPortraitInactiveBehavior SecondaryConversationPortraitInactiveBehavior { get; } = SecondaryConversationPortraitInactiveBehavior.Hide;
        [field: SerializeField, BoxGroup("Conversations")]
        public bool AnimatePortraitTransitions { get; } = true;
        [field: SerializeField, BoxGroup("Conversations"), Range(0f, 2f)]
        public float PortraitTransitionDuration { get; } = 0.4f;
        [field: SerializeField, BoxGroup("Conversations")]
        public Ease PortraitTransitionEase { get; } = Ease.InOutSine;
        [field: SerializeField, BoxGroup("Conversations"), Range(0f, 2f)]
        public float SwapCrossfade { get; } = 0.4f;
        public Color InactiveTintColor => _inactiveTintColor;
        [field: SerializeField, BoxGroup("Conversations"), Range(0f, 1f)]
        public float InactiveTintMix { get; } = 0.5f;

        [Header("Portrait Render Settings")]
        public int portraitRenderWidth = 512;

        [Header("Portrait Render Settings")]
        public int portraitRenderHeight = 512;
    }
}
