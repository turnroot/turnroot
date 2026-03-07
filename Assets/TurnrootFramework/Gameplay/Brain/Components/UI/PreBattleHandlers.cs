using Turnroot.GameSettings;
using Turnroot.UI.Components.GridMenu;
using Turnroot.UI.Components.Menu;
using Turnroot.UI.Components.RadialMenu;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;
#if COFFEE_UIEFFECTS
using Coffee.UIEffects;
#endif

namespace Turnroot.Gameplay.Brain.Segments
{
    /// <summary>
    /// Partial class containing pre-battle UI handlers, menu transitions, and unit selection logic.
    /// </summary>
    public partial class UiBrain : BrainComponent
    {
        public OperationResult HandlePreBattleUi()
        {
            var preBattleMenuLocation = GetValidatedMenuLocation(
                () => uiSettings?.GetPreBattleMenu(),
                "Pre-battle"
            );
            if (
                !ValidationHelper.ValidateNotNull(
                    "Pre-battle UI",
                    (preBattleMenuLocation, nameof(preBattleMenuLocation)),
                    (preBattleMenuLocation?.prefab, "preBattleMenuLocation.prefab")
                )
            )
            {
                return OperationResult.Failure("Pre-battle UI validation failed.");
            }

            preBattleMenuLocation.activeInstance = Instantiate(preBattleMenuLocation.prefab);

            // Notify subscribers that pre-battle prepare phase is occurring so systems like BattleBrain
            // can initialize pre-battle objects (e.g., BattlePreparationObject) before UI populates.

            _brain.PublishPreBattlePrepare();
            var uiFade = UIFadeCache.GetOrCreate(
                preBattleMenuLocation.activeInstance,
                uiSettings.MenuFadeTime
            );

            var menuStyle = preBattleMenuLocation.style;
            if (menuStyle is not MenuStyle.Pie and not MenuStyle.Grid)
            {
                menuStyle = MenuStyle.Grid;
            }
            if (menuStyle == MenuStyle.Pie)
            {
                if (
                    preBattleMenuLocation.activeInstance.TryGetComponent<RadialMenu>(
                        out var radialMenu
                    )
                )
                {
                    radialMenu.uiBrain = this;
                    radialMenu.OnItemSelected += HandlePreBattleMenuSelect;
                    // Disable input initially until scene transition completes
                    radialMenu.enabled = false;
                }
            }
            else if (menuStyle == MenuStyle.Grid)
            {
                if (
                    preBattleMenuLocation.activeInstance.TryGetComponent<MenuBase>(out var gridMenu)
                )
                {
                    gridMenu.uiBrain = this;
                    gridMenu.OnItemSelected += HandlePreBattleMenuSelect;
                    // Disable input initially until scene transition completes
                    gridMenu.enabled = false;
                }
            }

            // Hide the menu initially to prevent UI from showing through during scene transition
            preBattleMenuLocation.activeInstance.SetActive(false);

            // Subscribe to scene transition completed event to show menu when ready
            _brain.OnSceneTransitionCompleted += HandleSceneTransitionForPreBattleMenu;

            return OperationResult.Successful();
        }

        #region PreBattle Menu Event Handlers
        public void HandleUnitCellSelectionToggle(
            UnitCellGridMenuItem item,
            MenuLocation currentMenu
        )
        {
            // Compare by menu name rather than instance reference to avoid false negatives
            var currentMenuName = currentMenu?.menuName;
            if (
                currentMenuName != null
                && currentMenuName == uiSettings?.GetPrebattleUnitsMenu()?.menuName
            )
            {
                HandleUnitCellSelectionPreBattle(item);
                return;
            }

            // Fallback: if the transition manager indicates we're in Team menu context, treat as Units menu
            var menuType = _transitionManager?.CurrentMenuType;
            if (menuType == MenuType.Team)
            {
                HandleUnitCellSelectionPreBattle(item);
            }
        }

        #endregion

        #region Battle Transition Management
        public void HandleStartBattleClick()
        {
            var preBattleMenuLocation = uiSettings?.GetPreBattleMenu();
            if (preBattleMenuLocation?.activeInstance == null)
            {
                // Handle transition from submenu (e.g., Map menu)
                var currentMenuInstance = _menuTracker?.CurrentMenu?.activeInstance;
                if (currentMenuInstance != null)
                {
                    // Fade out the current submenu
                    var uiFade = UIFadeCache.GetOrCreate(
                        currentMenuInstance,
                        uiSettings.MenuFadeTime
                    );
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
            var fadeDuration = uiFade.lerpTime + (uiSettings?.MenuFadeBuffer ?? 0.1f);
            yield return new WaitForSeconds(fadeDuration);

            // Clean up menu
            if (menuInstance != null)
            {
                if (menuInstance.TryGetComponent<RadialMenu>(out var radialMenu))
                {
                    radialMenu.OnItemSelected -= HandlePreBattleMenuSelect;
                }

                if (menuInstance.TryGetComponent<MenuBase>(out var menu))
                {
                    menu.OnItemSelected -= HandlePreBattleMenuSelect;
                }

                // Unsubscribe from scene transition event if still subscribed
                _brain.OnSceneTransitionCompleted -= HandleSceneTransitionForPreBattleMenu;

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
            var fadeDuration = uiFade.lerpTime + (uiSettings?.MenuFadeBuffer ?? 0.1f);
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

        #region Scene Transition Handlers

        private void HandleSceneTransitionForPreBattleMenu(string sceneName, string displayName)
        {
            // Unsubscribe immediately to avoid multiple calls
            _brain.OnSceneTransitionCompleted -= HandleSceneTransitionForPreBattleMenu;

            var preBattleMenuLocation = uiSettings?.GetPreBattleMenu();
            if (preBattleMenuLocation?.activeInstance == null)
            {
                return;
            }

            // Show the menu now that scene transition is complete
            preBattleMenuLocation.activeInstance.SetActive(true);

            // Re-enable input
            var menuStyle = preBattleMenuLocation.style;
            if (menuStyle == MenuStyle.Pie)
            {
                if (
                    preBattleMenuLocation.activeInstance.TryGetComponent<RadialMenu>(
                        out var radialMenu
                    )
                )
                {
                    radialMenu.enabled = true;
                }
            }
            else if (menuStyle == MenuStyle.Grid)
            {
                if (
                    preBattleMenuLocation.activeInstance.TryGetComponent<MenuBase>(out var gridMenu)
                )
                {
                    gridMenu.enabled = true;
                }
            }

            // Trigger fade-in animation
            var uiFade = UIFadeCache.Get(preBattleMenuLocation.activeInstance);
            if (uiFade != null)
            {
                uiFade.Show();
            }
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

            var uiFade = UIFadeCache.Get(menuInstance);
            if (uiFade == null)
            {
                // No fade component, proceed directly
                if (menuInstance.TryGetComponent<RadialMenu>(out var menu))
                {
                    menu.OnItemSelected -= HandlePreBattleMenuSelect;
                }

                if (menuInstance.TryGetComponent<MenuBase>(out var baseMenu))
                {
                    baseMenu.OnItemSelected -= HandlePreBattleMenuSelect;
                }

                // Unsubscribe from scene transition event if still subscribed
                _brain.OnSceneTransitionCompleted -= HandleSceneTransitionForPreBattleMenu;

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
