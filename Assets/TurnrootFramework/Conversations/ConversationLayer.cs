using System;
using System.Linq;
using NaughtyAttributes;
using Turnroot.AbstractScripts.Graphics2D;
using Turnroot.Characters;
using Turnroot.Characters.Subclasses;
using Turnroot.Utilities;
using UnityEngine;

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
                        var sel = UnityEditor.Selection.activeObject as UnityEngine.Object;
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
                    var sel = UnityEditor.Selection.activeObject as UnityEngine.Object;
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
                var sel = UnityEditor.Selection.activeObject as UnityEngine.Object;
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

        public string SpeakerPortraitKey
        {
            get => _primary.PortraitKey;
            set
            {
                _primary.PortraitKey = value;
                _primary.CachedSprite = null;
            }
        }

        public string SecondarySpeakerPortraitKey
        {
            get => _secondary.PortraitKey;
            set
            {
                _secondary.PortraitKey = value;
                _secondary.CachedSprite = null;
            }
        }

        public void SetPortraitKey(ActiveSpeakerType type, string key)
        {
            var slot = type == ActiveSpeakerType.Primary ? _primary : _secondary;
            slot.PortraitKey = key;
            slot.CachedSprite = null;
        }

        public string SecondarySpeakerDisplayName
        {
            get => _secondary.DisplayName;
            set => _secondary.DisplayName = value;
        }

        public Portrait SpeakerPortrait => GetPortrait(_primary.Speaker, _primary.PortraitKey);

        public Portrait SecondarySpeakerPortrait =>
            GetPortrait(_secondary.Speaker, _secondary.PortraitKey);

        /// <summary>
        /// Raised when the layer starts playback.
        /// </summary>
        public event Action OnLayerStarted;

        /// <summary>
        /// Raised when the layer is completed by the player advancing the conversation.
        /// </summary>
        public event Action OnLayerCompleted;

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

        public void StartLayer() => OnLayerStarted?.Invoke();

        public void CompleteLayer() => OnLayerCompleted?.Invoke();

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

        private Portrait GetPortrait(CharacterData speaker, string portraitKey) =>
            speaker != null && portraitKey != null ? speaker.GetPortrait(portraitKey) : null;

        public ActiveSpeakerType ActiveSpeaker
        {
            get => _activeSpeaker;
            set => _activeSpeaker = value;
        }

        public SpeakerSlot GetActiveSlot() =>
            _activeSpeaker == ActiveSpeakerType.Primary ? _primary : _secondary;

        private SpeakerSlot GetInactiveSlot() =>
            _activeSpeaker == ActiveSpeakerType.Primary ? _secondary : _primary;

        private (Sprite sprite, Color tint) GetPortraitInfo(SpeakerSlot slot)
        {
            if (slot == null)
            {
                return (null, Color.white);
            }

            CachePortraitForSlot(slot);

            var tint = slot == GetActiveSlot() ? Color.white : ComputeInactiveTint();

            return (slot.CachedSprite, tint);
        }

        private void CachePortraitForSlot(SpeakerSlot slot)
        {
            if (slot.CachedSprite != null)
            {
                return;
            }

            var p = GetPortrait(slot.Speaker, slot.PortraitKey);
            if (p == null && slot.Speaker?.PortraitCount > 0)
            {
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

        private static Color ComputeInactiveTint()
        {
            var settings = Graphics2DSettings.Instance;
            var tintColor = settings?.InactiveTintColor ?? new Color(0.5f, 0.5f, 0.5f, 1f);
            var tintMix = settings?.InactiveTintMix ?? 0.5f;
            return Color.Lerp(Color.white, tintColor, tintMix);
        }

        public (
            Sprite activeSprite,
            Color activeTint,
            Sprite inactiveSprite,
            Color inactiveTint
        ) GetActiveAndInactivePortraits()
        {
            var active = GetPortraitInfo(GetActiveSlot());
            var inactive = GetPortraitInfo(GetInactiveSlot());
            return (active.sprite, active.tint, inactive.sprite, inactive.tint);
        }
    }
}
