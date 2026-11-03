using Turnroot.GameSettings;
using Turnroot.UI.Components.GridMenu;
using Turnroot.UI.Components.Menu;
using Turnroot.UI.Components.RadialMenu;
using Turnroot.Utilities;
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

            if (preBattleMenuLocation.activeInstance != null)
            {
                return OperationResult.Successful();
            }

            _battleStartRequested = false;
            preBattleMenuLocation.activeInstance = Instantiate(preBattleMenuLocation.prefab);

            // Notify subscribers that pre-battle prepare phase is occurring so systems like BattleBrain
            // can initialize pre-battle objects (e.g., BattlePreparationObject) before UI populates.

            _brain.PublishPreBattlePrepare();
            var uiFade = UIFadeCache.GetOrCreate(
                preBattleMenuLocation.activeInstance,
                uiSettings.MenuFadeTime
            );

            if (
                preBattleMenuLocation.activeInstance.TryGetComponent<RadialMenu>(out var radialMenu)
            )
            {
                radialMenu.uiBrain = this;
                radialMenu.OnItemSelected += HandlePreBattleMenuSelect;
            }

            // This handler runs after the PreBattle state is activated on scene arrival.
            preBattleMenuLocation.activeInstance.SetActive(true);
            if (
                preBattleMenuLocation.activeInstance.TryGetComponent<RadialMenu>(out radialMenu)
            )
            {
                radialMenu.enabled = true;
            }
            else if (
                preBattleMenuLocation.activeInstance.TryGetComponent<MenuBase>(out var gridMenu)
            )
            {
                gridMenu.enabled = true;
            }
            uiFade.Show();

            return OperationResult.Successful();
        }

        #region PreBattle Menu Event Handlers
        public void HandleUnitCellSelectionToggle(UnitCellGridMenuItem item, MenuEntry currentMenu)
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
            if (_isTransitioning || _battleStartRequested)
            {
                "Start battle ignored: already transitioning.".LogInfo();
                return;
            }

            _battleStartRequested = true;
            var preBattleMenuLocation = uiSettings?.GetPreBattleMenu();
            var activeMenu =
                preBattleMenuLocation?.activeInstance != null
                    ? preBattleMenuLocation
                    : _menuTracker?.CurrentMenu;

            if (activeMenu?.activeInstance == null)
            {
                _isTransitioning = true;
                if (_transitionManager != null)
                {
                    StartCoroutine(_transitionManager.TransitionToBattle(null));
                }
                else
                {
                    _isTransitioning = false;
                    _brain.PublishPreBattleCompleted();
                }
                return;
            }

            float effectDelay =
                activeMenu == preBattleMenuLocation
                    ? PlayEffectsOnSelectedPrebattleCenter(activeMenu.activeInstance)
                    : 0f;
            _isTransitioning = true;
            StartCoroutine(_transitionManager.TransitionToBattle(activeMenu, effectDelay));
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

    }
}
