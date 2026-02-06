using Turnroot.Utilities;

namespace Turnroot.Gameplay.Brain.Components.Battle
{
    /// <summary>
    /// Defines the possible states during a player-controlled unit's turn in battle.
    /// </summary>
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

    /// <summary>
    /// Manages state transitions for player turn flow with validation and tracking of previous states.
    /// </summary>
    public class PlayerTurnState
    {
        public PlayerTurnStates CurrentState { get; set; } = PlayerTurnStates.Inactive;
        public PlayerTurnStates PreviousState { get; private set; } = PlayerTurnStates.Inactive;

        // Define valid transitions as a static table for clarity and maintainability
        private static readonly System.Collections.Generic.Dictionary<
            PlayerTurnStates,
            System.Collections.Generic.HashSet<PlayerTurnStates>
        > ValidTransitions = new()
        {
            [PlayerTurnStates.Inactive] = new() { PlayerTurnStates.NoUnitSelected },
            [PlayerTurnStates.NoUnitSelected] = new() { PlayerTurnStates.UnitSelected },
            [PlayerTurnStates.UnitSelected] = new()
            {
                PlayerTurnStates.NoUnitSelected,
                PlayerTurnStates.ChoosingDestination,
                PlayerTurnStates.DestinationSelected,
                PlayerTurnStates.WaitActionChosen,
                PlayerTurnStates.AttackActionChosenChoosingTarget,
                PlayerTurnStates.UseItemActionChosenChoosingItem,
                PlayerTurnStates.HealActionChosenChoosingTarget,
                PlayerTurnStates.TalkActionChosenChoosingTarget,
                PlayerTurnStates.TradeActionChosenChoosingTarget,
            },
            [PlayerTurnStates.ChoosingDestination] = new()
            {
                PlayerTurnStates.UnitSelected,
                PlayerTurnStates.DestinationSelected,
            },
            [PlayerTurnStates.DestinationSelected] = new()
            {
                PlayerTurnStates.ChoosingDestination,
                PlayerTurnStates.ExecutingMove,
                PlayerTurnStates.ChoosingAction,
                PlayerTurnStates.ConfirmAction,
            },
            [PlayerTurnStates.ExecutingMove] = new() { PlayerTurnStates.ChoosingAction },
            [PlayerTurnStates.ChoosingAction] = new()
            {
                PlayerTurnStates.WaitActionChosen,
                PlayerTurnStates.AttackActionChosenChoosingTarget,
                PlayerTurnStates.UseItemActionChosenChoosingItem,
                PlayerTurnStates.HealActionChosenChoosingTarget,
                PlayerTurnStates.TalkActionChosenChoosingTarget,
                PlayerTurnStates.TradeActionChosenChoosingTarget,
            },
            [PlayerTurnStates.WaitActionChosen] = new()
            {
                PlayerTurnStates.UnitSelected,
                PlayerTurnStates.TurnEnded,
            },
            [PlayerTurnStates.AttackActionChosenChoosingTarget] = new()
            {
                PlayerTurnStates.UnitSelected,
                PlayerTurnStates.AttackActionChosenTargetSelected,
            },
            [PlayerTurnStates.AttackActionChosenTargetSelected] = new()
            {
                PlayerTurnStates.AttackActionChosenChoosingTarget,
                PlayerTurnStates.ConfirmAction,
            },
            [PlayerTurnStates.UseItemActionChosenChoosingItem] = new()
            {
                PlayerTurnStates.UnitSelected,
                PlayerTurnStates.UseItemActionChosenItemSelected,
            },
            [PlayerTurnStates.UseItemActionChosenItemSelected] = new()
            {
                PlayerTurnStates.UseItemActionChosenChoosingItem,
                PlayerTurnStates.ConfirmAction,
            },
            [PlayerTurnStates.HealActionChosenChoosingTarget] = new()
            {
                PlayerTurnStates.UnitSelected,
                PlayerTurnStates.HealActionChosenTargetSelected,
            },
            [PlayerTurnStates.HealActionChosenTargetSelected] = new()
            {
                PlayerTurnStates.HealActionChosenChoosingTarget,
                PlayerTurnStates.ConfirmAction,
            },
            [PlayerTurnStates.TalkActionChosenChoosingTarget] = new()
            {
                PlayerTurnStates.UnitSelected,
                PlayerTurnStates.TalkActionChosenTargetSelected,
            },
            [PlayerTurnStates.TalkActionChosenTargetSelected] = new()
            {
                PlayerTurnStates.TalkActionChosenChoosingTarget,
                PlayerTurnStates.ConfirmAction,
            },
            [PlayerTurnStates.TradeActionChosenChoosingTarget] = new()
            {
                PlayerTurnStates.UnitSelected,
                PlayerTurnStates.TradeActionChosenTargetSelected,
            },
            [PlayerTurnStates.TradeActionChosenTargetSelected] = new()
            {
                PlayerTurnStates.TradeActionChosenChoosingTarget,
                PlayerTurnStates.ConfirmAction,
            },
            [PlayerTurnStates.ConfirmAction] = new()
            {
                PlayerTurnStates.UnitSelected,
                PlayerTurnStates.ExecutingAction,
            },
            [PlayerTurnStates.ExecutingAction] = new()
            {
                PlayerTurnStates.UnitSelected,
                PlayerTurnStates.TurnEnded,
            },
        };

        public OperationResult TransitionToState(PlayerTurnStates newState)
        {
            // Check if transition is valid using the table
            if (
                !ValidTransitions.TryGetValue(CurrentState, out var allowedStates)
                || !allowedStates.Contains(newState)
            )
            {
                return OperationResult.Failure(
                    $"Invalid state transition from {CurrentState} to {newState}"
                );
            }

            PreviousState = CurrentState;
            CurrentState = newState;
            return OperationResult.Successful();
        }
    }
}
