using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Brain.UI;
using Turnroot.UI.Components;
using Turnroot.UI.Components.ListMenu;
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
        public void HandlePreBattleMenuNavigate(MenuItemBase item)
        {
            // Handle navigation to item
        }

        public void HandlePreBattleMenuSelect(MenuItemBase item)
        {
            // Handle selection of item
            if (item.IsCenter)
            {
                HandleStartBattleClick();
            }
            else
            {
                var preBattleMenuLocation = uiSettings?.GetPreBattleMenu();
                var radialMenu = preBattleMenuLocation?.activeInstance?.GetComponent<RadialMenu>();
                if (radialMenu != null)
                {
                    var selectedOption = radialMenu.FindPreBattleOptionByName(item.ItemName);
                    switch (selectedOption)
                    {
                        case PrebattleOptions.Items:
                            // TODO: inventory UI
                            break;
                        case PrebattleOptions.Team:
                            // TODO: team management UI
                            break;
                        case PrebattleOptions.Settings:
                            HandlePreBattleMenuSettings();
                            break;
                        case PrebattleOptions.Skills:
                            // TODO: skills UI
                            break;
                        case PrebattleOptions.Map:
                            // TODO: map UI
                            break;
                        case PrebattleOptions.Support:
                            // TODO: support UI
                            break;
                        case PrebattleOptions.Withdraw:
                            // TODO: Handle withdraw action
                            break;
                    }
                }
            }
        }

        private void HandlePreBattleMenuSettings()
        {
            OpenMainGameSettingsMenu();
        }

        private void HandleStartBattleClick()
        {
            var preBattleMenuLocation = uiSettings?.GetPreBattleMenu();
            if (preBattleMenuLocation?.activeInstance == null || _isTransitioning)
            {
                return;
            }

            _isTransitioning = true;
            var menuInstance = preBattleMenuLocation.activeInstance;

            if (!menuInstance.TryGetComponent<UIFade>(out var uiFade))
            {
                // No fade component, proceed directly
                if (menuInstance.TryGetComponent<RadialMenu>(out var menu))
                {
                    menu.OnNavigate -= HandlePreBattleMenuNavigate;
                    menu.OnItemSelected -= HandlePreBattleMenuSelect;
                }

                _brain.PublishPreBattleCompleted();
                Destroy(menuInstance);
                preBattleMenuLocation.activeInstance = null;
                _isTransitioning = false;
                return;
            }

#if COFFEE_UIEFFECTS
            var menuForEffect = menuInstance.GetComponent<RadialMenu>();
            var effectTweener = menuForEffect?.centerItem?.GetComponent<UIEffectTweener>();
#endif

            // Use coroutine approach since OnHidden callback wasn't working reliably
            StartCoroutine(HandleFadeAndTransition(menuInstance, uiFade));

#if COFFEE_UIEFFECTS
            effectTweener?.Play();
#endif
        }

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
    }
}
