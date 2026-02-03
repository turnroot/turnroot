using Turnroot.UI.Components;
using Turnroot.UI.Components.GridMenu;
using Turnroot.Utilities;
using UnityEngine;
#if COFFEE_UIEFFECTS
using Coffee.UIEffects;
#endif

namespace Turnroot.Gameplay.Brain.Segments
{
    public partial class UiBrain : BrainComponent
    {
        private void HandleUnitCellSelectionPreBattle(UnitCellGridMenuItem item)
        {
            if (!item.CanBeSelectedForBattle)
            {
                return;
            }

            var unitCell = item.gameObject;

            var unitColumns = unitCell.GetComponentInParent<UnitSelectionColumns>(true);

            var willSelect = !item.IsSelectedForBattle;

            // Prevent unselecting the only selected unit
            if (!willSelect && unitColumns != null && unitColumns.SelectedCount <= 1)
            {
                _brain.PublishUiPlayerIsTryingToUnselectLastUnit();
                return;
            }

            if (
                willSelect
                && unitColumns != null
                && unitColumns.SelectedCount >= unitColumns.MaxSelectedUnits
            )
            {
                TurnrootLogger.Log("UiBrain: Cannot select more units - max reached");

                return;
            }

            // Apply toggle
            item.IsSelectedForBattle = willSelect;
            item.CharacterInstanceData.IsSelectedForBattle = willSelect;

            var uf = new UtilityFunctions();
            var selectedT = uf.FindChildByTag(unitCell, "UnitCellSelected");
            if (selectedT != null)
            {
                var selectionIndicator = selectedT.gameObject;
                if (selectionIndicator != null)
                {
                    selectionIndicator.SetActive(item.IsSelectedForBattle);
                    if (item.IsSelectedForBattle)
                    {
#if COFFEE_UIEFFECTS
                        if (selectionIndicator.TryGetComponent<UIEffect>(out var uiEffect))
                        {
                            uiEffect.transitionRate = Random.Range(0, 1f);
                        }
#endif
                    }
                }
            }

            if (unitColumns != null)
            {
                unitColumns.RecomputeSelectedCount();
            }

            // Persist choice in LTM so it survives across menu opens
            var template = item?.CharacterInstanceData?.CharacterTemplate;
            var key = template != null ? LtmKeys.UnitSelectedForBattlePrefix + template.name : null;
            if (key != null)
            {
                _brain.ltm.RememberBool(key, item.IsSelectedForBattle);
            }

            if (item.CharacterInstanceData != null)
            {
                _brain.PublishUnitSelectionChanged(
                    item.CharacterInstanceData,
                    item.IsSelectedForBattle
                );
            }
        }

        public void HandlePreBattleMenuSelect(MenuItemBase item) =>
            _routeHandler?.HandleMenuSelect(item);
    }
}
