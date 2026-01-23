using System;
using System.Linq;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

public class BrainState
{
    public string Name { get; private set; }
    public BrainState Parent { get; set; }
    public BrainState[] Children { get; set; }
    public bool IsActive { get; set; }

    public BrainState(string name, BrainState parent = null)
    {
        Name = name;
        Parent = parent;
        Children = null;
        IsActive = false;
    }

    public string GetFullPath() => Parent != null ? $"{Parent.Name}.{Name}" : Name;
}

/// <summary>
/// Constant state names for type safety and refactoring ease.
/// </summary>
public static class BrainStateNames
{
    // High-level states (these are parent states)
    public const string Combat = "Combat";
    public const string Paused = "Paused";
    public const string Cutscene = "Cutscene";
    public const string WorldMap = "WorldMap";
    public const string MainMenu = "MainMenu";
    public const string GameOver = "GameOver";
    public const string Credits = "Credits";
    public const string NonCombatGameplay = "NonCombatGameplay";
    public const string Hub = "Hub";

    // Battle child states (full paths)
    public const string PreBattle = "PreBattle";

    public const string PreBattleTransitionToBattle = "PreBattleTransitionToBattle";
    public const string Battle = "Battle";
    public const string PostBattle = "PostBattle";

    /// <summary>
    /// Returns all valid state IDs as full paths. This is the single source of truth for all states.
    /// High-level states without children are listed as-is (e.g., "Paused").
    /// States with children are listed with their hierarchy (e.g., "Combat.PreBattle").
    /// Used by UI and flow systems for validation and dropdown menus.
    /// </summary>
    public static string[] GetAllStateIds()
    {
        return new[]
        {
            // High-level states without children
            Paused,
            Cutscene,
            WorldMap,
            MainMenu,
            GameOver,
            Credits,
            NonCombatGameplay,
            Hub,
            // Combat with child states
            $"{Combat}.{PreBattle}",
            $"{Combat}.{PreBattleTransitionToBattle}",
            $"{Combat}.{Battle}",
            $"{Combat}.{PostBattle}",
        };
    }
}

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Manages high-level game states and transitions within the brain system.
    /// </summary>
    public class StateBrain : BrainComponent
    {
        [SerializeField]
        private BrainState _currentState;
        public BrainState CurrentState => _currentState;

        private BrainState[] _highLevelStates;
        private BrainState _savedStateBeforePause;

        // States that require back button and menu UI
        public static readonly string[] StatesThatNeedMenus = new string[]
        {
            BrainStateNames.Paused,
            BrainStateNames.MainMenu,
            BrainStateNames.PreBattle,
        };

        protected override EventPriority GetSubscriptionPriority() => EventPriority.Highest;

        protected override void Awake()
        {
            base.Awake();
            InitializeHighLevelStates();
            InitializeBattleChildStates();
        }

        protected override void SubscribeToBrainEvents() =>
            // Listen for pre-battle completion and transition to Battle state
            _brain.OnPreBattleCompleted += HandlePreBattleCompleted;

        public void HandlePreBattleTransitionToBattleCompleted() =>
            ActivateChildState(BrainStateNames.Battle);

        private void HandlePreBattleCompleted()
        {
            // PreBattleTransitionToBattle is a sibling of PreBattle under Combat
            if (_currentState?.Parent != null)
            {
                // Find the PreBattleTransitionToBattle child state and set it directly
                var newState = _currentState.Parent.Children.FirstOrDefault(child =>
                    child.Name == BrainStateNames.PreBattleTransitionToBattle
                );
                if (newState != null)
                {
                    SetCurrentState(newState);
                }
                else
                {
                    // Fallback: activate child state
                    ActivateChildState(BrainStateNames.PreBattleTransitionToBattle);
                }
            }
            else
            {
                // Fallback: try to activate PreBattleTransitionToBattle directly
                ActivateChildState(BrainStateNames.PreBattleTransitionToBattle);
            }
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            if (_brain != null)
            {
                _brain.OnPreBattleCompleted -= HandlePreBattleCompleted;
            }
        }

        #region Initialization

        public void InitializeHighLevelStates()
        {
            if (_highLevelStates != null && _highLevelStates.Length > 0)
            {
                return;
            }

            SetHighLevelStates();
        }

        public void InitializeBattleChildStates()
        {
            var combatState = FindHighLevelState(BrainStateNames.Combat);
            if (combatState.Children != null && combatState.Children.Length > 0)
            {
                return; // Already initialized
            }

            SetBattleChildStates();
        }

        public OperationResult SetBattleChildStates()
        {
            var combatState = FindHighLevelState(BrainStateNames.Combat);
            if (combatState == null)
            {
                return OperationResult.Failure(
                    "StateBrain: Combat state not found during child state initialization."
                );
            }

            var childStates = new BrainState[]
            {
                new(BrainStateNames.PreBattle, combatState),
                new(BrainStateNames.PreBattleTransitionToBattle, combatState),
                new(BrainStateNames.Battle, combatState),
                new(BrainStateNames.PostBattle, combatState),
            };

            combatState.Children = childStates;
            return OperationResult.Successful();
        }

        public void SetHighLevelStates()
        {
            var states = new System.Collections.Generic.List<BrainState>
            {
                new(BrainStateNames.Cutscene),
                new(BrainStateNames.Paused),
                new(BrainStateNames.Combat),
                new(BrainStateNames.WorldMap),
            };

#if TURNROOT_CAMP_MODULE
            states.Add(new BrainState(BrainStateNames.Hub));
#endif

            states.AddRange(
                new[]
                {
                    new BrainState(BrainStateNames.MainMenu),
                    new BrainState(BrainStateNames.GameOver),
                    new BrainState(BrainStateNames.Credits),
                    new BrainState(BrainStateNames.NonCombatGameplay),
                }
            );

            _highLevelStates = states.ToArray();
            _brain?.PublishHighLevelStatesInitialized();
        }

        #endregion

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

            if (_currentState != null)
            {
                _currentState.IsActive = false;
            }

            _currentState = newState;
            _currentState.IsActive = true;

            TurnrootLogger.Log($"StateBrain: SetCurrentState -> {_currentState.Name}");
            _brain?.PublishStateChanged(_currentState);
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
            return _currentState;
        }

        public void ActivateChildState(string childStateName)
        {
            if (_currentState == null)
            {
                _ = OperationResult.Failure("No active state.");
                return;
            }

            // Determine the parent state to search in
            BrainState parentState =
                _currentState.Children != null && _currentState.Children.Length > 0
                    ? _currentState // Current state is a parent
                    : _currentState.Parent; // Current state is a child, use its parent

            if (parentState?.Children == null || parentState.Children.Length == 0)
            {
                _ = OperationResult.Failure(
                    $"StateBrain: Cannot find child state '{childStateName}' from '{_currentState.Name}'."
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
                TurnrootLogger.Log(
                    $"StateBrain: Child state '{childStateName}' not found in '{parentState.Name}'.",
                    TurnrootLogger.LogLevel.Error
                );
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
            return _currentState == null
                ? OperationResult.Failure("No active state.").Success
                : _currentState.Children != null && _currentState.Children.Length != 0;
        }

        #endregion

        #region Pause/Resume

        public void Pause() => SetPausedState(true);

        public void Resume() => SetPausedState(false);

        private bool SetPausedState(bool isPaused)
        {
            var pausedState = FindHighLevelState(BrainStateNames.Paused);
            if (pausedState == null)
            {
                return OperationResult.Failure("Paused state not found.").Success;
            }

            if (isPaused)
            {
                _savedStateBeforePause = _currentState;
                SetCurrentState(pausedState);
                TimeManager.PauseGame();
                _brain?.PublishPaused(_savedStateBeforePause);
            }
            else
            {
                if (_savedStateBeforePause != null)
                {
                    SetCurrentState(_savedStateBeforePause);
                    TimeManager.ResumeGame();
                    _brain?.PublishResumed(_savedStateBeforePause);
                    _savedStateBeforePause = null;
                }
                else if (_currentState != null)
                {
                    _currentState.IsActive = false;
                }
            }

            return true;
        }

        #endregion
    }
}
