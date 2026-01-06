using Turnroot.Utilities;

public enum PlayerTurnStates
{
    Inactive,
    NoUnitSelected,
    NoActionChosen,
    MoveActionChosenChoosingDestination,
    MoveActionChosenDestinationSelected,
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
        // 2. NoUnitSelected -> NoActionChosen. The player has chosen a unit but not an action.
        // 3. NoActionChosen -> NoUnitSelected. The player has deselected the unit.
        // 4. NoActionChosen -> any one of MoveActionChosenChoosingDestination, WaitActionChosen,
        //      AttackActionChosenChoosingTarget, UseItemActionChosenChoosingItem, HealActionChosenChoosingTarget,
        //      TalkActionChosenChoosingTarget, TradeActionChosenChoosingTarget. The player has chosen an action.
        // 5. Any action-chosen -> NoActionChosen. The player has undone their action choice.
        // 6. Any action-chosen -> corresponding action-chosen-target-selected state. The player has selected a target/destination/item.
        // 7. Any action-chosen-target-selected -> ConfirmAction. The player has confirmed their action.
        // 8. Any action-chosen-target-selected -> corresponding action-chosen-choosing-target state. The player has undone their target/destination/item choice.
        // 9. ConfirmAction -> ExecutingAction. The player's command executes and animations/effects play while input is locked.
        // 10. ExecutingAction -> TurnEnded. Action complete; the player's turn ends.
        // 11. ExecutingAction -> NoActionChosen. Action complete but player can act again (e.g., canto/follow-up).
        // 12. ConfirmAction -> NoActionChosen. This allows for skills that grant moving after attacking, or attacking a second enemy (shortcut flows).
        // 13. Wait ActionChosen -> TurnEnded. The player has chosen to wait and end their turn.
        PreviousState = CurrentState;
        bool allowed = (CurrentState, newState) switch
        {
            (PlayerTurnStates.Inactive, PlayerTurnStates.NoUnitSelected) => true,

            (PlayerTurnStates.NoUnitSelected, PlayerTurnStates.NoActionChosen) => true,

            (PlayerTurnStates.NoActionChosen, PlayerTurnStates.NoUnitSelected) => true,

            (
                PlayerTurnStates.NoActionChosen,
                PlayerTurnStates.MoveActionChosenChoosingDestination
            ) => true,
            (PlayerTurnStates.NoActionChosen, PlayerTurnStates.WaitActionChosen) => true,
            (PlayerTurnStates.NoActionChosen, PlayerTurnStates.AttackActionChosenChoosingTarget) =>
                true,
            (PlayerTurnStates.NoActionChosen, PlayerTurnStates.UseItemActionChosenChoosingItem) =>
                true,
            (PlayerTurnStates.NoActionChosen, PlayerTurnStates.HealActionChosenChoosingTarget) =>
                true,
            (PlayerTurnStates.NoActionChosen, PlayerTurnStates.TalkActionChosenChoosingTarget) =>
                true,
            (PlayerTurnStates.NoActionChosen, PlayerTurnStates.TradeActionChosenChoosingTarget) =>
                true,

            (
                PlayerTurnStates.MoveActionChosenChoosingDestination,
                PlayerTurnStates.NoActionChosen
            ) => true,
            (PlayerTurnStates.WaitActionChosen, PlayerTurnStates.NoActionChosen) => true,
            (PlayerTurnStates.AttackActionChosenChoosingTarget, PlayerTurnStates.NoActionChosen) =>
                true,
            (PlayerTurnStates.UseItemActionChosenChoosingItem, PlayerTurnStates.NoActionChosen) =>
                true,
            (PlayerTurnStates.HealActionChosenChoosingTarget, PlayerTurnStates.NoActionChosen) =>
                true,
            (PlayerTurnStates.TalkActionChosenChoosingTarget, PlayerTurnStates.NoActionChosen) =>
                true,
            (PlayerTurnStates.TradeActionChosenChoosingTarget, PlayerTurnStates.NoActionChosen) =>
                true,

            (
                PlayerTurnStates.MoveActionChosenChoosingDestination,
                PlayerTurnStates.MoveActionChosenDestinationSelected
            ) => true,
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

            (
                PlayerTurnStates.MoveActionChosenDestinationSelected,
                PlayerTurnStates.ConfirmAction
            ) => true,
            (PlayerTurnStates.AttackActionChosenTargetSelected, PlayerTurnStates.ConfirmAction) =>
                true,
            (PlayerTurnStates.UseItemActionChosenItemSelected, PlayerTurnStates.ConfirmAction) =>
                true,
            (PlayerTurnStates.HealActionChosenTargetSelected, PlayerTurnStates.ConfirmAction) =>
                true,
            (PlayerTurnStates.TalkActionChosenTargetSelected, PlayerTurnStates.ConfirmAction) =>
                true,
            (PlayerTurnStates.TradeActionChosenTargetSelected, PlayerTurnStates.ConfirmAction) =>
                true,

            (
                PlayerTurnStates.MoveActionChosenDestinationSelected,
                PlayerTurnStates.MoveActionChosenChoosingDestination
            ) => true,
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
            (PlayerTurnStates.ExecutingAction, PlayerTurnStates.NoActionChosen) => true,
            (PlayerTurnStates.ConfirmAction, PlayerTurnStates.NoActionChosen) => true,

            (PlayerTurnStates.WaitActionChosen, PlayerTurnStates.TurnEnded) => true,

            _ => false,
        };

        if (allowed)
        {
            CurrentState = newState;
            return OperationResult.SuccessResult();
        }

        return OperationResult.Failure(
            $"Invalid state transition from {CurrentState} to {newState}"
        );
    }
}
