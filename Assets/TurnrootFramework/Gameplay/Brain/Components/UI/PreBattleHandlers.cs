using Turnroot.Gameplay.Brain;
using Turnroot.UI.Components.RadialMenu;
using UnityEngine;
#if COFFEE_UIEFFECTS
using Coffee.UIEffects;
#endif

namespace TurnrootFramework.Gameplay.Brain.Segments
{
    public partial class UiBrain : BrainComponent
    {
        private void HandlePreBattleMenuNavigate(RadialMenuItemBase item)
        {
            // Handle navigation to item
        }

        private void HandlePreBattleMenuSelect(RadialMenuItemBase item)
        {
            // Handle selection of item
            if (item.IsCenter)
            {
                HandleStartBattleClick();
            }
            else
            {
                var radialMenu = PreBattleMenuInstance.GetComponent<RadialMenu>();
                var w = radialMenu.FindPreBattleOptionByName(item.ItemName);
                switch (w)
                {
                    case PrebattleOptions.Items:
                        // Open inventory UI
                        break;
                    case PrebattleOptions.Team:
                        // Open team management UI
                        break;
                    case PrebattleOptions.Settings:
                        // Open settings UI
                        break;
                    case PrebattleOptions.Skills:
                        // Open skills UI
                        break;
                    case PrebattleOptions.Map:
                        // Open conditions UI
                        break;
                    case PrebattleOptions.Support:
                        // Open support UI
                        break;
                    case PrebattleOptions.Withdraw:
                        // Handle withdraw action
                        break;
                }
            }
        }

        private void HandleStartBattleClick()
        {
            if (PreBattleMenuInstance == null || _isTransitioning)
            {
                return;
            }

            _isTransitioning = true;
            var menuInstance = PreBattleMenuInstance;

            if (!menuInstance.TryGetComponent<UIFade>(out var uiFade))
            {
                // No fade component, proceed directly
                var menu = menuInstance.GetComponent<RadialMenu>();
                if (menu != null)
                {
                    menu.OnNavigate -= HandlePreBattleMenuNavigate;
                    menu.OnItemSelected -= HandlePreBattleMenuSelect;
                }

                _brain.PublishPreBattleCompleted();
                Destroy(menuInstance);
                PreBattleMenuInstance = null;
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
                var radialMenu = menuInstance.GetComponent<RadialMenu>();
                if (radialMenu != null)
                {
                    radialMenu.OnNavigate -= HandlePreBattleMenuNavigate;
                    radialMenu.OnItemSelected -= HandlePreBattleMenuSelect;
                }

                Destroy(menuInstance);
                PreBattleMenuInstance = null;
            }

            // Publish battle completion to transition states
            _brain.PublishPreBattleCompleted();
            _isTransitioning = false;
        }
    }
}
