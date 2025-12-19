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
            Debug.Log("PlayerController MoveTestUnitToPoint called");
            if (TestUnitView != null)
            {
                TestUnitView.MoveUnitToPoint(MoveToPoint);
                Brain.Brain.PublishCharacterMoveCompleted(
                    TestUnitView.CharacterDataInstance,
                    TestUnitView.TestingGrid.GetGridPoint(MoveToPoint.x, MoveToPoint.y)
                // TODO: Move the event publishing to PlayerInputBrain. This is bad
                );
            }
            else
            {
                Debug.Log("No TestUnitView assigned to PlayerController.");
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
