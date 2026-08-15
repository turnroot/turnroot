using NaughtyAttributes;
using Turnroot.Gameplay.Brain;
using Turnroot.GameSettings;
using Turnroot.UI;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using Turnroot.Utilities.SceneFlows;
using UnityEngine;
using UnityEngine.Events;

namespace Turnroot.Gameplay.GameStart
{
    /// <summary>
    /// Base class for the title-screen / new-game flow.
    /// Handles save-file selection, pronouns, difficulty, permadeath, and scene transition.
    /// <br/><br/>
    /// Extend this in your project to add game-specific steps such as star gifts, avatar
    /// creation, birthday selection, or any other custom character-creation step.
    /// <br/><br/>
    /// Override <see cref="DispatchModeInput"/> to handle additional project-specific input
    /// modes, calling <c>base.DispatchModeInput(action)</c> at the end so the base handles
    /// the built-in modes. Override virtual hooks such as <see cref="OnPronounsConfirmed"/>
    /// or <see cref="OnNewSaveFileChosen"/> to react to player choices.
    /// </summary>
    [RequireComponent(typeof(UiInputProvider))]
    public abstract partial class GameStartManagerBase : MonoBehaviour
    {
        #region Serialized fields

        [BoxGroup("Input")]
        public UiInputProvider InputProvider;

        [BoxGroup("UI Choices"), HorizontalLine(color: EColor.Blue)]
        public SaveFileSlotUI[] SaveFileSlots;

        [BoxGroup("UI Choices")]
        public UiChoice[] PronounsUiManagers;

        [BoxGroup("UI Choices")]
        public UiChoice[] DifficultyUiManagers;

        [BoxGroup("UI Choices")]
        public UiChoice[] PermadeathUiManagers;

        [BoxGroup("UI Fades"), HorizontalLine(color: EColor.Indigo)]
        public UIFade EntryFade;

        [BoxGroup("UI Fades")]
        public UIFade SaveFilesFade;

        [BoxGroup("UI Fades")]
        public UIFade PronounsFade;

        [BoxGroup("UI Fades")]
        public UIFade DifficultyFade;

        [BoxGroup("UI Fades")]
        public UIFade PermadeathFade;

        [BoxGroup("UI Fades")]
        public UIFade LoadingFade;

        [BoxGroup("UI Fades")]
        public UiFillDriver LoadingFillDriver;

        [BoxGroup("Audio"), HorizontalLine(color: EColor.Orange)]
        public AudioSource StartFx;

        [BoxGroup("Audio")]
        public AudioClip StartClip;

        [BoxGroup("Audio")]
        public AudioClip TitleMusic;

        #endregion

        #region Protected state

        protected SaveFileBrain saveFileBrain;
        protected SceneFlowBrain sceneFlowBrain;
        protected LoadingController loadingController;
        protected LoadingScreenController LoadingScreen;

        protected int currentIndex = 0;
        protected string _currentInputMode = InputModeNames.None;

        #endregion

        #region Events

        public UnityEvent OnStartLoadingNextScene = new();

        #endregion

        #region Unity lifecycle

        protected virtual void Start()
        {
            if (InputProvider != null)
            {
                InputProvider.OnInput += HandleInput;
            }
            else
            {
                "GameStartManagerBase: InputProvider reference is null.".LogWarning(
                    "GameStartManagerBase"
                );
            }

            saveFileBrain = FindFirstObjectByType<SaveFileBrain>();
            saveFileBrain.LoadSaveFiles();

            sceneFlowBrain = saveFileBrain.Brain.sceneFlowBrain;
            loadingController = saveFileBrain.Brain.GetComponent<LoadingController>();

            if (LoadingScreen == null)
            {
                LoadingScreen = FindFirstObjectByType<LoadingScreenController>();
            }

            if (LoadingScreen == null && (LoadingFade != null || LoadingFillDriver != null))
            {
                LoadingScreen = gameObject.AddComponent<LoadingScreenController>();
                LoadingScreen.Fade = LoadingFade;
                LoadingScreen.FillDriver = LoadingFillDriver;
            }

            saveFileBrain.Brain.OnSceneReadyToDisplay += HandleSceneReadyToDisplay;
            sceneFlowBrain.SetCurrentScene(
                GameplayGeneralSettings.Instance?.StartingSceneId ?? "scene_1"
            );
            InitializeSaveFiles();

            saveFileBrain.Brain.OnUpdateSaveFileName += OnSaveFileNameChanged;
            saveFileBrain.OnActiveSaveFilePlaytimeUpdated += OnSaveFilePlaytimeUpdated;

            if (saveFileBrain.Brain?.audioBrain != null && TitleMusic != null)
            {
                saveFileBrain.Brain.audioBrain.SetMusic(TitleMusic);
            }
        }

        protected virtual void OnDestroy()
        {
            if (InputProvider != null)
            {
                InputProvider.OnInput -= HandleInput;
            }

            if (saveFileBrain?.Brain != null)
            {
                saveFileBrain.Brain.OnSceneReadyToDisplay -= HandleSceneReadyToDisplay;
                saveFileBrain.Brain.OnUpdateSaveFileName -= OnSaveFileNameChanged;
            }

            if (saveFileBrain != null)
            {
                saveFileBrain.OnActiveSaveFilePlaytimeUpdated -= OnSaveFilePlaytimeUpdated;
            }
        }

        #endregion

        #region Mode management

        protected void SetInputMode(string mode)
        {
            _currentInputMode = mode;
            currentIndex = 0;
        }

        public void ShowSaveFileSelection()
        {
            SaveFilesFade?.Show();
            SetInputMode(InputModeNames.SaveFiles);
        }

        public void ShowAvatarPronounSelection()
        {
            PronounsFade?.Show();
            SetInputMode(InputModeNames.Pronouns);
        }

        public void ShowDifficultySelection()
        {
            DifficultyFade?.Show();
            SetInputMode(InputModeNames.Difficulty);
        }

        public void ShowPermadeathSelection()
        {
            PermadeathFade?.Show();
            SetInputMode(InputModeNames.Permadeath);
        }

        #endregion

        #region Scene transition

        public void StartLoadingNextScene()
        {
            enabled = false;
            OnStartLoadingNextScene.Invoke();

            if (LoadingScreen != null)
            {
                LoadingScreen.Show();
            }
            else
            {
                LoadingFade?.Show();
            }

            var availableScenes = sceneFlowBrain.GetAvailableScenes();
            if (availableScenes == null || availableScenes.Count == 0)
            {
                "GameStartManagerBase: No available scenes to transition to!".LogError(
                    "GameStartManagerBase"
                );
                LoadingFade?.Hide();
                return;
            }

            sceneFlowBrain.TransitionToScene(availableScenes[0].sceneId);
        }

        public void CheckLoadingProgress(float progress)
        {
            if (LoadingScreen != null)
            {
                LoadingScreen.SetProgress(progress);
            }
            else
            {
                LoadingFillDriver?.SetAmount(progress);
            }
        }

        private void HandleSceneReadyToDisplay(string sceneName, string displayName) =>
            MoveToNextSceneAndUnloadThisOne();

        public void MoveToNextSceneAndUnloadThisOne() => LoadingFade?.Hide();

        #endregion

        #region Event handlers

        private void OnSaveFileNameChanged(string _)
        {
            if (_currentInputMode == InputModeNames.SaveFiles)
            {
                InitializeSaveFiles(preserveIndex: true);
            }
            else
            {
                InitializeSaveFiles();
            }
        }

        private void OnSaveFilePlaytimeUpdated(int _)
        {
            if (_currentInputMode == InputModeNames.SaveFiles)
            {
                InitializeSaveFiles(preserveIndex: true);
            }
            else
            {
                InitializeSaveFiles();
            }
        }

        #endregion

        #region Virtual hooks

        /// <summary>
        /// Called when the player confirms an empty save-file slot.
        /// Override to show the character-creation flow (name, pronouns, star gift, etc.).
        /// Default implementation does nothing — the flow is typically driven by Unity Events
        /// on UI components or animations.
        /// </summary>
        protected virtual void OnNewSaveFileChosen() { }

        /// <summary>
        /// Called when the player confirms an existing, named save file.
        /// Default: start loading the next scene.
        /// </summary>
        protected virtual void OnExistingSaveFileChosen() => StartLoadingNextScene();

        /// <summary>
        /// Called after the player confirms their pronouns choice.
        /// <paramref name="choiceIndex"/> is the 0-based index into <see cref="PronounsUiManagers"/>.
        /// Override to apply the choice to your character data (e.g. 0=she, 1=he, 2=they).
        /// </summary>
        protected virtual void OnPronounsConfirmed(int choiceIndex) { }

        #endregion
    }

    /// <summary>
    /// String constants for the input modes built into <see cref="GameStartManagerBase"/>.
    /// Define additional constants in your project subclass for game-specific modes.
    /// </summary>
    public static class InputModeNames
    {
        public const string None = nameof(None);
        public const string SaveFiles = nameof(SaveFiles);
        public const string Pronouns = nameof(Pronouns);
        public const string Difficulty = nameof(Difficulty);
        public const string Permadeath = nameof(Permadeath);

        /// <summary>On-screen text keyboard for name entry.</summary>
        public const string Keyboard = nameof(Keyboard);

        /// <summary>Appearance/body type scroller.</summary>
        public const string Appearance = nameof(Appearance);
    }
}
