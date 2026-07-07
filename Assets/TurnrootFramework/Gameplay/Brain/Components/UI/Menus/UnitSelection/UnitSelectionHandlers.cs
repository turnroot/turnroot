using Turnroot.UI.Components;
using Turnroot.UI.Components.GridMenu;
using Turnroot.Utilities;
#if COFFEE_UIEFFECTS
using Coffee.UIEffects;
#endif

namespace Turnroot.Gameplay.Brain.Segments
{
    /// <summary>
    /// Handles unit selection and menu interactions for pre-battle setup.
    /// </summary>
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
                Brain.PublishUiPlayerIsTryingToUnselectLastUnit();
                return;
            }

            if (
                willSelect
                && unitColumns != null
                && unitColumns.SelectedCount >= unitColumns.MaxSelectedUnits
            )
            {
                "UiBrain: Cannot select more units - max reached".LogInfo();

                return;
            }

            // Apply toggle to UI
            item.IsSelectedForBattle = willSelect;
            // Apply toggle to the per-battle selection set when in pre-battle; otherwise mutate the instance flag
            var prep = Brain?.battleBrain.PreparationObject;
            if (prep != null && item.CharacterInstanceData != null)
            {
                prep.SetBattleSelected(item.CharacterInstanceData, willSelect);
            }
            else if (item.CharacterInstanceData != null)
            {
                item.CharacterInstanceData.IsSelectedForBattle = willSelect;
            }

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
                Brain.ltm.RememberBool(key, item.IsSelectedForBattle);
            }

            // Publish selection change to the rest of the system. When in pre-battle we already
            // called `SetBattleSelected(...)` which publishes, so avoid double-publishing here.
            if (item.CharacterInstanceData != null && prep == null)
            {
                Brain.PublishUnitSelectionChanged(
                    item.CharacterInstanceData,
                    item.IsSelectedForBattle
                );
            }
        }

        public void HandlePreBattleMenuSelect(MenuItemBase item) =>
            _routeHandler?.HandleMenuSelect(item);
    }
}
