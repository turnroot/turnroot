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
        private BrainState[] _highLevelStates;
        private BrainState _currentState;

        public void OnEnable()
        {
            SetHighLevelStates();
        }

        public void SetHighLevelStates()
        {
            var cutscene = new BrainState("Cutscene");
            var paused = new BrainState("Paused");
            var combat = new BrainState("Combat");
            var worldMap = new BrainState("WorldMap");
#if TURNROOT_HUB_MODULE
            var hub = new BrainState("Hub");
#endif
            var mainMenu = new BrainState("MainMenu");
            var gameOver = new BrainState("GameOver");
            var credits = new BrainState("Credits");
            var nonCombatGameplay = new BrainState("NonCombatGameplay");

            _highLevelStates = new[]
            {
                cutscene,
                paused,
                combat,
                worldMap,
#if TURNROOT_HUB_MODULE
                hub,
#endif
                mainMenu,
                gameOver,
                credits,
                nonCombatGameplay,
            };
            HighLevelStatesInitialized?.Invoke();
            Debug.Log("High-level states initialized.");
        }

        public BrainState ActivateHighLevelState(string stateName)
        {
            var newState = Array.Find(_highLevelStates, s => s.Name == stateName);
            if (newState != null)
            {
                _currentState = newState;
                _currentState.IsActive = true;
                return _currentState;
            }
            else
            {
                Debug.LogError($"State '{stateName}' not found.");
                return null;
            }
        }

        public void ActivateChildState(string childStateName)
        {
            if (_currentState == null)
            {
                Debug.LogError("No active high-level state.");
                return;
            }

            var childState = Array.Find(
                _currentState.ParentOfStates,
                s => s.Name == childStateName
            );
            if (childState != null)
            {
                _currentState.IsActive = false;
                _currentState = childState;
                _currentState.IsActive = true;
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
    }
}

/*
A public state event Action<> is like a UnityEvent, but script-accessible instead.
I can use it to notify other parts of the game when a state changes, such as when the player takes damage or the game ends.
I can define events in the Brain, and then other scripts can call them or subscribe to them to respond to state changes.

public static event Action<string> OnGameOver;

public void TakeDamage(float damage)
{
    health -= damage;
    if(health < 0)
    {
        Brain.OnGameOver?.Invoke("The game is over");
    }
}

public class GameController : MonoBehaviour
{
    void RestartGame()
    {
        // Restart the game!
    }

    private void OnEnable()
    {
        PlayerHealth.onGameOver += RestartGame;
    }

    private void OnDisable()
    {
        PlayerHealth.onGameOver -= RestartGame;
    }
    Publisher (already in StateBrain):
public static event Action<BrainState> ChangeState;
// Raise:
ChangeState?.Invoke(currentState);

Subscriber:
private void OnEnable() { StateBrain.ChangeState += HandleChange; }
private void OnDisable() { StateBrain.ChangeState -= HandleChange; }
private void HandleChange(BrainState s) {
     Do something with the new state}
}
*/
