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

namespace TurnrootFramework.Gameplay.Brain
{
    public class StateBrain : MonoBehaviour
    {
        public static event Action<BrainState> ChangeState;
        public static event Action HighLevelStatesInitialized;
        public static event Action OnGameOver;
        public static event Action<BrainState> OnGamePaused;
        public static event Action<BrainState> OnGameResumed;
        private BrainState[] _highLevelStates;
        private BrainState _savedStateBeforePause;

        [SerializeField]
        private BrainState _currentState;
        public LongTermMemory longTermMemory;
        public BrainState CurrentState => _currentState;

        public void Awake()
        {
            Debug.Log("StateBrain Awake called.");
            longTermMemory =
                gameObject.GetComponent<LongTermMemory>()
                ?? gameObject.AddComponent<LongTermMemory>();

            InitializeHighLevelStates();
        }

        public void InitializeHighLevelStates()
        {
            if (_highLevelStates == null || _highLevelStates.Length == 0)
            {
                if (!TryRestoreHighLevelStates())
                {
                    SetHighLevelStates();
                    SaveHighLevelStates();
                    Debug.Log("High-level states set and saved to long-term memory.");
                }
            }
            else
            {
                Debug.Log("High-level states already initialized.");
            }
        }

        public void SetHighLevelStates()
        {
            var list = new System.Collections.Generic.List<BrainState>
            {
                new("Cutscene"),
                new("Paused"),
                new("Combat"),
                new("WorldMap"),
            };
#if TURNROOT_HUB_MODULE
            list.Add(new BrainState("Hub"));
#endif
            list.AddRange(
                new[]
                {
                    new BrainState("MainMenu"),
                    new BrainState("GameOver"),
                    new BrainState("Credits"),
                    new BrainState("NonCombatGameplay"),
                }
            );

            _highLevelStates = list.ToArray();
            SaveHighLevelStates();
            HighLevelStatesInitialized?.Invoke();
            Debug.Log("High-level states initialized.");
        }

        private bool TryRestoreHighLevelStates()
        {
            int storedCount = longTermMemory.RecallInt("StateBrain.HighLevelStates");
            if (storedCount <= 0)
                return false;

            for (int i = 0; i < storedCount; i++)
            {
                if (string.IsNullOrEmpty(longTermMemory.Recall("StateBrain.HighLevelState." + i)))
                    return false;
            }

            _highLevelStates = new BrainState[storedCount];
            for (int i = 0; i < storedCount; i++)
            {
                string name = longTermMemory.Recall("StateBrain.HighLevelState." + i);
                _highLevelStates[i] = new BrainState(name);
            }

            return true;
        }

        private void SaveHighLevelStates()
        {
            if (_highLevelStates == null)
                return;
            for (int i = 0; i < _highLevelStates.Length; i++)
                longTermMemory.Remember("StateBrain.HighLevelState." + i, _highLevelStates[i].Name);
            longTermMemory.RememberInt("StateBrain.HighLevelStates", _highLevelStates.Length);
        }

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

            ChangeState?.Invoke(_currentState);
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

            var childState =
                _currentState.ParentOfStates == null
                    ? null
                    : Array.Find(_currentState.ParentOfStates, s => s.Name == childStateName);
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

        public bool SetPausedState(bool isPaused)
        {
            var pausedState = FindHighLevelState("Paused");
            var _previousState = _currentState;
            if (pausedState == null)
            {
                Debug.LogError("Paused state not found.");
                return false;
            }

            if (isPaused)
            {
                _savedStateBeforePause = _previousState;
                SetCurrentState(pausedState);
                OnGamePaused?.Invoke(_previousState);
            }
            else
            {
                if (_savedStateBeforePause != null)
                {
                    SetCurrentState(_savedStateBeforePause);
                    OnGameResumed?.Invoke(_savedStateBeforePause);
                    _savedStateBeforePause = null;
                }
                else
                {
                    if (_currentState != null)
                        _currentState.IsActive = false;
                }
            }
            return true;
        }

        public void Pause()
        {
            SetPausedState(true);
        }

        public void Resume()
        {
            SetPausedState(false);
        }
    }
}
