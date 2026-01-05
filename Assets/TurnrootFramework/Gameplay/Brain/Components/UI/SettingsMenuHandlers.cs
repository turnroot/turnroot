using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.PlayerSettings;
using Turnroot.UI.Components.Menu;
using Turnroot.UI.Components.Menu.Submenu;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TurnrootFramework.Gameplay.Brain.Segments
{
    public partial class UiBrain : BrainComponent
    {
        #region Menu Input Setup

        private void SetupMenuInputActions(MenuBase menu)
        {
            // Force refresh menu items to make sure they're properly detected
            menu.RefreshMenuItems();

            // Create new InputActions with proper bindings for keyboard navigation
            if (menu.navigateUpAction == null || menu.navigateUpAction.bindings.Count == 0)
            {
                menu.navigateUpAction = new UnityEngine.InputSystem.InputAction(
                    "NavigateUp",
                    UnityEngine.InputSystem.InputActionType.Button
                );
                menu.navigateUpAction.AddBinding("<Keyboard>/w");
                menu.navigateUpAction.AddBinding("<Keyboard>/upArrow");
            }

            if (menu.navigateDownAction == null || menu.navigateDownAction.bindings.Count == 0)
            {
                menu.navigateDownAction = new UnityEngine.InputSystem.InputAction(
                    "NavigateDown",
                    UnityEngine.InputSystem.InputActionType.Button
                );
                menu.navigateDownAction.AddBinding("<Keyboard>/s");
                menu.navigateDownAction.AddBinding("<Keyboard>/downArrow");
            }

            if (menu.selectAction == null || menu.selectAction.bindings.Count == 0)
            {
                menu.selectAction = new UnityEngine.InputSystem.InputAction(
                    "Select",
                    UnityEngine.InputSystem.InputActionType.Button
                );
                menu.selectAction.AddBinding("<Keyboard>/enter");
                menu.selectAction.AddBinding("<Keyboard>/space");
            }

            // Enable the actions
            menu.navigateUpAction?.Enable();
            menu.navigateDownAction?.Enable();
            menu.selectAction?.Enable();
        }

        #endregion

        #region Settings UI Bindings

        private void SetupSettingsUIBindings(GameObject menuInstance)
        {
            if (menuInstance == null)
            {
                return;
            }

            var gamewideContext = _brain.GetComponent<GamewideContextBrain>();
            if (gamewideContext?.PlayerSettings == null)
            {
                return;
            }

            // Find all PanelRow components and set up their events
            var panelRows = menuInstance.GetComponentsInChildren<PanelRow>();
            foreach (var panelRow in panelRows)
            {
                SetupPanelRowSettingsBinding(panelRow, gamewideContext);
            }

            // Also look for SimpleToggle components directly (not in PanelRows)
            var directToggles = menuInstance.GetComponentsInChildren<SimpleToggle>();
            foreach (var toggle in directToggles)
            {
                SetupDirectToggleBinding(toggle, gamewideContext);
            }

            // Also look for Slider components directly (not in PanelRows)
            var directSliders = menuInstance.GetComponentsInChildren<Slider>();
            foreach (var slider in directSliders)
            {
                SetupDirectSliderBinding(slider, gamewideContext);
            }
        }

        private void SetupPanelRowSettingsBinding(
            PanelRow panelRow,
            GamewideContextBrain gamewideContext
        )
        {
            if (panelRow == null)
            {
                return;
            }

            var settings = gamewideContext.PlayerSettings;

            // Handle sliders in the panel row
            if (panelRow.rowType == PanelRow.RowType.Slider && panelRow.sliderComponent != null)
            {
                string settingName = panelRow.labelText?.text?.Trim();
                if (!string.IsNullOrEmpty(settingName))
                {
                    SetupSliderBinding(
                        panelRow.sliderComponent,
                        settingName,
                        settings,
                        gamewideContext
                    );
                }
            }

            // Handle ALL toggles in the panel row independently
            if (panelRow.rowType == PanelRow.RowType.Toggles && panelRow.toggleComponents != null)
            {
                foreach (var toggle in panelRow.toggleComponents)
                {
                    if (toggle != null)
                    {
                        SetupDirectToggleBinding(toggle, gamewideContext);
                    }
                }
            }
        }

        private void SetupSliderBinding(
            Slider slider,
            string settingName,
            GameplayPlayerSettings settings,
            GamewideContextBrain gamewideContext
        )
        {
            if (slider == null)
            {
                return;
            }

            // Initialize slider value from settings
            switch (settingName.ToLower())
            {
                case "brightness":
                    slider.value = settings.Brightness;
                    break;
                case "gamma":
                    slider.value = settings.Gamma;
                    break;
                case "quality":
                    slider.value = settings.Quality;
                    break;
                default:
                    return; // Unknown slider setting
            }

            // Set up change listener
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(value =>
            {
                gamewideContext.UpdatePlayerSetting(settingName, value);
            });
        }

        private void SetupDirectToggleBinding(
            SimpleToggle toggle,
            GamewideContextBrain gamewideContext
        )
        {
            if (toggle == null || gamewideContext?.PlayerSettings == null)
            {
                return;
            }

            // Get the setting name from the GameObject name
            string settingName = toggle.gameObject.name;
            var settings = gamewideContext.PlayerSettings;

            // Initialize toggle value from settings
            switch (settingName.ToLower())
            {
                case "tutorialprompts":
                    toggle.isOn = settings.TutorialPrompts;
                    break;
                case "subtitles":
                    toggle.isOn = settings.Subtitles;
                    break;
                case "bloom":
                    toggle.isOn = settings.Bloom;
                    break;
                case "depthoffield":
                    toggle.isOn = settings.DepthOfField;
                    break;
                case "lensflare":
                    toggle.isOn = settings.LensFlare;
                    break;
                case "animatedcameramovement":
                    toggle.isOn = settings.AnimatedCameraMovement;
                    break;
                default:
                    return; // Unknown toggle setting
            }

            // Set up change listener
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener(value =>
            {
                gamewideContext.UpdatePlayerSetting(settingName, value);
            });
        }

        private void SetupDirectSliderBinding(Slider slider, GamewideContextBrain gamewideContext)
        {
            if (slider == null || gamewideContext?.PlayerSettings == null)
            {
                return;
            }

            // Get the setting name from the GameObject name
            string settingName = slider.gameObject.name;
            var settings = gamewideContext.PlayerSettings;

            // Initialize slider value from settings
            switch (settingName.ToLower())
            {
                case "brightness":
                    slider.value = settings.Brightness;
                    break;
                case "gamma":
                    slider.value = settings.Gamma;
                    break;
                case "quality":
                    slider.value = settings.Quality;
                    break;
                default:
                    return; // Unknown slider setting
            }

            // Set up change listener
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(value =>
            {
                gamewideContext.UpdatePlayerSetting(settingName, value);
            });
        }

        #endregion
    }
}
