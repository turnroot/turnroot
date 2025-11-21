using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Turnroot.Conversations
{
    /// <summary>
    /// Scene-level wrapper for a Conversation ScriptableObject allowing
    /// per-scene UnityEvent wiring (conversation and per-layer events).
    /// </summary>
    public class ConversationInstance : MonoBehaviour
    {
        [Header("Conversation Reference")]
        public Conversation Conversation;

        [Header("Conversation Events (scene-level)")]
        public UnityEvent OnConversationStart;

        public UnityEvent OnConversationFinished;

        [System.Serializable]
        public class LayerEvents
        {
            [Tooltip("Index of layer in the referenced Conversation (0-based)")]
            public int LayerIndex;

            public UnityEvent OnLayerStart;
            public UnityEvent OnLayerComplete;
        }

        [Header("Per-layer events (scene-level)")]
        public List<LayerEvents> PerLayerEvents = new List<LayerEvents>();

        /// <summary>
        /// Try to find the per-layer events for the given zero-based layer index.
        /// </summary>
        public LayerEvents GetEventsForLayer(int layerIndex)
        {
            if (PerLayerEvents == null)
                return null;
            for (int i = 0; i < PerLayerEvents.Count; i++)
            {
                var e = PerLayerEvents[i];
                if (e != null && e.LayerIndex == layerIndex)
                    return e;
            }
            return null;
        }
    }
}
