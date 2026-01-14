using Turnroot.Gameplay.Brain;
using Turnroot.UI.Components;
using Turnroot.UI.Components.GridMenu;
using UnityEngine;
#if COFFEE_UIEFFECTS
using Coffee.UIEffects;
#endif

namespace TurnrootFramework.Gameplay.Brain.Segments
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

            // Determine whether this toggle would select or deselect
            var willSelect = !item.IsSelectedForBattle;

            // Prevent unselecting the only selected unit
            if (!willSelect && unitColumns != null && unitColumns.SelectedCount <= 1)
            {
                // TODO: Fire brain event, provide UI feedback
                return;
            }

            // If attempting to select but we've reached the maximum, ignore
            if (
                willSelect
                && unitColumns != null
                && unitColumns.SelectedCount >= unitColumns.MaxSelectedUnits
            )
            {
#if UNITY_EDITOR
                Debug.Log("UiBrain: Cannot select more units - max reached");
#endif
                return;
            }

            // Apply toggle
            item.IsSelectedForBattle = willSelect;
            item.CharacterInstanceData.IsSelectedForBattle = willSelect;

            var uf = new Turnroot.Utilities.UtilityFunctions();
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

            // Update SelectedCount on parent columns and persist selection to LTM
            if (unitColumns != null)
            {
                // Recompute authoritative count to avoid drift
                unitColumns.RecomputeSelectedCount();
            }

            // Persist choice in LTM so it survives across menu opens
            var unitName =
                item?.ItemName?.StartsWith("UnitCell_") == true
                    ? item.ItemName.Substring("UnitCell_".Length)
                    : item?.ItemName ?? "";
            var key = LtmKeys.UnitSelectedForBattlePrefix + unitName;

            _brain.ltm.RememberBool(key, item.IsSelectedForBattle);
        }

        // Removed unit cell handling for positioning, not actually using
        // unit cells for this

        public void HandlePreBattleMenuSelect(MenuItemBase item) =>
            // Delegate to the route handler for unified menu handling
            _routeHandler?.HandleMenuSelect(item);
    }
}
