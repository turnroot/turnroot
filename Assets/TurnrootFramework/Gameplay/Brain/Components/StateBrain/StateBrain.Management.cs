using System;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.Brain
{
    public partial class StateBrain : BrainComponent
    {
        #region State Management

        private BrainState FindHighLevelState(string name)
        {
            return string.IsNullOrEmpty(name) || _highLevelStates == null
                ? null
                : Array.Find(_highLevelStates, s => s.Name == name);
        }

        private void SetCurrentState(BrainState newState)
        {
            if (newState == null)
            {
                return;
            }

            // If the requested state is already active (or logically the same), ignore
            if (CurrentState != null)
            {
                var currentPath = CurrentState.GetFullPath();
                var newPath = newState.GetFullPath();
                if (string.Equals(currentPath, newPath, StringComparison.Ordinal))
                {
                    return;
                }
            }

            if (CurrentState != null)
            {
                CurrentState.IsActive = false;
            }

            _currentState = newState;
            CurrentState.IsActive = true;

            $"StateBrain: SetCurrentState -> {CurrentState.Name}".LogInfo();
            Brain.PublishStateChanged(CurrentState);
        }

        public BrainState ActivateHighLevelState(string stateName)
        {
            var newState = FindHighLevelState(stateName);
            if (newState == null)
            {
                _ = OperationResult.Failure(
                    $"StateBrain: High-level state '{stateName}' not found."
                );
                return null;
            }

            SetCurrentState(newState);
            return CurrentState;
        }

        public void ActivateChildState(string childStateName)
        {
            if (CurrentState == null)
            {
                _ = OperationResult.Failure("No active state.");
                return;
            }

            // Determine the parent state to search in
            BrainState parentState =
                CurrentState.Children != null && CurrentState.Children.Length > 0
                    ? CurrentState // Current state is a parent
                    : CurrentState.Parent; // Current state is a child, use its parent

            if (parentState?.Children == null || parentState.Children.Length == 0)
            {
                _ = OperationResult.Failure(
                    $"StateBrain: Cannot find child state '{childStateName}' from '{CurrentState.Name}'."
                );
                return;
            }

            var childState = Array.Find(parentState.Children, s => s.Name == childStateName);
            if (childState != null)
            {
                SetCurrentState(childState);
            }
            else
            {
                $"StateBrain: Child state '{childStateName}' not found in '{parentState.Name}'.".LogError();
            }
        }

        public OperationResult ActivateChildStateByFullPath(
            string parentStateName,
            string childStateName
        )
        {
            // Find the parent state among high-level states
            var parentState = FindHighLevelState(parentStateName);
            if (parentState == null)
            {
                return OperationResult.Failure(
                    $"StateBrain: Parent state '{parentStateName}' not found."
                );
            }

            // Validate that the parent state has children
            if (parentState.Children == null || parentState.Children.Length == 0)
            {
                return OperationResult.Failure(
                    $"StateBrain: Parent state '{parentStateName}' has no child states."
                );
            }

            // Find the child state within the parent
            var childState = Array.Find(parentState.Children, s => s.Name == childStateName);
            if (childState != null)
            {
                // Directly set the child state, which will automatically handle the parent relationship
                SetCurrentState(childState);
            }
            else
            {
                return OperationResult.Failure(
                    $"StateBrain: Child state '{childStateName}' not found in parent state '{parentStateName}'."
                );
            }
            return OperationResult.Successful();
        }

        public bool GetChildStates()
        {
            return CurrentState == null
                ? OperationResult.Failure("No active state.").Success
                : CurrentState.Children != null && CurrentState.Children.Length != 0;
        }

        #endregion
    }
}
