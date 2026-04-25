using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using TMPro;
using Turnroot.AbstractScripts.Graphics2D;
using Turnroot.UI;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
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
        private readonly List<Coroutine> _activeTweens = new();
        private Sprite _lastActiveSprite;
        private int _pendingChoiceTarget = int.MinValue;
        private int _activeBranchingNodeId = int.MinValue;
        private ConversationLayer _activeBranchingLayer;
        private ConversationInstance _runningInstance;

        // One-shot playback support
        [Header("Audio")]
        [SerializeField]
        private AudioSource _audioSource;

        [Header("UI")]
        [SerializeField]
        private UIFade _uiFade;

        private ConversationLayer _activeOneShotLayer;
        private Coroutine _oneShotRoutine;

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

        // Uses shared UI actions configured via UIInputActionBootstrap

        [Header("Choice UI")]
        [SerializeField]
        private GameObject _choiceButtonPrefab;

        [SerializeField]
        private Transform _choiceButtonsContainer;

        [Header("Controller Events")]
        public UnityEvent OnAwake;
        public UnityEvent OnAnyConversationStart;
        public UnityEvent OnAnyConversationFinished;

        #region Help & Documentation

#if UNITY_EDITOR
        [Button("📖 Show Conversation System Help", EButtonEnableMode.Always)]
        private void ShowHelp()
        {
            // Use reflection to call the editor window since it's in a separate Editor assembly
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            System.Type windowType = null;

            foreach (var assembly in assemblies)
            {
                windowType = assembly.GetType(
                    "Turnroot.Conversations.Editor.ConversationControllerHelpWindow"
                );
                if (windowType != null)
                {
                    break;
                }
            }

            if (windowType != null)
            {
                var showMethod = windowType.GetMethod(
                    "ShowWindowFromButton",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
                );
                showMethod?.Invoke(null, null);
            }
            else
            {
                UnityEditor.EditorUtility.DisplayDialog(
                    "Help",
                    "Could not find ConversationControllerHelpWindow editor script",
                    "OK"
                );
            }
        }
#endif

        #endregion

        private ConversationInstance SelectedInstance =>
            _conversationInstances != null
            && _currentConversation >= 0
            && _currentConversation < _conversationInstances.Count
                ? _conversationInstances[_currentConversation]
                : null;

        private void Awake()
        {
            EnsureAudioSource();
            OnAwake?.Invoke();
        }

        private bool _inputSubscribed;

        private void SubscribeAdvanceInput()
        {
            if (_inputSubscribed)
            {
                return;
            }

            var action = UIInputActionDefaults.Select;
            if (action != null)
            {
                action.performed += OnAdvanceInputPerformed;
                _inputSubscribed = true;
            }
        }

        private void UnsubscribeAdvanceInput()
        {
            if (!_inputSubscribed)
            {
                return;
            }

            var action = UIInputActionDefaults.Select;
            if (action != null)
            {
                action.performed -= OnAdvanceInputPerformed;
            }
            _inputSubscribed = false;
        }

        private void OnDisable()
        {
            UnsubscribeAdvanceInput();

            CleanupTweens();

            if (_conversationRoutine != null)
            {
                StopCoroutine(_conversationRoutine);
                _conversationRoutine = null;
            }

            if (_oneShotRoutine != null)
            {
                StopCoroutine(_oneShotRoutine);
                _oneShotRoutine = null;
            }
        }

        private void OnAdvanceInputPerformed(InputAction.CallbackContext context)
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            // Only advance when a conversation or one-shot is actually in progress.
            if (
                _activeOneShotLayer == null
                && _activeBranchingLayer == null
                && SelectedConversation?.CurrentLayer == null
            )
            {
                return;
            }

            NextLayer();
        }

        private void EnsureAudioSource()
        {
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
            }

            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
            }
        }

        private Conversation SelectedConversation => SelectedInstance?.Conversation;

        private Coroutine StartTween(IEnumerator routine)
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
            SubscribeAdvanceInput();
            _conversationRoutine = StartCoroutine(RunConversation(instance));
        }

        [Button("Next Layer")]
        public void NextLayer()
        {
            if (_activeOneShotLayer != null)
            {
                _activeOneShotLayer.CompleteLayer();
                return;
            }

            if (_activeBranchingLayer != null)
            {
                _activeBranchingLayer.CompleteLayer();
                return;
            }
            SelectedConversation?.CurrentLayer?.CompleteLayer();
        }

        /// <summary>
        /// Plays a full <see cref="Conversation"/> asset immediately without requiring it to be
        /// pre-registered in the <c>ConversationInstances</c> list.
        /// Intended for runtime-selected conversations (e.g. hub chitchat).
        /// <paramref name="onFinished"/> is called once the conversation completes.
        /// </summary>
        public void PlayConversationDirect(Conversation conversation, UnityAction onFinished = null)
        {
            if (conversation == null)
            {
                "PlayConversationDirect called with null conversation.".LogInfo();
                return;
            }

            CleanupPreviousConversation();
            ResetUI();
            ShowConversationUI();

            conversation.StartConversation();
            OnAnyConversationStart?.Invoke();

            SubscribeAdvanceInput();
            _conversationRoutine = StartCoroutine(RunConversationDirect(conversation, onFinished));
        }

        private IEnumerator RunConversationDirect(Conversation conversation, UnityAction onFinished)
        {
            var sceneFlow = FindFirstObjectByType<BattleSceneFlow>();

            yield return conversation.BranchingConversation
                ? RunBranchingConversation(conversation, sceneFlow)
                : RunLinearConversation(conversation, sceneFlow);

            onFinished?.Invoke();
            UnsubscribeAdvanceInput();
            OnAnyConversationFinished?.Invoke();

            if (
                sceneFlow != null
                && sceneFlow.IsInterruptQueued
                && sceneFlow.CurrentInterrupt
                    == Utilities.AbstractScripts.InterruptType.Conversation
            )
            {
                sceneFlow.CompleteInterrupt();
            }

            _conversationRoutine = null;
        }

        /// <summary>
        /// Play a short, one‑layer conversation (e.g. a single NPC quip).
        /// This is intended for lightweight notifications or UI flavor.
        /// </summary>
        public void PlayOneShot(OneShot oneShot)
        {
            if (string.IsNullOrWhiteSpace(oneShot.Dialogue))
            {
                "PlayOneShot called with empty dialogue".LogInfo();
                return;
            }

            ShowConversationUI();

            if (oneShot.Audio != null)
            {
                EnsureAudioSource();
                if (_audioSource == null)
                {
                    "Audio clip provided but AudioSource could not be created.".LogWarning();
                }
                else
                {
                    _audioSource.PlayOneShot(oneShot.Audio);
                }
            }

            CleanupPreviousConversation();
            ResetUI();

            if (_dialogueText == null || _speakerNameText == null)
            {
                "UI references are not assigned (dialogue or speaker name is missing). Please assign _dialogueText and _speakerNameText.".LogWarning();
            }

            if (_oneShotRoutine != null)
            {
                StopCoroutine(_oneShotRoutine);
                _oneShotRoutine = null;
            }

            SubscribeAdvanceInput();
            _oneShotRoutine = StartCoroutine(RunOneShot(oneShot));
        }

        private void ShowConversationUI()
        {
            if (_uiFade != null)
            {
                _uiFade.Show();
            }
            else
            {
                if (!gameObject.activeInHierarchy)
                {
                    gameObject.SetActive(true);
                }
            }
        }

        private void HideConversationUI()
        {
            if (_uiFade != null)
            {
                _uiFade.Hide();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private IEnumerator RunOneShot(OneShot oneShot)
        {
            _activeOneShotLayer = CreateOneShotLayer(oneShot);

            OnAnyConversationStart?.Invoke();

            if (!_activeOneShotLayer.HasBeenParsed)
            {
                _activeOneShotLayer.ParseDialogue();
            }

            _activeOneShotLayer.StartLayer();
            UpdateUIForLayer(_activeOneShotLayer);

            var sceneFlow = FindFirstObjectByType<BattleSceneFlow>();
            sceneFlow?.ResetInterruptActivityTimer();
            if (sceneFlow != null)
            {
                sceneFlow.InterruptIsWaitingForPlayerInput = true;
            }

            bool completed = false;
            void OnComplete() => completed = true;

            var completionEvent = _activeOneShotLayer?.OnLayerComplete;
            if (completionEvent != null)
            {
                completionEvent.AddListener(OnComplete);
                yield return new WaitUntil(() => completed);
                completionEvent.RemoveListener(OnComplete);
            }
            else
            {
                "One-shot layer does not have a completion event; concluding immediately.".LogWarning();
            }

            if (sceneFlow != null)
            {
                sceneFlow.ResetInterruptActivityTimer();
                sceneFlow.InterruptIsWaitingForPlayerInput = false;
            }

            _activeOneShotLayer = null;
            _oneShotRoutine = null;
            UnsubscribeAdvanceInput();
            OnAnyConversationFinished?.Invoke();
            HideConversationUI();
        }

        private ConversationLayer CreateOneShotLayer(OneShot oneShot)
        {
            var layer = new ConversationLayer
            {
                Dialogue = oneShot.Dialogue,
                SpeakerDisplayName = oneShot.SpeakerName ?? string.Empty,
                ParsePronouns = false,
            };

            // Apply the portrait via the public API instead of using reflection.
            layer.SetPrimaryPortraitSprite(oneShot.Portrait);

            return layer;
        }
    }
}
