using Turnroot.Characters;
using Turnroot.Characters.Components;
using Turnroot.Gameplay.Combat;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// The battle brain manages one battle at a time.
    /// It is responsible for initializing the battle and managing turn order.
    /// </summary>
    public class BattleBrain : BrainComponent
    {
        private BattleGameObject _battleGameObject;

        public BattleGameObject BattleObject => _battleGameObject;
        private TurnRotisserie _turnRotisserie;

        // Accessor for current battle's rosters through BattleGameObject
        public RosterInstance PlayerTeamRoster => _battleGameObject?.PlayerTeamRoster;
        public RosterInstance EnemyTeamRoster => _battleGameObject?.EnemyTeamRoster;
        public RosterInstance ThirdPartyTeamRoster => _battleGameObject?.ThirdPartyTeamRoster;

        protected override void Awake()
        {
            base.Awake(); // Calls parent Awake which gets Brain and subscribes

            _turnRotisserie = GetComponent<TurnRotisserie>();
            if (_turnRotisserie == null)
            {
                _turnRotisserie = gameObject.AddComponent<TurnRotisserie>();
            }
            _turnRotisserie.Brain = _brain;
            Debug.Log("BattleBrain TurnRotisserie is ready.");
        }

        protected override void SubscribeToBrainEvents()
        {
            _brain.OnStartBattle += HandleStartBattle;
            _brain.OnExitBattle += HandleExitBattle;
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            _brain.OnStartBattle -= HandleStartBattle;
            _brain.OnExitBattle -= HandleExitBattle;
        }

        public void ProgressTurnOrder()
        {
            if (!_turnRotisserie.Progress())
            {
                Debug.LogError("BattleBrain: Failed to progress turn order!");
                Debug.Break();
            }
        }

        private void HandleStartBattle()
        {
            Debug.Log("BattleBrain: Handling StartBattle event.");

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
                                $"BattleBrain: Found BattleGameObject in scene '{scene.name}'."
                            );
                            _turnRotisserie.HasThirdParty = _battleGameObject.HasThirdParty;

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
            Debug.Log($"BattleBrain: Handling ExitBattle event with exit type: {exitType}.");
            _battleGameObject?.ClearBattleRosters();
        }
    }
}
