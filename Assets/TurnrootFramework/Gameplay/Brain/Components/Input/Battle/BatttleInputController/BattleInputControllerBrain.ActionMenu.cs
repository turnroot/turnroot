using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components.Battle;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.UI.Components.ListMenu;
using Turnroot.UI.Components.Menu;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.Brain
{
    public partial class BattleInputControllerBrain : BrainComponent
    {
        #region Action Menu Management

        private OperationResult ShowActionMenu()
        {
            Brain.battleBrain.IsInputEnabled = false;

            var menuEntry = Brain.uiBrain.battleActionSelectMenuLocation;
            var validation = OperationResultGuards.RequireNotNull(
                menuEntry?.prefab,
                "BattleActionSelectMenu prefab"
            );
            if (!validation.Success)
            {
                return validation;
            }

            CloseActionMenu();

            _currentActionMenu = Instantiate(menuEntry.prefab);
            var battleSelectAction =
                _currentActionMenu.GetComponent<UI.Components.BattleSelectAction>();

            validation = OperationResultGuards.RequireNotNull(
                battleSelectAction,
                "BattleSelectAction component"
            );
            if (!validation.Success)
            {
                return validation;
            }

            // TODO: Add more actions (Attack, Item, Trade, etc.)
            var populateResult = battleSelectAction.PopulateList(PopulateActionMenu());

            // Wire up button click handlers
            if (battleSelectAction.ListMenuContainer.TryGetComponent<MenuBase>(out var menuBase))
            {
                menuBase.OnItemSelected += (item) =>
                {
                    if (item is ListMenuItem listMenuItem)
                    {
                        HandleActionSelected(listMenuItem.ItemName);
                    }
                };

                // Disable menu input for one frame to prevent same-frame input processing
                StartCoroutine(EnableMenuInputNextFrame(menuBase));
            }

            return populateResult;
        }

        private System.Collections.IEnumerator EnableMenuInputNextFrame(MenuBase menu)
        {
            if (menu != null)
            {
                menu.enabled = false;

                // Wait until the confirm button is released
                while (_inputActions.Confirm?.IsPressed() == true)
                {
                    yield return null;
                }

                // Wait one additional frame after release
                yield return null;

                if (menu != null)
                {
                    menu.enabled = true;
                }
            }
        }

        private void CloseActionMenu()
        {
            if (_currentActionMenu != null)
            {
                Destroy(_currentActionMenu);
                _currentActionMenu = null;
            }
        }

        internal string[] PopulateActionMenu()
        {
            var actions = new List<string> { "Wait" };
            // Trade with adjacent allies
            if (BattleContext.Participants.AdjacentUnits.GetAdjacentAllyCount(BattleContext) > 0)
            {
                actions.Add("Trade");
                // TODO: Check talk/support
            }
            // mount/dismount
            if (BattleContext.Unit.UnitInstance.CurrentClass.ClassData.Identity.IsMountedClass())
            {
                if (BattleContext.Unit.UnitInstance.IsMounted)
                {
                    actions.Add("Dismount");
                }
                else
                {
                    actions.Add("Mount");
                }
            }
            // check if any enemies are in range (already updated after movement)
            if (BattleContext.Participants.TargetsInRange.Count > 0)
            {
                actions.Add("Attack");
            }
            return actions.ToArray();
        }

        public void HandleActionSelected(string actionName)
        {
            switch (actionName.ToLower())
            {
                case "wait":
                    HandleWaitAction();
                    break;
                default:
                    $"Unknown action: {actionName}".LogWarning();
                    break;
            }
        }

        private void HandleWaitAction()
        {
            CloseActionMenu();

            var validation = OperationResultGuards.RequireNotNull(SelectedUnit, "SelectedUnit");
            if (!validation.Success)
            {
                validation.ErrorMessage.LogWarning();
                return;
            }

            _playerTurnFlow.WaitAndEndTurn();
            // Note: Turn progression is handled by TurnRotisserie via PlayerTurnEnded event
            // DO NOT call Progress() here or turns will advance twice
            Brain.battleBrain.IsInputEnabled = true;
        }

        public void HandleActionMenuBack()
        {
            CloseActionMenu();
            RequestUndo();
        }

        public void OpenActionMenu() =>
            _playerTurnFlow.ActionChosen(PlayerTurnStates.ChoosingAction);

        private OperationResult ComputeValidTiles(CharacterInstance unit)
        {
            var validation = OperationResultGuards.RequireNotNull(unit, nameof(unit));
            if (!validation.Success)
            {
                validation.ErrorMessage.LogWarning();
                return validation;
            }

            var context = Brain.battleBrain.BattleObject.Context;
            if (!context.TryGetValidTilesForUnit(unit, out var moveTiles, out var attackTiles))
            {
                var templateName =
                    unit.CharacterTemplate != null
                        ? unit.CharacterTemplate.DisplayName
                        : $"<null template for unit id {unit.Id}>";
                return OperationResult.Failure(
                    $"Failed to get valid tiles for unit {templateName}"
                );
            }

            _validMoveTiles = moveTiles;
            _validAttackTiles = attackTiles;
            Brain.PublishValidTilesComputed(moveTiles, attackTiles);

            return OperationResult.Successful();
        }

        #endregion
    }
}
