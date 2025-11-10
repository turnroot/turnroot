using System.Collections;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace TurnrootFramework.Conversations
{
    [CreateAssetMenu(
        fileName = "New Conversation",
        menuName = "Turnroot/Dialogue/Simple Conversation"
    )]
    public class SimpleConversation : ScriptableObject
    {
        public UnityEvent OnConversationStart;
        public UnityEvent OnConversationEnd;

        [SerializeField, ReorderableList]
        private SimpleConversationLayer[] _layers;
        public SimpleConversationLayer[] Layers
        {
            get => _layers;
            set => _layers = value;
        }

        [SerializeField]
        private int _currentLayerIndex = 0;
        public int CurrentLayerIndex
        {
            get => _currentLayerIndex;
            set => _currentLayerIndex = Mathf.Clamp(value, 0, _layers.Length - 1);
        }

        public SimpleConversationLayer CurrentLayer
        {
            get
            {
                if (_currentLayerIndex < 0 || _currentLayerIndex >= _layers.Length)
                    return null;
                return _layers[_currentLayerIndex];
            }
        }

        public void StartConversation()
        {
            OnConversationStart?.Invoke();
            _currentLayerIndex = 0;
        }
    }
}
