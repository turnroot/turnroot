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
        public void HandlePreBattleUi()
        {
            var preBattleMenuLocation = GetValidatedMenuLocation(
                () => uiSettings?.GetPreBattleMenu(),
                "Pre-battle"
            );
            if (preBattleMenuLocation == null)
            {
                return;
            }

            // Guard: Return early if activeInstance already exists to prevent duplicates
            if (preBattleMenuLocation.activeInstance != null)
            {
                return;
            }

            if (preBattleMenuLocation.prefab == null)
            {
#if UNITY_EDITOR
                Debug.LogError("UiBrain: No prefab set for pre-battle menu location");
#endif
                return;
            }

            Debug.Log(
                $"UiBrain: Creating pre-battle menu instance from prefab {preBattleMenuLocation.prefab?.name}"
            );
            preBattleMenuLocation.activeInstance = Instantiate(preBattleMenuLocation.prefab);
            Debug.Log(
                $"UiBrain: Created pre-battle instance {preBattleMenuLocation.activeInstance?.name}"
            );

            // Notify subscribers that pre-battle prepare phase is occurring so systems like BattleBrain
            // can initialize pre-battle objects (e.g., BattlePreparationObject) before UI populates.
            Debug.Log("UiBrain: Publishing PreBattlePrepare event");
            _brain.PublishPreBattlePrepare();
            if (!preBattleMenuLocation.activeInstance.TryGetComponent<UIFade>(out var uiFade))
            {
                uiFade = preBattleMenuLocation.activeInstance.AddComponent<UIFade>();
                uiFade.lerpTime = uiSettings.MenuFadeTime;
            }

            var menuStyle = preBattleMenuLocation.style;
            if (menuStyle == MenuStyle.Pie)
            {
                if (
                    preBattleMenuLocation.activeInstance.TryGetComponent<RadialMenu>(
                        out var radialMenu
                    )
                )
                {
                    radialMenu.uiBrain = this;
                    radialMenu.OnNavigate += HandlePreBattleMenuNavigate;
                    radialMenu.OnItemSelected += HandlePreBattleMenuSelect;
                    Debug.Log(
                        $"UiBrain: Attached prebattle handlers to radial instance {preBattleMenuLocation.activeInstance?.name}"
                    );
                }
            }
            else if (menuStyle == MenuStyle.Filmstrip)
            {
                // TODO: Set up filmstrip prebattle menu handling
            }
            else if (menuStyle == MenuStyle.List)
            {
                if (
                    preBattleMenuLocation.activeInstance.TryGetComponent<MenuBase>(out var listMenu)
                )
                {
                    listMenu.uiBrain = this;
                    listMenu.OnNavigate += HandlePreBattleMenuNavigate;
                    listMenu.OnItemSelected += HandlePreBattleMenuSelect;
                }
            }
            else if (menuStyle == MenuStyle.Grid)
            {
                // TODO: Set up grid prebattle menu handling
            }
        }

        #region PreBattle Menu Event Handlers

        public void HandlePreBattleMenuNavigate(MenuItemBase item) =>
            // Delegate to the route handler for unified menu handling
            _routeHandler?.HandleMenuNavigate(item);

        public void HandlePreBattleMenuSelect(MenuItemBase item)
        {
#if UNITY_EDITOR
            Debug.Log($"UiBrain: HandlePreBattleMenuSelect received item: {item?.ItemName}");
#endif
            // Delegate to the route handler for unified menu handling
            _routeHandler?.HandleMenuSelect(item);
        }

        #endregion

        #region Battle Transition Management
        public void HandleStartBattleClick()
        {
            var preBattleMenuLocation = uiSettings?.GetPreBattleMenu();
            if (preBattleMenuLocation?.activeInstance == null)
            {
#if UNITY_EDITOR
                Debug.Log("UiBrain: Start battle called outside of pre-battle radial menu");
#endif
                // Handle transition from submenu (e.g., Map menu)
                var currentMenuInstance = _menuTracker?.CurrentMenu?.activeInstance;
                if (currentMenuInstance != null)
                {
                    // Fade out the current submenu
                    if (!currentMenuInstance.TryGetComponent<UIFade>(out var uiFade))
                    {
                        uiFade = currentMenuInstance.AddComponent<UIFade>();
                        uiFade.lerpTime = uiSettings.MenuFadeTime;
                    }
                    StartCoroutine(HandleFadeAndTransitionForSubmenu(currentMenuInstance, uiFade));
                }
                else
                {
                    // No menu to fade, transition directly
                    _brain.PublishPreBattleCompleted();
                }
                return;
            }

            if (_isTransitioning)
            {
                return;
            }

            // Play any center item effects (UITweener/UIEffect/etc.) before starting transition
            float effectDelay = PlayEffectsOnSelectedPrebattleCenter(
                preBattleMenuLocation.activeInstance
            );

            // Start a coroutine that waits for effect to play then transitions to battle
            StartCoroutine(StartBattleCoroutine(preBattleMenuLocation, effectDelay));
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

        private System.Collections.IEnumerator HandleFadeAndTransitionForSubmenu(
            GameObject menuInstance,
            UIFade uiFade
        )
        {
            _isTransitioning = true;

            // Start the fade
            uiFade.Hide();

            // Wait for fade duration (plus a small buffer) - use lerpTime property
            var fadeDuration = uiFade.lerpTime + 0.1f;
            yield return new WaitForSeconds(fadeDuration);

            // Clean up menu
            if (menuInstance != null)
            {
                Destroy(menuInstance);
            }

            // Clear the menu tracker since we're leaving the menu system
            _menuTracker?.Clear();

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
