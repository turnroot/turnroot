using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Environment;
using UnityEngine;

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

        [Header("Battle Components")]
        [SerializeField]
        private BattleContext _battleContext;

        [SerializeField]
        private BattleCondition[] _battleConditions;

        [SerializeField]
        private EnvironmentalConditions _battleEnvironment;

        [SerializeField]
        private MapGrid _mapGrid;

        private void Awake()
        {
            _brain = GetComponent<Brain>();
            InitializeBattleContext();
        }

        private void InitializeBattleContext()
        {
            _battleContext = new BattleContext();
            _brain?.PublishBattleContextInitialized(_battleContext);
        }
    }
}
