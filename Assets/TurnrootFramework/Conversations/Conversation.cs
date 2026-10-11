using System.Collections.Generic;
using System.Linq;
using Turnroot.Conversations.Mermaid;
using Turnroot.Gameplay.Objects;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.Conversations
{
    [CreateAssetMenu(fileName = "New Conversation", menuName = "Turnroot/Conversation")]
    public class Conversation : ScriptableObject
    {
        [SerializeField]
        private TextAsset mermaidSource;

        [SerializeField]
        private List<ConversationPerson> people = new();

        [SerializeField]
        private SerializableDictionary<string, ObjectItem> itemTemplates = new();

        [SerializeField]
        private bool canRepeat;

        [SerializeField]
        private string uniqueId;

        public string UniqueId
        {
            get
            {
                if (string.IsNullOrEmpty(uniqueId))
                {
                    uniqueId = System.Guid.NewGuid().ToString();
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(this);
#endif
                }

                return uniqueId;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(uniqueId))
            {
                uniqueId = System.Guid.NewGuid().ToString();
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif

        public TextAsset MermaidSource
        {
            get => mermaidSource;
            set => mermaidSource = value;
        }

        public bool CanRepeat
        {
            get => canRepeat;
            set => canRepeat = value;
        }

        public List<ConversationPerson> People
        {
            get => people ??= new List<ConversationPerson>();
            set => people = value ?? new List<ConversationPerson>();
        }

        public SerializableDictionary<string, ObjectItem> ItemTemplates =>
            itemTemplates ??= new SerializableDictionary<string, ObjectItem>();

        public bool TryGetItemTemplate(string itemId, out ObjectItem template)
        {
            template = null;
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            var trimmedId = itemId.Trim();

            foreach (var key in ItemTemplates.Keys)
            {
                if (string.Equals(key, trimmedId, System.StringComparison.OrdinalIgnoreCase))
                {
                    template = ItemTemplates[key];
                    return template != null;
                }
            }

            var resolved = FindItemTemplate(trimmedId);
            if (resolved != null)
            {
                RegisterItemTemplate(trimmedId, resolved);
                template = resolved;
                return true;
            }

            return false;
        }

        public void RegisterItemTemplate(string itemId, ObjectItem template)
        {
            if (string.IsNullOrWhiteSpace(itemId) || template == null)
            {
                return;
            }

            var trimmedId = itemId.Trim();
            foreach (var key in ItemTemplates.Keys.ToList())
            {
                if (string.Equals(key, trimmedId, System.StringComparison.OrdinalIgnoreCase))
                {
                    ItemTemplates.Remove(key);
                }
            }

            ItemTemplates[trimmedId] = template;
        }

        public void RegisterItemTemplatesFromGraph(MermaidConversationGraph graph)
        {
            if (graph == null)
            {
                return;
            }

            foreach (var node in graph.Nodes)
            {
                if (node?.Kind != MermaidNodeKind.Action)
                {
                    continue;
                }

                var actionType = node.ActionType?.Trim();
                if (string.IsNullOrWhiteSpace(actionType))
                {
                    continue;
                }

                var upper = actionType.ToUpperInvariant();
                if (
                    upper != "PLAYERGAINSITEM"
                    && upper != "GAINSITEM"
                    && upper != "PLAYERLOSESITEM"
                    && upper != "LOSESITEM"
                )
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(node.ActionTarget))
                {
                    continue;
                }

                if (!TryGetItemTemplate(node.ActionTarget, out _))
                {
                    var resolved = FindItemTemplate(node.ActionTarget);
                    if (resolved != null)
                    {
                        RegisterItemTemplate(node.ActionTarget, resolved);
                    }
                }
            }
        }

        private static ObjectItem FindItemTemplate(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return null;
            }

            var normalized = itemId.Trim();
            var candidates = Resources.FindObjectsOfTypeAll<ObjectItem>();
            foreach (var item in candidates)
            {
                if (item == null)
                {
                    continue;
                }

                if (
                    string.Equals(item.Id, normalized, System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(
                        item.name,
                        normalized,
                        System.StringComparison.OrdinalIgnoreCase
                    )
                    || string.Equals(
                        item.Name,
                        normalized,
                        System.StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return item;
                }
            }

            return null;
        }

        [System.NonSerialized]
        private MermaidConversationGraph _cachedGraph;

        public MermaidConversationGraph GetGraph()
        {
            if (_cachedGraph != null)
            {
                return _cachedGraph;
            }

            if (MermaidSource == null)
            {
                return null;
            }

            _cachedGraph = MermaidConversationParser.Parse(MermaidSource.text, name);
            return _cachedGraph;
        }

        public void InvalidateCache() => _cachedGraph = null;
    }
}
