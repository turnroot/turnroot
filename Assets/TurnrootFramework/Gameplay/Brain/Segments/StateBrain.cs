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

namespace Assets.Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Manages high-level game states and transitions within the brain system.
    /// LongTermMemory persists state information.
    /// Brains have a farfalle structure >< :)
    /// </summary>
    [RequireComponent(typeof(Brain))]
    public class StateBrain : MonoBehaviour
    {
        private static class LtmKeys
        {
            public const string HighLevelStatesCount = "StateBrain.HighLevelStates";
            public const string HighLevelStatePrefix = "StateBrain.HighLevelState.";
        }

        [SerializeField]
        private BrainState _currentState;
        public BrainState CurrentState => _currentState;

        private BrainState[] _highLevelStates;
        private BrainState _savedStateBeforePause;
        private Brain _brain;

        #region Initialization

        public void Awake()
        {
            Debug.Log("StateBrain Awake called.");
            _brain = GetComponent<Brain>();
            InitializeHighLevelStates();
        }

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

        public void SetHighLevelStates()
        {
            var states = new System.Collections.Generic.List<BrainState>
            {
                new BrainState("Cutscene"),
                new BrainState("Paused"),
                new BrainState("Combat"),
                new BrainState("WorldMap"),
            };

#if TURNROOT_CAMP_MODULE
            states.Add(new BrainState("Hub"));
#endif

            states.AddRange(
                new[]
                {
                    new BrainState("MainMenu"),
                    new BrainState("GameOver"),
                    new BrainState("Credits"),
                    new BrainState("NonCombatGameplay"),
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
                return false;

            int storedCount = _brain.ltm.RecallInt(LtmKeys.HighLevelStatesCount);
            if (storedCount <= 0)
                return false;

            if (!ValidateStoredStates(storedCount))
                return false;

            _highLevelStates = new BrainState[storedCount];
            for (int i = 0; i < storedCount; i++)
            {
                string name = _brain.ltm.Recall(LtmKeys.HighLevelStatePrefix + i);
                _highLevelStates[i] = new BrainState(name);
            }

            return true;
        }

        private bool ValidateStoredStates(int count)
        {
            for (int i = 0; i < count; i++)
            {
                string stateName = _brain.ltm.Recall(LtmKeys.HighLevelStatePrefix + i);
                if (string.IsNullOrEmpty(stateName))
                    return false;
            }
            return true;
        }

        private void SaveHighLevelStates()
        {
            if (_highLevelStates == null || _brain?.ltm == null)
                return;

            for (int i = 0; i < _highLevelStates.Length; i++)
            {
                _brain.ltm.Remember(LtmKeys.HighLevelStatePrefix + i, _highLevelStates[i].Name);
            }

            _brain.ltm.RememberInt(LtmKeys.HighLevelStatesCount, _highLevelStates.Length);
        }

        #endregion

        #region State Management

        private BrainState FindHighLevelState(string name)
        {
            if (string.IsNullOrEmpty(name) || _highLevelStates == null)
                return null;

            return Array.Find(_highLevelStates, s => s.Name == name);
        }

        private void SetCurrentState(BrainState newState)
        {
            if (newState == null)
                return;

            if (_currentState != null)
                _currentState.IsActive = false;

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

        public void Pause()
        {
            SetPausedState(true);
        }

        public void Resume()
        {
            SetPausedState(false);
        }

        private bool SetPausedState(bool isPaused)
        {
            var pausedState = FindHighLevelState("Paused");
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

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_brain == null)
                _brain = GetComponent<Brain>();
        }
#endif
    }
}
