using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Characters.Components;
using Turnroot.Gameplay.Brain.Components.Battle;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Skills.Nodes;
using Turnroot.UI.Components.ListMenu;
using Turnroot.UI.Components.Menu;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.Brain
{
    public partial class BattleInputControllerBrain : BrainComponent
    {
        #region Action Validations
        public bool CanTrade() =>
            BattleContext.Participants.AdjacentUnits.GetAdjacentAllyCount(BattleContext) > 0;

        public bool CanAccessStorehouse() =>
            BattleContext.Unit.UnitInstance.CharacterTemplate.Which == CharacterWhich.AVATAR
            || BattleContext.AvatarIsOneUnitAway();

        public bool CanMountOrDismount() =>
            BattleContext.Unit.UnitInstance.CurrentClass.ClassData.Identity.IsMountedClass();

        public bool CanSteal()
        {
            if (BattleContext.Unit.UnitCanSteal(BattleContext.Skill))
            {
                if (
                    BattleContext
                        .GetPotentialTargets(
                            BattleTargetIntentMap.GetIntent(BattleTargetAction.Steal),
                            IsStealTargetInRange
                        )
                        .Count > 0
                )
                {
                    return true;
                }
            }
            return false;
        }

        public bool CanAttack() =>
            BattleContext
                .GetPotentialTargets(
                    BattleTargetIntentMap.GetIntent(BattleTargetAction.Attack),
                    IsAttackTargetInRange
                )
                .Count > 0;

        #endregion


        #region Action Handlers
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
                // TODO: Implement other actions (Attack, Trade, Storehouse, Mount/Dismount, etc.)
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

            _playerTurnFlow.WaitAndEndUnitTurn();
            // The unit-turn advance is handled by TurnRotisserie, do not advance the rotisserie here or the unit turn will advance twice
            Brain.battleBrain.IsInputEnabled = true;
        }

        public void HandleActionMenuBack()
        {
            CloseActionMenu();
            RequestUndo();
        }
        #endregion

        internal string[] PopulateActionMenu()
        {
            // TODO: finish this: Heal, Support, Talk, Steal missing

            var actions = new List<string> { "Wait" };

            // Trade
            if (CanTrade())
            {
                actions.Add("Trade");
            }

            // storehouse
            if (CanAccessStorehouse())
            {
                // storehouse access is only available to the avatar or units adjacent to the avatar
                actions.Add("Storehouse");
            }

            // mount/dismount
            if (CanMountOrDismount())
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

            // steal
            // before rolling GetPotentialTargets check if this unit can even steal
            // This is skill-dependent- the active unit must have a skill that allows stealing
            // There is one node for this, the StealNode
            if (CanSteal())
            {
                actions.Add("Steal");
            }

            // attack
            if (CanAttack())
            {
                actions.Add("Attack");
            }

            return actions.ToArray();
        }

        #region Menu Life
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
