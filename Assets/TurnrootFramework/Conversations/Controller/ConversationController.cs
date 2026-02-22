using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using TMPro;
using Turnroot.AbstractScripts.Graphics2D;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Turnroot.Conversations
{
    /// <summary>
    /// Manages conversation playback, UI, and branching dialogue choices.
    /// </summary>
    public partial class ConversationController : MonoBehaviour
    {
        private Coroutine _conversationRoutine;
        private int _tweenRunId;
        private readonly System.Collections.Generic.List<Coroutine> _activeTweens = new();
        private Sprite _lastActiveSprite;
        private int _pendingChoiceTarget = int.MinValue;
        private int _activeBranchingNodeId = int.MinValue;
        private ConversationLayer _activeBranchingLayer;
        private ConversationInstance _runningInstance;

        [Header("Available Conversations")]
        [SerializeField]
        private List<ConversationInstance> _conversationInstances = new();

        [SerializeField]
        private int _currentConversation;

        [Header("UI References")]
        [SerializeField]
        private TextMeshProUGUI _dialogueText;

        [SerializeField]
        private TextMeshProUGUI _speakerNameText;

        [SerializeField]
        private Image _speakerPortraitImageActive;

        [SerializeField]
        private Image _speakerPortraitImageInactive;

        [Header("Choice UI")]
        [SerializeField]
        private GameObject _choiceButtonPrefab;

        [SerializeField]
        private Transform _choiceButtonsContainer;

        [Header("Controller Events")]
        public UnityEvent OnAwake;
        public UnityEvent OnAnyConversationStart;
        public UnityEvent OnAnyConversationFinished;

        private ConversationInstance SelectedInstance =>
            _conversationInstances != null
            && _currentConversation >= 0
            && _currentConversation < _conversationInstances.Count
                ? _conversationInstances[_currentConversation]
                : null;

        private void Awake() => OnAwake?.Invoke();

        private Conversation SelectedConversation => SelectedInstance?.Conversation;

        private Coroutine StartTween(System.Collections.IEnumerator routine)
        {
            if (routine == null)
            {
                return null;
            }

            var c = StartCoroutine(routine);
            _activeTweens.Add(c);
            return c;
        }

        private void CancelActiveTweens()
        {
            foreach (var c in _activeTweens)
            {
                if (c != null)
                {
                    StopCoroutine(c);
                }
            }
            _activeTweens.Clear();

            // tidy up any temporary swap overlays that might have been left behind
            // when a coroutine was stopped early.
            // use the newer API to avoid unnecessary sorting overhead
            var allImages = FindObjectsByType<Image>(FindObjectsSortMode.None);
            foreach (var img in allImages)
            {
                if (img.gameObject.name.StartsWith("swap_overlay_"))
                {
                    Destroy(img.gameObject);
                }
            }
        }

        private Graphics2DSettings GfxSettings => Graphics2DSettings.Instance;

        public void Advance() => NextLayer();

        public void Proceed() => NextLayer();

        public void StartCurrentConversation() => StartConversation();

        public void StartConversationAtIndex(int index)
        {
            if (index < 0 || index >= _conversationInstances.Count)
            {
                return;
            }

            _currentConversation = index;
            StartConversation();
        }

        public void IncrementConversationIndex()
        {
            if (_conversationInstances == null || _conversationInstances.Count == 0)
            {
                return;
            }

            _currentConversation = (_currentConversation + 1) % _conversationInstances.Count;
        }

        public void DecrementConversationIndex()
        {
            if (_conversationInstances == null || _conversationInstances.Count == 0)
            {
                return;
            }

            _currentConversation =
                (_currentConversation - 1 + _conversationInstances.Count)
                % _conversationInstances.Count;
        }

        public bool ChooseBranchTarget(int targetNodeId)
        {
            if (SelectedConversation?.BranchingConversation != true)
            {
                return false;
            }

            _pendingChoiceTarget = targetNodeId;
            ClearChoiceButtons();
            return true;
        }

        public List<ChoiceData> GetCurrentChoices()
        {
            if (
                SelectedConversation?.BranchingConversation != true
                || _activeBranchingNodeId == int.MinValue
            )
            {
                return null;
            }

            var nodes = SelectedConversation.GetGraphNodes();
            return nodes?.TryGetValue(_activeBranchingNodeId, out var node) == true
                ? node.choices
                : null;
        }

        [Button("Start Conversation")]
        public void StartConversation()
        {
            if (!ValidateConversationStart())
            {
                return;
            }

            CleanupPreviousConversation();
            ResetUI();

            var instance = SelectedInstance;
            SelectedConversation?.StartConversation();
            SelectedConversation?.OnConversationStart?.Invoke();
            instance?.OnConversationStart?.Invoke();
            OnAnyConversationStart?.Invoke();

            _runningInstance = instance;
            _conversationRoutine = StartCoroutine(RunConversation(instance));
        }

        [Button("Next Layer")]
        public void NextLayer()
        {
            if (_activeBranchingLayer != null)
            {
                _activeBranchingLayer.CompleteLayer();
                return;
            }
            SelectedConversation?.CurrentLayer?.CompleteLayer();
        }
    }
}
