using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
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
            _brain.OnStartBattle += FindBattle;
        }

        public void FindBattle()
        {
            BattleGameObject battleGameObject = null;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                {
                    GameObject[] rootObjects = scene.GetRootGameObjects();
                    foreach (GameObject rootObject in rootObjects)
                    {
                        BattleGameObject bgo =
                            rootObject.GetComponentInChildren<BattleGameObject>();
                        if (bgo != null)
                        {
                            battleGameObject = bgo;
                            Debug.Log($"{Prefix} Found BattleGameObject in scene '{scene.name}'.");
                            break;
                        }
                    }
                }
            }
        }
    }
}
