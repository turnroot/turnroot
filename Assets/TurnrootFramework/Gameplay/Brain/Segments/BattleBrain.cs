using Assets.Turnroot.Characters;
using Assets.Turnroot.Gameplay.Combat;
using Turnroot.Characters;
using Turnroot.Characters.Components;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The battle brain manages one battle at a time.
/// It is responsible for initializing the battle and managing turn order.
/// In keeping with the farfalle architecture, events are propagated upwards
/// to here, which then sends them out as needed.
/// </summary>
namespace Assets.Turnroot.Gameplay.Brain
{
    [RequireComponent(typeof(Brain))]
    [RequireComponent(typeof(TurnRotisserie))]
    public class BattleBrain : MonoBehaviour
    {
        private Brain _brain;

        private BattleGameObject _battleGameObject;

        private TurnRotisserie _turnRotisserie;

        // Accessor for current battle's rosters through BattleGameObject
        public RosterInstance PlayerTeamRoster => _battleGameObject?.PlayerTeamRoster;
        public RosterInstance EnemyTeamRoster => _battleGameObject?.EnemyTeamRoster;
        public RosterInstance ThirdPartyTeamRoster => _battleGameObject?.ThirdPartyTeamRoster;

        private void Awake()
        {
            _brain = GetComponent<Brain>();
            Debug.Log($"BattleBrain Awake - subscribing to OnStartBattle.");
            SubscribeToBrainEvents();

            _turnRotisserie = GetComponent<TurnRotisserie>();
            _turnRotisserie.Brain = _brain;
            Debug.Log($"BattleBrain TurnRotisserie is ready.");
        }

        public void ProgressTurnOrder()
        {
            if (!_turnRotisserie.Progress())
            {
                Debug.Break();
            }
        }

        public void SubscribeToBrainEvents()
        {
            _brain.OnStartBattle += HandleStartBattle;
            _brain.OnExitBattle += HandleExitBattle;
        }

        private void HandleStartBattle()
        {
            Debug.Log($"BattleBrain Handling StartBattle event.");

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                {
                    GameObject[] rootObjects = scene.GetRootGameObjects();
                    foreach (GameObject rootObject in rootObjects)
                    {
                        _battleGameObject = rootObject.GetComponentInChildren<BattleGameObject>();
                        if (_battleGameObject != null)
                        {
                            _battleGameObject.Brain = _brain;
                            _battleGameObject.ConnectToBrainEvents();
                            _battleGameObject.ConnectBattleConditionsToGamewideContextBrain();
                            Debug.Log(
                                $"BattleBrain Found BattleGameObject in scene '{scene.name}'."
                            );
                            _turnRotisserie.HasThirdParty = _battleGameObject.HasThirdParty;

                            // Initialize and populate battle rosters on BattleGameObject
                            _battleGameObject.InitializeBattleRosters();
                            _battleGameObject.PopulateBattleRostersFromGamewideContext(
                                _brain.gamewideContextBrain
                            );
                            break;
                        }
                    }
                }
            }
        }

        private void HandleExitBattle(BattleExitType exitType)
        {
            Debug.Log($"BattleBrain Handling ExitBattle event with exit type: {exitType}.");

            // Clear temporary battle rosters
            _battleGameObject?.ClearBattleRosters();
        }

        public void OnDestroy()
        {
            if (_brain != null)
            {
                Debug.Log($"BattleBrain OnDestroy - unsubscribing from brain events.");
                _brain.OnStartBattle -= HandleStartBattle;
                _brain.OnExitBattle -= HandleExitBattle;
            }
        }
    }
}
