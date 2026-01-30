using Turnroot.Utilities;

namespace Turnroot.Gameplay.Brain.Components.Battle
{
    public enum PlayerTurnStates
    {
        Inactive,
        NoUnitSelected,
        UnitSelected, // replaces NoActionChosen: unit selected and ready for input
        ChoosingDestination, // choosing where to move
        DestinationSelected, // destination confirmed, will execute move
        ExecutingMove, // move in progress; input locked until move/animation completes
        ChoosingAction, // choose an action after moving
        WaitActionChosen,
        AttackActionChosenChoosingTarget,
        AttackActionChosenTargetSelected,
        UseItemActionChosenChoosingItem,
        UseItemActionChosenItemSelected,
        HealActionChosenChoosingTarget,
        HealActionChosenTargetSelected,
        TalkActionChosenChoosingTarget,
        TalkActionChosenTargetSelected,
        TradeActionChosenChoosingTarget,
        TradeActionChosenTargetSelected,
        ChoosingTarget,
        TargetSelected,
        ConfirmAction,
        ExecutingAction, // Command/animation in progress; input locked
        TurnEnded,
    }

    public class PlayerTurnState
    {
        public PlayerTurnStates CurrentState { get; set; } = PlayerTurnStates.Inactive;

        public PlayerTurnStates PreviousState { get; private set; } = PlayerTurnStates.Inactive;

        public OperationResult TransitionToState(PlayerTurnStates newState)
        {
            // Valid transitions:
            // 1. Inactive -> NoUnitSelected. This occurs when the player's turn begins.
            // 2. NoUnitSelected -> UnitSelected. The player has chosen a unit but not an action.
            // 3. UnitSelected -> NoUnitSelected. The player has deselected the unit.
            // 4. UnitSelected -> DestinationSelected or ChoosingDestination: the player chose a tile directly or entered destination choosing mode.
            // 5. DestinationSelected -> ExecutingMove: move command issued; wait for move/animation.
            // 6. ExecutingMove -> ChoosingAction: when move has finished, player picks action from new location.
            // 7. ChoosingAction -> action-specific choosing states (Attack/Heal/UseItem/Wait/etc.).
            // 8. Any action-chosen-target-selected -> ConfirmAction. The player has confirmed their action.
            // 9. ConfirmAction -> ExecutingAction. The player's command executes and animations/effects play while input is locked.
            // 10. ExecutingAction -> TurnEnded or UnitSelected depending on follow-up rules (canto, extra action, etc.).
            // 11. Wait ActionChosen -> TurnEnded. The player has chosen to wait and end their turn.
            PreviousState = CurrentState;
            bool allowed = (CurrentState, newState) switch
            {
                (PlayerTurnStates.Inactive, PlayerTurnStates.NoUnitSelected) => true,

                (PlayerTurnStates.NoUnitSelected, PlayerTurnStates.UnitSelected) => true,

                (PlayerTurnStates.UnitSelected, PlayerTurnStates.NoUnitSelected) => true,

                (PlayerTurnStates.UnitSelected, PlayerTurnStates.ChoosingDestination) => true,
                // Allow entering action selection from the ChoosingAction state (post-move)
                (PlayerTurnStates.ChoosingAction, PlayerTurnStates.WaitActionChosen) => true,
                (
                    PlayerTurnStates.ChoosingAction,
                    PlayerTurnStates.AttackActionChosenChoosingTarget
                ) => true,
                (
                    PlayerTurnStates.ChoosingAction,
                    PlayerTurnStates.UseItemActionChosenChoosingItem
                ) => true,
                (
                    PlayerTurnStates.ChoosingAction,
                    PlayerTurnStates.HealActionChosenChoosingTarget
                ) => true,
                (
                    PlayerTurnStates.ChoosingAction,
                    PlayerTurnStates.TalkActionChosenChoosingTarget
                ) => true,
                (
                    PlayerTurnStates.ChoosingAction,
                    PlayerTurnStates.TradeActionChosenChoosingTarget
                ) => true,
                (PlayerTurnStates.UnitSelected, PlayerTurnStates.DestinationSelected) => true,
                (PlayerTurnStates.UnitSelected, PlayerTurnStates.WaitActionChosen) => true,
                (
                    PlayerTurnStates.UnitSelected,
                    PlayerTurnStates.AttackActionChosenChoosingTarget
                ) => true,
                (PlayerTurnStates.UnitSelected, PlayerTurnStates.UseItemActionChosenChoosingItem) =>
                    true,
                (PlayerTurnStates.UnitSelected, PlayerTurnStates.HealActionChosenChoosingTarget) =>
                    true,
                (PlayerTurnStates.UnitSelected, PlayerTurnStates.TalkActionChosenChoosingTarget) =>
                    true,
                (PlayerTurnStates.UnitSelected, PlayerTurnStates.TradeActionChosenChoosingTarget) =>
                    true,

                (PlayerTurnStates.ChoosingDestination, PlayerTurnStates.UnitSelected) => true,
                (PlayerTurnStates.DestinationSelected, PlayerTurnStates.ExecutingMove) => true,
                (PlayerTurnStates.DestinationSelected, PlayerTurnStates.ChoosingAction) => true,
                (PlayerTurnStates.WaitActionChosen, PlayerTurnStates.UnitSelected) => true,
                (
                    PlayerTurnStates.AttackActionChosenChoosingTarget,
                    PlayerTurnStates.UnitSelected
                ) => true,
                (PlayerTurnStates.UseItemActionChosenChoosingItem, PlayerTurnStates.UnitSelected) =>
                    true,
                (PlayerTurnStates.HealActionChosenChoosingTarget, PlayerTurnStates.UnitSelected) =>
                    true,
                (PlayerTurnStates.TalkActionChosenChoosingTarget, PlayerTurnStates.UnitSelected) =>
                    true,
                (PlayerTurnStates.TradeActionChosenChoosingTarget, PlayerTurnStates.UnitSelected) =>
                    true,

                (PlayerTurnStates.ChoosingDestination, PlayerTurnStates.DestinationSelected) =>
                    true,
                (
                    PlayerTurnStates.AttackActionChosenChoosingTarget,
                    PlayerTurnStates.AttackActionChosenTargetSelected
                ) => true,
                (
                    PlayerTurnStates.UseItemActionChosenChoosingItem,
                    PlayerTurnStates.UseItemActionChosenItemSelected
                ) => true,
                (
                    PlayerTurnStates.HealActionChosenChoosingTarget,
                    PlayerTurnStates.HealActionChosenTargetSelected
                ) => true,
                (
                    PlayerTurnStates.TalkActionChosenChoosingTarget,
                    PlayerTurnStates.TalkActionChosenTargetSelected
                ) => true,
                (
                    PlayerTurnStates.TradeActionChosenChoosingTarget,
                    PlayerTurnStates.TradeActionChosenTargetSelected
                ) => true,

                (PlayerTurnStates.DestinationSelected, PlayerTurnStates.ConfirmAction) => true,
                (
                    PlayerTurnStates.AttackActionChosenTargetSelected,
                    PlayerTurnStates.ConfirmAction
                ) => true,
                (
                    PlayerTurnStates.UseItemActionChosenItemSelected,
                    PlayerTurnStates.ConfirmAction
                ) => true,
                (PlayerTurnStates.HealActionChosenTargetSelected, PlayerTurnStates.ConfirmAction) =>
                    true,
                (PlayerTurnStates.TalkActionChosenTargetSelected, PlayerTurnStates.ConfirmAction) =>
                    true,
                (
                    PlayerTurnStates.TradeActionChosenTargetSelected,
                    PlayerTurnStates.ConfirmAction
                ) => true,

                (PlayerTurnStates.DestinationSelected, PlayerTurnStates.ChoosingDestination) =>
                    true,
                (
                    PlayerTurnStates.AttackActionChosenTargetSelected,
                    PlayerTurnStates.AttackActionChosenChoosingTarget
                ) => true,
                (
                    PlayerTurnStates.UseItemActionChosenItemSelected,
                    PlayerTurnStates.UseItemActionChosenChoosingItem
                ) => true,
                (
                    PlayerTurnStates.HealActionChosenTargetSelected,
                    PlayerTurnStates.HealActionChosenChoosingTarget
                ) => true,
                (
                    PlayerTurnStates.TalkActionChosenTargetSelected,
                    PlayerTurnStates.TalkActionChosenChoosingTarget
                ) => true,
                (
                    PlayerTurnStates.TradeActionChosenTargetSelected,
                    PlayerTurnStates.TradeActionChosenChoosingTarget
                ) => true,

                (PlayerTurnStates.ConfirmAction, PlayerTurnStates.ExecutingAction) => true,
                (PlayerTurnStates.ExecutingAction, PlayerTurnStates.TurnEnded) => true,
                (PlayerTurnStates.ExecutingAction, PlayerTurnStates.UnitSelected) => true,
                (PlayerTurnStates.ConfirmAction, PlayerTurnStates.UnitSelected) => true,
                (PlayerTurnStates.ExecutingMove, PlayerTurnStates.ChoosingAction) => true,

                (PlayerTurnStates.WaitActionChosen, PlayerTurnStates.TurnEnded) => true,

                _ => false,
            };

            if (allowed)
            {
                CurrentState = newState;
                return OperationResult.Successful();
            }

            return OperationResult.Failure(
                $"Invalid state transition from {CurrentState} to {newState}"
            );
        }
    }
}
