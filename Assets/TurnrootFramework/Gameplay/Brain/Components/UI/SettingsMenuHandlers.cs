using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Brain.UI;
using Turnroot.GameSettings;
using Turnroot.UI.Components;
using Turnroot.UI.Components.Menu;
using UnityEngine;

namespace TurnrootFramework.Gameplay.Brain.Segments
{
    public partial class UiBrain : BrainComponent
    {
        public void OpenMainGameSettingsMenu()
        {
            var settingsMenuLocation = uiSettings?.GetGameSettingsMenu();
            if (settingsMenuLocation == null)
            {
#if UNITY_EDITOR
                Debug.LogError("UiBrain: Game settings menu location not found");
#endif
                return;
            }

            if (settingsMenuLocation == null)
            {
#if UNITY_EDITOR
                Debug.LogError("UiBrain: Settings menu location not found");
#endif
                return;
            }

            // Guard: Return early if activeInstance already exists to prevent duplicates
            if (settingsMenuLocation.activeInstance != null)
            {
                return;
            }

            if (settingsMenuLocation.prefab == null)
            {
#if UNITY_EDITOR
                Debug.LogError("UiBrain: No prefab set for settings menu location");
#endif
                return;
            }

            settingsMenuLocation.activeInstance = Instantiate(settingsMenuLocation.prefab);
            if (!settingsMenuLocation.activeInstance.TryGetComponent<UIFade>(out var uiFade))
            {
                uiFade = settingsMenuLocation.activeInstance.AddComponent<UIFade>();
                uiFade.lerpTime = uiSettings.MenuFadeTime;
            }

            // Simplified: Just get MenuBase component since both ListMenu and GridMenu inherit from it
            if (settingsMenuLocation.activeInstance.TryGetComponent<MenuBase>(out var menu))
            {
                menu.uiBrain = this;
                menu.OnNavigate += HandleGameSettingsMenuNavigate;
                menu.OnItemSelected += HandleGameSettingsMenuSelect;
            }
        }

        public void HandleGameSettingsMenuNavigate(MenuItemBase item)
        {
#if UNITY_EDITOR
            Debug.Log($"UiBrain: Navigated to settings item: {item.ItemName}");
#endif
            // TODO: Handle settings menu navigation (highlighting, audio feedback, etc.)
        }

        public void HandleGameSettingsMenuSelect(MenuItemBase item)
        {
#if UNITY_EDITOR
            Debug.Log($"UiBrain: Selected settings item: {item.ItemName}");
#endif
            // TODO: Handle settings item selection based on item.ItemName
            // TODO: Open sub-menus or apply settings changes
        }
    }
}
