using Turnroot.Gameplay.PlayerSettings;
using Turnroot.UI;
using Turnroot.Utilities;
using UnityEngine.InputSystem;

namespace Turnroot.Gameplay.GameStart
{
    public abstract partial class GameStartManagerBase
    {
        /// <summary>
        /// Main input entry point, subscribed to <see cref="UiInputProvider.OnInput"/>.
        /// Performs null guard, quit-combo check, then delegates to
        /// <see cref="DispatchModeInput"/> for mode-specific handling.
        /// </summary>
        protected virtual void HandleInput(string action)
        {
            if (InputProvider == null)
            {
                "InputProvider not found! Cannot handle input.".LogError("GameStartManagerBase");
                return;
            }

            if (TryQuitFromInputCombo(action))
            {
                return;
            }

            DispatchModeInput(action);
        }

        /// <summary>
        /// Dispatches the action to the handler for the current input mode.
        /// Override in your subclass to handle project-specific modes — handle them first,
        /// then call <c>base.DispatchModeInput(action)</c> so the built-in modes still work.
        /// </summary>
        protected virtual void DispatchModeInput(string action)
        {
            switch (_currentInputMode)
            {
                case InputModeNames.SaveFiles:
                    HandleSaveFileInput(action);
                    return;
                case InputModeNames.Pronouns:
                    HandlePronounsInput(action);
                    return;
                case InputModeNames.Difficulty:
                    HandleDifficultyInput(action);
                    return;
                case InputModeNames.Permadeath:
                    HandlePermadeathInput(action);
                    return;
            }

            // Entry screen: any confirm action advances to save file selection
            if (
                action
                is InputActionConstants.Select
                    or InputActionConstants.Start
                    or InputActionConstants.Submit
                    or InputActionConstants.Confirm
            )
            {
                if (EntryFade != null && EntryFade.Visible)
                {
                    if (StartFx != null && StartClip != null)
                    {
                        StartFx.PlayOneShot(StartClip);
                    }

                    EntryFade.Hide();

                    if (saveFileBrain == null)
                    {
                        "SaveFileBrain not found!".LogError("GameStartManagerBase");
                        return;
                    }

                    SaveFilesFade?.Show();
                    SetInputMode(InputModeNames.SaveFiles);
                }
            }
        }

        private bool TryQuitFromInputCombo(string action)
        {
            if (
                action
                is not InputActionConstants.Select
                    and not InputActionConstants.Start
                    and not InputActionConstants.Submit
                    and not InputActionConstants.Confirm
                    and not InputActionConstants.Cancel
                    and not InputActionConstants.Back
            )
            {
                return false;
            }

            if (
                IsPressed(UiChoice.BackAction)
                && (
                    IsPressed(UIInputActionDefaults.ToggleDetails)
                    || IsPressed(UiChoice.StartAction)
                )
            )
            {
                QuitApplication();
                return true;
            }

            return false;
        }

        private static bool IsPressed(InputAction action) => action != null && action.IsPressed();

        private static void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }

        private void HandlePronounsInput(string action)
        {
            if (_currentInputMode != InputModeNames.Pronouns || PronounsUiManagers == null)
            {
                return;
            }

            InputProvider.Navigate(
                action,
                PronounsUiManagers,
                ref currentIndex,
                PronounsUiManagers.Length,
                () =>
                {
                    OnPronounsConfirmed(currentIndex);
                    SetInputMode(InputModeNames.None);
                    enabled = false;
                    PronounsFade?.Hide();
                }
            );
        }

        private void HandleDifficultyInput(string action)
        {
            if (_currentInputMode != InputModeNames.Difficulty || DifficultyUiManagers == null)
            {
                return;
            }

            InputProvider.Navigate(
                action,
                DifficultyUiManagers,
                ref currentIndex,
                DifficultyUiManagers.Length,
                () =>
                {
                    GameplayPlayerSettings.Instance.GameDifficulty = currentIndex switch
                    {
                        0 => GameplayPlayerSettings.DifficultyLevel.Easy,
                        1 => GameplayPlayerSettings.DifficultyLevel.Normal,
                        2 => GameplayPlayerSettings.DifficultyLevel.Hard,
                        3 => GameplayPlayerSettings.DifficultyLevel.Extreme,
                        _ => GameplayPlayerSettings.Instance.GameDifficulty,
                    };
                    SetInputMode(InputModeNames.None);
                    enabled = false;
                    DifficultyFade?.Hide();
                }
            );
        }

        private void HandlePermadeathInput(string action)
        {
            if (_currentInputMode != InputModeNames.Permadeath || PermadeathUiManagers == null)
            {
                return;
            }

            InputProvider.Navigate(
                action,
                PermadeathUiManagers,
                ref currentIndex,
                2,
                () =>
                {
                    GameplayPlayerSettings.Instance.Permadeath = currentIndex == 0;
                    SetInputMode(InputModeNames.None);
                    enabled = false;
                    PermadeathFade?.Hide();
                }
            );
        }
    }
}
