using NaughtyAttributes;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using UnityEngine;

namespace Turnroot.Gameplay.Player
{
    public class PlayerController : MonoBehaviour
    {
        // For now (testing), this just holds a single TestUnitInstanceView
        public TestUnitInstanceView TestUnitView;

        // TODO: Get this from BattleBrain

        public TestUnitInstanceView EnemyTestUnitView;

        public BattleContextAIHelper EnemyAIHelper;

        // This is all testing stuff. Rewrite all this

        [Button]
        public void MoveTestUnitToPoint()
        {
            // 1. Execute the command (updates data immediately)
            Debug.Log("PlayerController MoveTestUnitToPoint called");
            Debug.Log(Brain.Brain.battleBrain.BattleObject); // TODO: BattleObject is null for some reason
            Debug.Log(Brain.Brain.battleBrain.BattleObject.Context);
            bool success = Brain.Brain.battleBrain.BattleObject.Context.MoveUnitToPoint(
                TestUnitView.CharacterDataInstance,
                TestUnitView.TestingGrid.GetGridPoint(MoveToPoint.x, MoveToPoint.y)
            );

            // 2. The command fires OnCharacterMoveCompleted event
            // TODO: 3. Something needs to listen to that event and tell the view to animate

            if (!success)
            {
                Debug.LogError("Move command failed!");
            }
        }

        [Button]
        public void EvaluateAIAction()
        {
            Debug.Log("PlayerController EvaluateAIAction called");
            if (EnemyAIHelper != null)
            {
                EnemyAIHelper.PickTileAndAction();
            }
            else
            {
                Debug.Log("No EnemyAIHelper assigned to PlayerController.");
            }
        }

        public Vector2Int MoveToPoint;

        public Brain.PlayerInputBrain Brain;

        public void Initialize()
        {
            Debug.Log("PlayerController started.");
            Brain.Brain.battleBrain.HandleStartBattle();
            Debug.Log("BattleBrain: Battle started from PlayerController.");
        }
    }
}
