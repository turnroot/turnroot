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
        // cache these starting on UnitSelected and ending on NoUnitSelected
        private bool _canTradeCached;
        private bool _canSupportCached;
        private bool _canTalkCached;
        private bool _canHealCached;
        private bool _canAccessStorehouseCached;
        private bool _canMountOrDismountCached;
        private bool _canStealCached;
        private bool _canAttackCached;

        private bool _hasCached;

        public void CacheUnitActions()
        {
            if (_playerTurnFlow.GetCurrentState() == PlayerTurnStates.UnitSelected || !_hasCached)
            {
                _hasCached = true;
            }
            {
                _canTradeCached = CanTrade();
                _canSupportCached = CanSupport();
                _canTalkCached = CanTalk();
                _canHealCached = CanHeal();
                _canAccessStorehouseCached = CanAccessStorehouse();
                _canMountOrDismountCached = CanMountOrDismount();
                _canStealCached = CanSteal();
                _canAttackCached = CanAttack();
            }

            if (_playerTurnFlow.GetCurrentState() == PlayerTurnStates.NoUnitSelected)
            {
                _hasCached = false;
            }
        }

        public bool CanTrade() =>
            BattleContext.Participants.AdjacentUnits.GetAdjacentAllyCount(BattleContext) > 0;

        public bool CanSupport()
        {
            var targets = BattleContext.GetPotentialTargets(
                BattleTargetIntentMap.GetIntent(BattleTargetAction.Support),
                IsSupportTargetInRange,
                BattleTargetAction.Support
            );

            var any = false;
            foreach (var target in targets)
            {
                if (
                    BattleContext.Unit.UnitInstance.HasSupportConversationAvailable(
                        target.CharacterTemplate
                    )
                )
                {
                    any = true;
                    break;
                }
            }
            return any;
        }

        public bool CanTalk() => false; // TODO: implement

        public bool CanHeal()
        {
            // have to be targets and the class has to allow healing
            var targets =
                BattleContext
                    .GetPotentialTargets(
                        BattleTargetIntentMap.GetIntent(BattleTargetAction.Heal),
                        IsHealTargetInRange,
                        BattleTargetAction.Heal
                    )
                    .Count > 0;
            var canHeal = BattleContext.Unit.UnitInstance.CurrentClass.ClassData.Identity.CanHeal;
            return canHeal && targets;
        }

        public bool CanAccessStorehouse() =>
            BattleContext.Unit.UnitInstance.CharacterTemplate.Which == CharacterWhich.AVATAR
            || BattleContext.AvatarIsOneUnitAway();

        public bool CanMountOrDismount() =>
            BattleContext.Unit.UnitInstance.CurrentClass.ClassData.Identity.IsMountedClass();

        public bool CanSteal()
        {
            var activeUnit = BattleContext?.Unit?.UnitInstance;
            return (activeUnit?.InventoryInstance) != null
                && !activeUnit.InventoryInstance.IsFull
                && BattleContext.Unit.UnitCanSteal()
                && BattleContext
                    .GetPotentialTargets(
                        BattleTargetIntentMap.GetIntent(BattleTargetAction.Steal),
                        IsStealTargetInRange,
                        BattleTargetAction.Steal
                    )
                    .Count > 0;
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
                case "steal":
                    HandleStealAction();
                    break;
                case "attack":
                    HandleAttackAction();
                    break;
                case "trade":
                    HandleTradeAction();
                    break;
                case "storehouse":
                    HandleStorehouseAction();
                    break;
                case "mount":
                    HandleMountAction();
                    break;
                case "dismount":
                    HandleDismountAction();
                    break;
                case "heal":
                    HandleHealAction();
                    break;
                case "support":
                    HandleSupportAction();
                    break;
                case "talk":
                    HandleTalkAction();
                    break;
                default:
                    $"Unknown action: {actionName}".LogWarning();
                    break;
            }
        }

        private void HandleAttackAction()
        {
            // TODO: Implement attack action
            CloseActionMenu();
            _playerTurnFlow.ActionChosen(PlayerTurnStates.AttackActionChosenChoosingTarget);
        }

        private void HandleTradeAction()
        {
            // TODO: Implement trade action
            CloseActionMenu();
            _playerTurnFlow.ActionChosen(PlayerTurnStates.TradeActionChosenChoosingTarget);
        }

        private void HandleStorehouseAction()
        {
            // TODO: Implement storehouse action
            CloseActionMenu();
            _playerTurnFlow.ActionChosen(PlayerTurnStates.StorehouseActionChosen);
        }

        private void HandleMountAction()
        {
            // mount character, refresh their movement and pathfinding
            // don't do either directly here- just
            // publish the brain event for mounting so graphics etc will update
            // then battlebrain should listen to that event
            Brain.PublishBattleUnitMounts(BattleContext.Unit.UnitInstance);
            CloseActionMenu();
            _playerTurnFlow.ActionChosen(PlayerTurnStates.MountActionChosen);
        }

        private void HandleDismountAction()
        {
            Brain.PublishBattleUnitDismounts(BattleContext.Unit.UnitInstance);
            CloseActionMenu();
            _playerTurnFlow.ActionChosen(PlayerTurnStates.DismountActionChosen);
        }

        private void HandleHealAction()
        {
            // TODO: Implement heal action
            CloseActionMenu();
            _playerTurnFlow.ActionChosen(PlayerTurnStates.HealActionChosenChoosingTarget);
        }

        private void HandleSupportAction()
        {
            // TODO: Implement support action
            CloseActionMenu();
            _playerTurnFlow.ActionChosen(PlayerTurnStates.SupportActionChosenChoosingTarget);
        }

        private void HandleTalkAction()
        {
            // TODO: Implement talk action
            CloseActionMenu();
            _playerTurnFlow.ActionChosen(PlayerTurnStates.TalkActionChosenChoosingTarget);
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

        private void HandleStealAction()
        {
            // TODO: Implement logic for steal action
            CloseActionMenu();
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
            var actions = new List<string> { "Wait" };

            // Trade
            if (_canTradeCached)
            {
                actions.Add("Trade");
            }

            // Support
            if (_canSupportCached)
            {
                actions.Add("Support");
            }

            // Talk
            if (_canTalkCached)
            {
                actions.Add("Talk");
            }

            // storehouse
            if (_canAccessStorehouseCached)
            {
                // storehouse access is only available to the avatar or units adjacent to the avatar
                actions.Add("Storehouse");
            }

            // mount/dismount
            if (_canMountOrDismountCached)
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
            if (_canStealCached)
            {
                actions.Add("Steal");
            }

            // attack
            if (_canAttackCached)
            {
                actions.Add("Attack");
            }

            // heal
            if (_canHealCached)
            {
                actions.Add("Heal");
            }

            return actions.ToArray();
        }

        #region Menu Life
        private OperationResult ShowActionMenu()
        {
            CacheUnitActions();
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
            _playerTurnFlow.ActionChosen(PlayerTurnStates.ChoosingActionAfterMove);

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
