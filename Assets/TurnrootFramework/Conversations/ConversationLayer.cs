using System.Linq;
using NaughtyAttributes;
using Turnroot.AbstractScripts.Graphics2D;
using Turnroot.Characters;
using Turnroot.Characters.Subclasses;
using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace Turnroot.Conversations
{
    /// <summary>
    /// Represents a single layer of dialogue with speaker configuration, portraits, and lifecycle events.
    /// </summary>
    [System.Serializable]
    public class ConversationLayer : BaseConversation
    {
        /// <summary>
        /// Represents a speaker configuration with character data, display name, and portrait selection.
        /// </summary>
        [System.Serializable]
        public class SpeakerSlot
        {
            [SerializeField, SerializeReference]
            public CharacterData Speaker;

            [SerializeField]
            public string DisplayName;

            [
                SerializeField,
                Dropdown("GetAvailablePortraitKeys"),
                OnValueChanged("OnPortraitKeyChanged")
            ]
            public string PortraitKey;

            private string[] GetAvailablePortraitKeys()
            {
                if (
                    !ValidationHelper.ValidateNotNull(Speaker, nameof(Speaker))
                    || Speaker.PortraitCount == 0
                )
                {
                    if (!string.IsNullOrEmpty(PortraitKey))
                    {
                        PortraitKey = null;
#if UNITY_EDITOR
                        var sel = UnityEditor.Selection.activeObject as Object;
                        if (sel != null)
                        {
                            var selPath = UnityEditor.AssetDatabase.GetAssetPath(sel);
                            if (!string.IsNullOrEmpty(selPath))
                            {
                                UnityEditor.EditorUtility.SetDirty(sel);
                            }
                        }
#endif
                    }
                    return new string[] { "No speaker selected" };
                }

                var keys = Speaker.GetPortraitKeys();
                if (!string.IsNullOrEmpty(PortraitKey) && !keys.Contains(PortraitKey))
                {
                    PortraitKey = null;
#if UNITY_EDITOR
                    var sel = UnityEditor.Selection.activeObject as Object;
                    if (sel != null)
                    {
                        var selPath = UnityEditor.AssetDatabase.GetAssetPath(sel);
                        if (!string.IsNullOrEmpty(selPath))
                        {
                            UnityEditor.EditorUtility.SetDirty(sel);
                        }
                    }
#endif
                }
                return keys.Length > 0 ? keys : new string[] { "No portraits available" };
            }

            [System.NonSerialized]
            public Sprite CachedSprite;

            private void OnPortraitKeyChanged()
            {
                CachedSprite = null;
#if UNITY_EDITOR
                // Ensure the change is recorded so the conversation asset is marked dirty and saved.
                var sel = UnityEditor.Selection.activeObject as Object;
                if (sel != null)
                {
                    var selPath = UnityEditor.AssetDatabase.GetAssetPath(sel);
                    if (!string.IsNullOrEmpty(selPath))
                    {
                        UnityEditor.EditorUtility.SetDirty(sel);
                    }
                }
#endif
            }
        }

        [SerializeField]
        private SpeakerSlot _primary = new();

        [SerializeField]
        private SpeakerSlot _secondary = new();

        /// <summary>
        /// Indicates which speaker (primary or secondary) is currently active in the conversation.
        /// </summary>
        public enum ActiveSpeakerType
        {
            Primary = 0,
            Secondary = 1,
        }

        [Header("Active Speaker")]
        [SerializeField]
        private ActiveSpeakerType _activeSpeaker = ActiveSpeakerType.Primary;

        public CharacterData Speaker
        {
            get => _primary.Speaker;
            set
            {
                _primary.Speaker = value;
                ValidatePortraitKeyOnSpeakerChange(ref _primary.PortraitKey, _primary.Speaker);
                _primary.CachedSprite = null;
            }
        }

        public CharacterData SecondarySpeaker
        {
            get => _secondary.Speaker;
            set
            {
                _secondary.Speaker = value;
                ValidatePortraitKeyOnSpeakerChange(ref _secondary.PortraitKey, _secondary.Speaker);
                _secondary.CachedSprite = null;
            }
        }

        public string SpeakerDisplayName
        {
            get => _primary.DisplayName;
            set => _primary.DisplayName = value;
        }

        /// <summary>
        /// Overrides the cached portrait sprite used for the primary speaker (used in one-shot playback).
        /// </summary>
        public void SetPrimaryPortraitSprite(Sprite sprite) => _primary.CachedSprite = sprite;

        public string SecondarySpeakerDisplayName
        {
            get => _secondary.DisplayName;
            set => _secondary.DisplayName = value;
        }

        public Portrait SpeakerPortrait => GetPortrait(_primary.Speaker, _primary.PortraitKey);

        public Portrait SecondarySpeakerPortrait =>
            GetPortrait(_secondary.Speaker, _secondary.PortraitKey);

        /// <summary>
        /// Contains Unity events triggered at the start and completion of a conversation layer.
        /// </summary>
        [System.Serializable]
        public class LayerEvents
        {
            public UnityEvent OnLayerStart = new UnityEvent();
            public UnityEvent OnLayerComplete = new UnityEvent();
        }

        [HideInInspector]
        public UnityEvent OnLayerStart => Events.OnLayerStart;

        [HideInInspector]
        public UnityEvent OnLayerComplete => Events.OnLayerComplete;

        [Foldout("Events")]
        public LayerEvents Events = new();

        public Sprite PortraitSprite => GetPortraitSpriteForSlot(_primary);

        public Sprite SecondaryPortraitSprite => GetPortraitSpriteForSlot(_secondary);

        public void Awake()
        {
            if (SpeakerPortrait != null)
            {
                _primary.CachedSprite = SpeakerPortrait.SavedSprite;
            }
            if (SecondarySpeakerPortrait != null)
            {
                _secondary.CachedSprite = SecondarySpeakerPortrait.SavedSprite;
            }
        }

        public void StartLayer() => OnLayerStart?.Invoke();

        public void CompleteLayer() => OnLayerComplete?.Invoke();

        private void ValidatePortraitKeyOnSpeakerChange(
            ref string portraitKey,
            CharacterData speaker
        )
        {
            if (
                speaker == null
                || (portraitKey != null && !speaker.ContainsPortraitKey(portraitKey))
            )
            {
                portraitKey = null;
            }
        }

        private Portrait GetPortrait(CharacterData speaker, string portraitKey) => speaker != null && portraitKey != null ? speaker.GetPortrait(portraitKey) : null;

        // Active speaker helpers
        public ActiveSpeakerType ActiveSpeaker
        {
            get => _activeSpeaker;
            set => _activeSpeaker = value;
        }

        public SpeakerSlot GetActiveSlot() =>
            _activeSpeaker == ActiveSpeakerType.Primary ? _primary : _secondary;

        public Portrait ActivePortrait =>
            GetPortrait(GetActiveSlot().Speaker, GetActiveSlot().PortraitKey);

        // Tint helpers: return the color that should be applied to a portrait image
        public Color GetPortraitTint(SpeakerSlot slot)
        {
            if (slot == null)
            {
                return Color.white;
            }

            if (slot == GetActiveSlot())
            {
                return Color.white;
            }

            var settings = Graphics2DSettings.Instance;
            var tintColor = settings?.InactiveTintColor ?? new Color(0.5f, 0.5f, 0.5f, 1f);
            var tintMix = settings?.InactiveTintMix ?? 0.5f;
            return Color.Lerp(Color.white, tintColor, tintMix);
        }

        public Color PrimaryPortraitTint => GetPortraitTint(_primary);
        public Color SecondaryPortraitTint => GetPortraitTint(_secondary);

        private Sprite GetPortraitSpriteForSlot(SpeakerSlot slot)
        {
            if (slot == null)
            {
                return null;
            }

            if (slot.CachedSprite == null)
            {
                // If a portrait key is set, use it. Otherwise, try to pick the first available portrait
                var p = GetPortrait(slot.Speaker, slot.PortraitKey);
                if (p == null && slot.Speaker?.PortraitCount > 0)
                {
                    // pick the first available portrait key as a sensible default
                    var keys = slot.Speaker.GetPortraitKeys();
                    if (keys.Length > 0)
                    {
                        slot.PortraitKey = keys[0];
                        p = slot.Speaker.GetPortrait(slot.PortraitKey);
                    }
                }

                if (p != null)
                {
                    slot.CachedSprite = p.SavedSprite;
                }
            }
            return slot.CachedSprite;
        }
    }
}
