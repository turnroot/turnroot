using System.Collections;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TurnrootFramework.Conversations
{
    public class ConversationController : MonoBehaviour
    {
        [SerializeField]
        SerializableDictionary<int, SimpleConversation> _sceneConversations;

        [SerializeField]
        private SimpleConversation _currentConversation;

        [SerializeField]
        private TextMeshProUGUI _dialogueText;

        [SerializeField]
        private TextMeshProUGUI _speakerNameText;

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
            if (_sceneConversations?.Dictionary == null)
            {
                Debug.LogError($"{nameof(_sceneConversations)} is null or not initialized.");
                return;
            }

            if (!_sceneConversations.Dictionary.TryGetValue(index, out var conversation))
            {
                Debug.LogError($"No conversation found for scene index {index}");
                return;
            }

            _currentConversation = conversation;
        }

        public void SetAndStartConversation(int index)
        {
            SetConversation(index);
            StartConversation();
        }

        private IEnumerator RunConversation(SimpleConversation conversation)
        {
            if (conversation == null)
                yield break;

            _currentConversation = conversation;
            Debug.Log($"Starting conversation: {conversation.name}");

            for (int i = 0; i < conversation.Layers.Length; i++)
            {
                conversation.CurrentLayerIndex = i;
                var layer = conversation.Layers[i];

                if (!layer.HasBeenParsed)
                {
                    layer.ParseDialogue();
                }

                layer.StartLayer();
                _dialogueText.text = layer.Dialogue;
                _speakerNameText.text = !string.IsNullOrWhiteSpace(layer.SpeakerDisplayName)
                    ? layer.SpeakerDisplayName
                    : (
                        layer.Speaker != null
                        && !string.IsNullOrWhiteSpace(layer.Speaker.DisplayName)
                            ? layer.Speaker.DisplayName
                            : string.Empty
                    );

                bool completed = false;
                UnityEngine.Events.UnityAction onComplete = () => completed = true;
                layer.OnLayerComplete.AddListener(onComplete);

                yield return new WaitUntil(() => completed);

                layer.OnLayerComplete.RemoveListener(onComplete);
            }

            if (TryGetNextConversation(out var nextConversation))
            {
                yield return StartCoroutine(RunConversation(nextConversation));
            }
            else
            {
                Debug.Log("Conversation sequence completed.");
            }
        }

        private bool TryGetNextConversation(out SimpleConversation nextConversation)
        {
            nextConversation = null;

            var dict = _sceneConversations?.Dictionary;
            if (dict == null || _currentConversation == null)
            {
                return false;
            }

            foreach (var kvp in dict)
            {
                if (kvp.Value != _currentConversation)
                {
                    continue;
                }

                int nextKey = kvp.Key + 1;
                if (dict.TryGetValue(nextKey, out nextConversation))
                {
                    _currentConversation = nextConversation;
                    return true;
                }

                break;
            }

            return false;
        }
    }
}
