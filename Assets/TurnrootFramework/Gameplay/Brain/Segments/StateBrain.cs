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
    [RequireComponent(typeof(Brain))]
    /// <summary>
    /// Manages high-level game states and transitions within the brain system.
    /// LongTermMemory persists state information.
    /// Brains have a farfalle structure >< :)
    /// </summary>
    public class StateBrain : MonoBehaviour
    {
        // track the LTM key cache version so state brain can react if needed
        private int lastKnownLtmKeyCacheVersion = 0;
        private BrainState[] _highLevelStates;
        private BrainState _savedStateBeforePause;
        private Brain _brain;

        [SerializeField]
        private BrainState _currentState;
        public BrainState CurrentState => _currentState;

        public void Awake()
        {
            Debug.Log("StateBrain Awake called.");
            if (_brain == null)
                _brain = GetComponent<Brain>();
            try
            {
                if (_brain != null)
                {
                    _brain.OnLtmKeyCacheUpdated += OnLtmKeyCacheUpdated;
                    try
                    {
                        lastKnownLtmKeyCacheVersion =
                            _brain.ltm?.KeyCacheVersion ?? lastKnownLtmKeyCacheVersion;
                    }
                    catch { }
                }
            }
            catch { }

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
#if TURNROOT_CAMP_MODULE
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
            _brain?.PublishHighLevelStatesInitialized();
            Debug.Log("High-level states initialized.");
        }

        private bool TryRestoreHighLevelStates()
        {
            int storedCount = _brain.ltm.RecallInt("StateBrain.HighLevelStates");
            if (storedCount <= 0)
                return false;

            for (int i = 0; i < storedCount; i++)
            {
                if (string.IsNullOrEmpty(_brain.ltm.Recall("StateBrain.HighLevelState." + i)))
                    return false;
            }

            _highLevelStates = new BrainState[storedCount];
            for (int i = 0; i < storedCount; i++)
            {
                string name = _brain.ltm.Recall("StateBrain.HighLevelState." + i);
                _highLevelStates[i] = new BrainState(name);
            }

            return true;
        }

        private void SaveHighLevelStates()
        {
            if (_highLevelStates == null)
                return;
            for (int i = 0; i < _highLevelStates.Length; i++)
            {
                var saved = _brain.ltm.Remember(
                    "StateBrain.HighLevelState." + i,
                    _highLevelStates[i].Name
                );
            }
            var savedCount = _brain.ltm.RememberInt(
                "StateBrain.HighLevelStates",
                _highLevelStates.Length
            );
            try
            {
                lastKnownLtmKeyCacheVersion = _brain.ltm.KeyCacheVersion;
            }
            catch { }
            // LongTermMemory will publish keyset changes and Brain will forward them if required.
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

        private bool SetPausedState(bool isPaused)
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
                _brain?.PublishPaused(_previousState);
            }
            else
            {
                if (_savedStateBeforePause != null)
                {
                    SetCurrentState(_savedStateBeforePause);
                    _brain?.PublishResumed(_savedStateBeforePause);
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

        /// <summary>
        /// Pauses the game by setting the current state to "Paused", saving the previous state, and invoking the OnGamePaused event.
        /// </summary>
        public void Pause()
        {
            SetPausedState(true);
        }

        /// <summary>
        /// Resumes the game by restoring the previous state before the pause and invoking the OnGameResumed event.
        /// </summary>
        public void Resume()
        {
            SetPausedState(false);
        }

        private void OnDestroy()
        {
            try
            {
                if (_brain != null)
                    _brain.OnLtmKeyCacheUpdated -= OnLtmKeyCacheUpdated;
            }
            catch { }
        }

        private void OnLtmKeyCacheUpdated(int version)
        {
            try
            {
                lastKnownLtmKeyCacheVersion = version;
            }
            catch { }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-assign Brain on same GameObject to make inspector setup easier
            if (_brain == null)
                _brain = GetComponent<Brain>();
        }
#endif
    }
}
