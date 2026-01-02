using System;
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

    public string GetFullPath()
    {
        return Parent != null ? $"{Parent.Name}.{Name}" : Name;
    }
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

        protected override void Awake()
        {
            base.Awake(); // Calls parent Awake
#if UNITY_EDITOR
            Debug.Log("StateBrain Awake called.");
#endif
            InitializeHighLevelStates();
            InitializeBattleChildStates();
        }

        protected override void SubscribeToBrainEvents()
        {
            // Listen for pre-battle completion and transition to Battle state
            _brain.OnPreBattleCompleted += HandlePreBattleCompleted;
        }

        private void HandlePreBattleCompleted()
        {
            // Battle is a sibling of PreBattle under Combat
            if (_currentState?.Parent != null)
            {
                // Go to parent (Combat) then activate Battle as its child
                SetCurrentState(_currentState.Parent);
                ActivateChildState(BrainStateNames.Battle);
            }
            else
            {
                // Fallback: try to activate Battle directly
                ActivateChildState(BrainStateNames.Battle);
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

        public void SetBattleChildStates()
        {
            var combatState = FindHighLevelState(BrainStateNames.Combat);
            if (combatState == null)
            {
#if UNITY_EDITOR
                Debug.LogError("Combat state not found.");
#endif
                return;
            }

            var childStates = new BrainState[]
            {
                new(BrainStateNames.PreBattle, combatState),
                new(BrainStateNames.Battle, combatState),
                new(BrainStateNames.PostBattle, combatState),
            };

            combatState.Children = childStates;
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

#if UNITY_EDITOR
            Debug.Log($"StateBrain: SetCurrentState -> {_currentState.Name}");
#endif
            _brain?.PublishStateChanged(_currentState);
        }

        public BrainState ActivateHighLevelState(string stateName)
        {
            var newState = FindHighLevelState(stateName);
            if (newState == null)
            {
#if UNITY_EDITOR
                Debug.LogError($"State '{stateName}' not found.");
#endif
                return null;
            }

            SetCurrentState(newState);
            return _currentState;
        }

        public void ActivateChildState(string childStateName)
        {
            if (_currentState == null)
            {
#if UNITY_EDITOR
                Debug.LogError("No active high-level state.");
#endif
                return;
            }

            // Determine the parent state to search in
            BrainState parentState =
                _currentState.Children != null && _currentState.Children.Length > 0
                    ? _currentState // Current state is a parent
                    : _currentState.Parent; // Current state is a child, use its parent

            if (parentState?.Children == null || parentState.Children.Length == 0)
            {
#if UNITY_EDITOR
                Debug.LogError(
                    $"Cannot find child state '{childStateName}' from '{_currentState.Name}'."
                );
#endif
                return;
            }

            var childState = Array.Find(parentState.Children, s => s.Name == childStateName);
            if (childState != null)
            {
                SetCurrentState(childState);
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogError(
                    $"Child state '{childStateName}' not found in '{parentState.Name}'."
                );
#endif
            }
        }

        public bool GetChildStates()
        {
            if (_currentState == null)
            {
#if UNITY_EDITOR
                Debug.LogError("No active high-level state.");
#endif
                return false;
            }

            if (_currentState.Children == null || _currentState.Children.Length == 0)
            {
#if UNITY_EDITOR
                Debug.Log("No child states available.");
#endif
                return false;
            }

            foreach (var child in _currentState.Children)
            {
#if UNITY_EDITOR
                Debug.Log($"Child State: {child.Name}");
#endif
            }
            return true;
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
#if UNITY_EDITOR
                Debug.LogError("Paused state not found.");
#endif
                return false;
            }

            if (isPaused)
            {
                _savedStateBeforePause = _currentState;
                SetCurrentState(pausedState);
                _brain?.PublishPaused(_savedStateBeforePause);
            }
            else
            {
                if (_savedStateBeforePause != null)
                {
                    SetCurrentState(_savedStateBeforePause);
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
