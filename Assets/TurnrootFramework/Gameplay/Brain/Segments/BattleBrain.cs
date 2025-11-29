using Assets.Turnroot.Gameplay.Combat;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The battle brain manages one battle at a time.
/// It holds a map grid and all the points, features, and units.
/// It also stores BattleContext, BattleConditions, and BattleEnvironment data.
/// It is responsible for initializing the battle and managing turn order.
/// In keeping with the farfalle architecture, events are propagated upwards
/// to here, which then sends them out as needed.
/// </summary>
namespace Assets.Turnroot.Gameplay.Brain
{
    [RequireComponent(typeof(Brain))]
    public class BattleBrain : MonoBehaviour
    {
        private const string Prefix = "BattleBrain.";

        private Brain _brain;

        private void Awake()
        {
            _brain = GetComponent<Brain>();
            Debug.Log($"{Prefix} BattleBrain Awake - subscribing to OnStartBattle.");
        }

        public void SubscribeToBrainEvents()
        {
            _brain.OnStartBattle += HandleStartBattle;
            _brain.OnExitBattle += HandleExitBattle;
        }

        private void HandleStartBattle()
        {
            Debug.Log($"{Prefix} Handling StartBattle event.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                {
                    GameObject[] rootObjects = scene.GetRootGameObjects();
                    foreach (GameObject rootObject in rootObjects)
                    {
                        BattleGameObject battleGameObject =
                            rootObject.GetComponentInChildren<BattleGameObject>();
                        if (battleGameObject != null)
                        {
                            battleGameObject.Brain = _brain;
                            battleGameObject.ConnectToBrainEvents();
                            Debug.Log($"{Prefix} Found BattleGameObject in scene '{scene.name}'.");
                            break;
                        }
                    }
                }
            }
        }

        private void HandleExitBattle(BattleExitType exitType)
        {
            Debug.Log($"{Prefix} Handling ExitBattle event with exit type: {exitType}.");
        }

        public void OnDestroy()
        {
            if (_brain != null)
            {
                Debug.Log($"{Prefix} BattleBrain OnDestroy - unsubscribing from OnStartBattle.");
                _brain.OnStartBattle -= HandleStartBattle;
                _brain.OnExitBattle -= HandleExitBattle;
            }
        }
    }
}
