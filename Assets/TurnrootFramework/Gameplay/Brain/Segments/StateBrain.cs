using System;
using UnityEngine;

public class BrainState
{
    public string Name { get; private set; }
    public BrainState[] ChildOfState { get; set; }
    public BrainState[] ParentOfStates { get; set; }
    public bool IsActive { get; set; }

    public BrainState(
        string name,
        BrainState[] childOfState = null,
        BrainState[] parentOfStates = null
    )
    {
        Name = name;
        ChildOfState = childOfState;
        ParentOfStates = parentOfStates;
        IsActive = false;
    }
}

/// <summary>
/// Constant state names for type safety and refactoring ease.
/// </summary>
public static class BrainStateNames
{
    // High-level states
    public const string Combat = "Combat";
    public const string Paused = "Paused";
    public const string Cutscene = "Cutscene";
    public const string WorldMap = "WorldMap";
    public const string MainMenu = "MainMenu";
    public const string GameOver = "GameOver";
    public const string Credits = "Credits";
    public const string NonCombatGameplay = "NonCombatGameplay";
    public const string Hub = "Hub";

    // Battle child states
    public const string PreBattle = "PreBattle";
    public const string PlayerTurn = "PlayerTurn";
    public const string EnemyTurn = "EnemyTurn";
    public const string ThirdPartyTurn = "ThirdPartyTurn";
    public const string SpecialCircumstances = "SpecialCircumstances";
    public const string PostBattle = "PostBattle";
}

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Manages high-level game states and transitions within the brain system.
    /// </summary>
    public class StateBrain : BrainComponent
    {
        // LtmKeys are now centralized in LtmKeys.cs

        [SerializeField]
        private BrainState _currentState;
        public BrainState CurrentState => _currentState;

        private BrainState[] _highLevelStates;
        private BrainState _savedStateBeforePause;

        protected override void Awake()
        {
            base.Awake(); // Calls parent Awake
            Debug.Log("StateBrain Awake called.");
            InitializeHighLevelStates();
            InitializeBattleChildStates();
        }

        protected override void SubscribeToBrainEvents()
        {
            // StateBrain doesn't subscribe to events, it publishes them
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            // No subscriptions to clean up
        }

        #region Initialization

        public void InitializeHighLevelStates()
        {
            if (_highLevelStates != null && _highLevelStates.Length > 0)
            {
                Debug.Log("High-level states already initialized.");
                return;
            }

            if (!TryRestoreHighLevelStates())
            {
                SetHighLevelStates();
                SaveHighLevelStates();
                Debug.Log("High-level states set and saved to long-term memory.");
            }
        }

        public void InitializeBattleChildStates()
        {
            var battleState = FindHighLevelState(BrainStateNames.Combat);
            if (battleState == null)
            {
                Debug.LogError("Combat state not found. Cannot initialize battle child states.");
                return;
            }

            if (battleState.ParentOfStates != null && battleState.ParentOfStates.Length > 0)
            {
                Debug.Log("Battle child states already initialized.");
                return;
            }

            if (!TryRestoreBattleChildStates())
            {
                SetBattleChildStates();
                SaveBattleChildStates();
                Debug.Log("Battle child states set and saved.");
            }
        }

        public void SetBattleChildStates()
        {
            var battleState = FindHighLevelState(BrainStateNames.Combat);
            if (battleState == null)
            {
                Debug.LogError("Combat state not found.");
                return;
            }

            var childStates = new System.Collections.Generic.List<BrainState>
            {
                new(BrainStateNames.PreBattle, null, new[] { battleState }),
                new(BrainStateNames.PlayerTurn, null, new[] { battleState }),
                new(BrainStateNames.EnemyTurn, null, new[] { battleState }),
                new(BrainStateNames.ThirdPartyTurn, null, new[] { battleState }),
                new(BrainStateNames.SpecialCircumstances, null, new[] { battleState }),
                new(BrainStateNames.PostBattle, null, new[] { battleState }),
            };

            battleState.ParentOfStates = childStates.ToArray();
            Debug.Log("Battle child states initialized.");
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
            SaveHighLevelStates();
            _brain?.PublishHighLevelStatesInitialized();
            Debug.Log("High-level states initialized.");
        }

        private bool TryRestoreHighLevelStates()
        {
            if (_brain?.ltm == null)
            {
                return false;
            }

            int storedCount = _brain.ltm.RecallInt(LtmKeys.HighLevelStatesCount);
            if (storedCount <= 0)
            {
                return false;
            }

            if (!ValidateStoredStates(storedCount))
            {
                return false;
            }

            _highLevelStates = new BrainState[storedCount];
            for (int i = 0; i < storedCount; i++)
            {
                string name = _brain.ltm.Recall(LtmKeys.HighLevelStatePrefix + i);
                _highLevelStates[i] = new BrainState(name);
            }

            return true;
        }

        private bool TryRestoreBattleChildStates()
        {
            var battleState = FindHighLevelState(BrainStateNames.Combat);
            if (battleState == null)
            {
                Debug.LogError("Combat state not found.");
                return false;
            }

            if (_brain?.ltm == null)
            {
                return false;
            }

            var childStates = new System.Collections.Generic.List<BrainState>();
            int index = 0;
            while (true)
            {
                string key = $"{LtmKeys.HighLevelStatePrefix}Combat.Child.{index}";
                string stateName = _brain.ltm.Recall(key);
                if (string.IsNullOrEmpty(stateName))
                {
                    break;
                }

                childStates.Add(new BrainState(stateName, null, new[] { battleState }));
                index++;
            }

            if (childStates.Count == 0)
            {
                return false;
            }

            battleState.ParentOfStates = childStates.ToArray();
            return true;
        }

        private bool ValidateStoredStates(int count)
        {
            for (int i = 0; i < count; i++)
            {
                string stateName = _brain.ltm.Recall(LtmKeys.HighLevelStatePrefix + i);
                if (string.IsNullOrEmpty(stateName))
                {
                    return false;
                }
            }
            return true;
        }

        private void SaveHighLevelStates()
        {
            if (_highLevelStates == null || _brain?.ltm == null)
            {
                return;
            }

            for (int i = 0; i < _highLevelStates.Length; i++)
            {
                _brain.ltm.Remember(LtmKeys.HighLevelStatePrefix + i, _highLevelStates[i].Name);
            }

            _brain.ltm.RememberInt(LtmKeys.HighLevelStatesCount, _highLevelStates.Length);
        }

        private void SaveBattleChildStates()
        {
            var battleState = FindHighLevelState(BrainStateNames.Combat);
            if (battleState == null || battleState.ParentOfStates == null || _brain?.ltm == null)
            {
                return;
            }

            for (int i = 0; i < battleState.ParentOfStates.Length; i++)
            {
                string key = $"{LtmKeys.HighLevelStatePrefix}Combat.Child.{i}";
                _brain.ltm.Remember(key, battleState.ParentOfStates[i].Name);
            }
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

            _brain?.PublishStateChanged(_currentState);
        }

        public BrainState ActivateHighLevelState(string stateName)
        {
            var newState = FindHighLevelState(stateName);
            if (newState == null)
            {
                Debug.LogError($"State '{stateName}' not found.");
                return null;
            }

            SetCurrentState(newState);
            return _currentState;
        }

        public void ActivateChildState(string childStateName)
        {
            if (_currentState == null)
            {
                Debug.LogError("No active high-level state.");
                return;
            }

            if (_currentState.ParentOfStates == null)
            {
                Debug.LogError($"Child state '{childStateName}' not found.");
                return;
            }

            var childState = Array.Find(
                _currentState.ParentOfStates,
                s => s.Name == childStateName
            );
            if (childState != null)
            {
                SetCurrentState(childState);
            }
            else
            {
                Debug.LogError($"Child state '{childStateName}' not found.");
            }
        }

        public bool GetChildStates()
        {
            if (_currentState == null)
            {
                Debug.LogError("No active high-level state.");
                return false;
            }

            if (_currentState.ParentOfStates == null || _currentState.ParentOfStates.Length == 0)
            {
                Debug.Log("No child states available.");
                return false;
            }

            foreach (var child in _currentState.ParentOfStates)
            {
                Debug.Log($"Child State: {child.Name}");
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
                Debug.LogError("Paused state not found.");
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
