using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Brain.UI;
using Turnroot.GameSettings;
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

        private float PlayEffectsOnSelectedPrebattleCenter(GameObject preBattleInstance)
        {
            if (preBattleInstance == null)
            {
                return 0f;
            }

            if (!preBattleInstance.TryGetComponent<RadialMenu>(out var radialMenu))
            {
                return 0f;
            }

            var center = radialMenu.centerItem;
            if (center == null)
            {
                return 0f;
            }

#if COFFEE_UIEFFECTS
            var effectTweener =
                center.GetComponent<UIEffectTweener>()
                ?? center.GetComponentInChildren<UIEffectTweener>();
            if (effectTweener != null)
            {
                effectTweener.Play();
                // Do not block; play effect concurrently with fade
                return 0f;
            }
#endif

            return 0f;
        }

        private System.Collections.IEnumerator StartBattleCoroutine(
            MenuLocation preBattleMenuLocation,
            float delay
        )
        {
            _isTransitioning = true;

            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            var menuInstance = preBattleMenuLocation?.activeInstance;
            if (menuInstance == null)
            {
                _isTransitioning = false;
                yield break;
            }

            if (!menuInstance.TryGetComponent<UIFade>(out var uiFade))
            {
                // No fade component, proceed directly
                if (menuInstance.TryGetComponent<RadialMenu>(out var menu))
                {
                    menu.OnNavigate -= HandlePreBattleMenuNavigate;
                    menu.OnItemSelected -= HandlePreBattleMenuSelect;
                }

                if (menuInstance.TryGetComponent<MenuBase>(out var baseMenu))
                {
                    baseMenu.OnNavigate -= HandlePreBattleMenuNavigate;
                    baseMenu.OnItemSelected -= HandlePreBattleMenuSelect;
                }

                _brain.PublishPreBattleCompleted();
                Destroy(menuInstance);
                preBattleMenuLocation.activeInstance = null;
                _isTransitioning = false;
                yield break;
            }

            // Use existing coroutine for fade and cleanup
            yield return StartCoroutine(HandleFadeAndTransition(menuInstance, uiFade));
        }
    }
}
