using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Brain.UI;
using Turnroot.UI.Components;
using Turnroot.UI.Components.Menu;
using Turnroot.UI.Components.RadialMenu;
using UnityEngine;
#if COFFEE_UIEFFECTS
using Coffee.UIEffects;
#endif

namespace TurnrootFramework.Gameplay.Brain.Segments
{
    public partial class UiBrain : BrainComponent
    {
        #region PreBattle Menu Event Handlers

        public void HandlePreBattleMenuNavigate(MenuItemBase item)
        {
            // Delegate to the route handler for unified menu handling
            _routeHandler?.HandleMenuNavigate(item);
        }

        public void HandlePreBattleMenuSelect(MenuItemBase item)
        {
            // Delegate to the route handler for unified menu handling
            _routeHandler?.HandleMenuSelect(item);
        }

        #endregion

        #region PreBattle Settings and Helpers

        private void HandlePreBattleMenuSettings() => OpenMainGameSettingsMenu();

        private void HandlePreBattleMenuMap() => OpenPreBattleMapOverview();

        #endregion

        #region Battle Transition Management

        private System.Collections.IEnumerator HandleFadeAndTransition(
            GameObject menuInstance,
            UIFade uiFade
        )
        {
            // Start the fade
            uiFade.Hide();

            // Wait for fade duration (plus a small buffer) - use lerpTime property
            var fadeDuration = uiFade.lerpTime + 0.1f;
            yield return new WaitForSeconds(fadeDuration);

            // Clean up menu
            if (menuInstance != null)
            {
                if (menuInstance.TryGetComponent<RadialMenu>(out var radialMenu))
                {
                    radialMenu.OnNavigate -= HandlePreBattleMenuNavigate;
                    radialMenu.OnItemSelected -= HandlePreBattleMenuSelect;
                }

                if (menuInstance.TryGetComponent<MenuBase>(out var menu))
                {
                    menu.OnNavigate -= HandlePreBattleMenuNavigate;
                    menu.OnItemSelected -= HandlePreBattleMenuSelect;
                }
                Destroy(menuInstance);

                // Clear the active instance from the MenuLocation
                var preBattleMenuLocation = uiSettings?.GetPreBattleMenu();
                if (preBattleMenuLocation != null)
                {
                    preBattleMenuLocation.activeInstance = null;
                }
            }

            // Publish battle completion to transition states
            _brain.PublishPreBattleCompleted();
            _isTransitioning = false;
        }

        #endregion
    }
}
