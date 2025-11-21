using System.Linq;
using NaughtyAttributes;
using Turnroot.AbstractScripts.Graphics2D;
using Turnroot.Characters;
using Turnroot.Characters.Subclasses;
using UnityEngine;
using UnityEngine.Events;

namespace Turnroot.Conversations
{
    [System.Serializable]
    public class ConversationLayer : BaseConversation
    {
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
                if (Speaker == null || Speaker.Portraits == null)
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

                var keys = Speaker.Portraits.Keys.ToArray();
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

        public string SecondarySpeakerDisplayName
        {
            get => _secondary.DisplayName;
            set => _secondary.DisplayName = value;
        }

        public Portrait SpeakerPortrait => GetPortrait(_primary.Speaker, _primary.PortraitKey);

        public Portrait SecondarySpeakerPortrait =>
            GetPortrait(_secondary.Speaker, _secondary.PortraitKey);

        [System.Serializable]
        public class LayerEvents
        {
            public UnityEvent OnLayerStart;
            public UnityEvent OnLayerComplete;
        }

        [HideInInspector]
        public UnityEvent OnLayerStart => Events.OnLayerStart;

        [HideInInspector]
        public UnityEvent OnLayerComplete => Events.OnLayerComplete;

        [Foldout("Events")]
        public LayerEvents Events = new();

        public Sprite PortraitSprite => GetPortraitSpriteForSlot(_primary);

        public Sprite SecondaryPortraitSprite => GetPortraitSpriteForSlot(_secondary);

        public void OnAwake()
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

        public void StartLayer()
        {
            OnLayerStart?.Invoke();
        }

        public void CompleteLayer()
        {
            OnLayerComplete?.Invoke();
        }

        private void ValidatePortraitKeyOnSpeakerChange(
            ref string portraitKey,
            CharacterData speaker
        )
        {
            if (
                speaker == null
                || (
                    portraitKey != null
                    && (speaker.Portraits == null || !speaker.Portraits.ContainsKey(portraitKey))
                )
            )
            {
                portraitKey = null;
            }
        }

        private Portrait GetPortrait(CharacterData speaker, string portraitKey)
        {
            if (
                speaker != null
                && portraitKey != null
                && speaker.Portraits != null
                && speaker.Portraits.ContainsKey(portraitKey)
            )
            {
                return speaker.Portraits[portraitKey];
            }
            return null;
        }

        // Active speaker helpers
        public ActiveSpeakerType ActiveSpeaker
        {
            get => _activeSpeaker;
            set => _activeSpeaker = value;
        }

        public SpeakerSlot GetActiveSlot()
        {
            return _activeSpeaker == ActiveSpeakerType.Primary ? _primary : _secondary;
        }

        public Portrait ActivePortrait =>
            GetPortrait(GetActiveSlot().Speaker, GetActiveSlot().PortraitKey);

        // Tint helpers: return the color that should be applied to a portrait image
        public Color GetPortraitTint(SpeakerSlot slot)
        {
            if (slot == null)
                return Color.white;
            if (slot == GetActiveSlot())
                return Color.white;
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
                return null;
            if (slot.CachedSprite == null)
            {
                // If a portrait key is set, use it. Otherwise, try to pick the first available portrait
                var p = GetPortrait(slot.Speaker, slot.PortraitKey);
                if (p == null && slot.Speaker?.Portraits != null)
                {
                    // pick the first available portrait key as a sensible default
                    var keys = slot.Speaker.Portraits.Keys.ToArray();
                    if (keys.Length > 0)
                    {
                        slot.PortraitKey = keys[0];
                        p = slot.Speaker.Portraits[slot.PortraitKey];
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
