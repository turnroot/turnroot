using System.Collections;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace TurnrootFramework.Conversations
{
    public class ConversationController : MonoBehaviour
    {
        [SerializeField]
        SerializableDictionary<int, SimpleConversation> _sceneConversations;

        [SerializeField]
        private SimpleConversation _currentConversation;

        [Button("Start Conversation")]
        public void StartConversation()
        {
            if (_currentConversation == null)
            {
                Debug.LogError("Cannot start a null conversation.");
                return;
            }

            StartCoroutine(RunConversation(_currentConversation));
        }

        [Button("Next Layer")]
        public void NextLayer()
        {
            if (_currentConversation?.CurrentLayer != null)
            {
                _currentConversation.CurrentLayer.CompleteLayer();
            }
        }

        public void Proceed()
        {
            NextLayer();
        }

        public void SetConversation(int index)
        {
            if (_sceneConversations.Dictionary.TryGetValue(index, out var conversation))
            {
                _currentConversation = conversation;
            }
            else
            {
                Debug.LogError($"No conversation found for scene index {index}");
            }
        }

        public void SetAndStartConversation(int index)
        {
            SetConversation(index);
            StartConversation();
        }

        private IEnumerator RunConversation(SimpleConversation conversation)
        {
            Debug.Log($"Starting conversation: {conversation.name}");
            // Iterate through each layer
            for (int i = 0; i < conversation.Layers.Length; i++)
            {
                conversation.CurrentLayerIndex = i;
                var currentLayer = conversation.Layers[i];
                Debug.Log(
                    $"Running layer {i + 1}/{conversation.Layers.Length}: {currentLayer.Dialogue}"
                );

                if (currentLayer != null)
                {
                    // Start the layer
                    currentLayer.StartLayer();

                    // Wait for the layer to complete
                    bool layerCompleted = false;
                    UnityAction completionCallback = () => layerCompleted = true;

                    currentLayer.OnLayerComplete.AddListener(completionCallback);

                    // Wait until the layer signals completion
                    yield return new WaitUntil(() => layerCompleted);

                    currentLayer.OnLayerComplete.RemoveListener(completionCallback);

                    // End the layer
                    currentLayer.EndLayer();
                }
            }

            // Check if there's a next conversation to auto-advance to
            if (TryGetNextConversation(out var nextConversation))
            {
                Debug.Log($"Auto-advancing to next conversation: {nextConversation.name}");
                yield return StartCoroutine(RunConversation(nextConversation));
            }
            else
            {
                Debug.Log("Conversation sequence completed - no more conversations");
            }
        }

        private bool TryGetNextConversation(out SimpleConversation nextConversation)
        {
            nextConversation = null;

            if (_sceneConversations == null || _currentConversation == null)
                return false;

            // Find the current conversation's key
            foreach (var kvp in _sceneConversations.Dictionary)
            {
                if (kvp.Value == _currentConversation)
                {
                    // Try to get the next conversation (current key + 1)
                    int nextKey = kvp.Key + 1;
                    if (_sceneConversations.Dictionary.TryGetValue(nextKey, out nextConversation))
                    {
                        _currentConversation = nextConversation;
                        return true;
                    }
                    break;
                }
            }

            return false;
        }
    }
}
