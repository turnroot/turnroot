using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Environment;
using UnityEngine;

namespace Assets.Turnroot.Gameplay.Combat
{
    public enum BattleExitType
    {
        Victory,
        Defeat,
        Retreat,
        Bookmark,
    }

    [RequireComponent(typeof(EnvironmentalConditions))]
    public class BattleGameObject : MonoBehaviour
    {
        public bool HasThirdParty;

        [Header("Battle Components")]
        [SerializeField]
        private BattleContext _battleContext;

        [SerializeField, SerializeReference]
        private BattleCondition[] _battleConditions;

        [SerializeField]
        private MapGrid _mapGrid;

        [SerializeField, NaughtyAttributes.ReadOnly]
        private int _currentTurnCount;

        [HideInInspector]
        private Brain.Brain _brain;
        public Brain.Brain Brain
        {
            get => _brain;
            set => _brain = value;
        }

        public void ConnectToBrainEvents()
        {
            if (_brain != null)
            {
                Debug.Log("BattleGameObject connecting to Brain events.");
                // TODO: Subscribe to relevant Brain events here
            }
            else
            {
                Debug.LogWarning("BattleGameObject has no Brain to connect to.");
            }
        }

        public void Awake()
        {
            ResetTurnCount();
            _battleContext ??= new BattleContext();
            _mapGrid = _mapGrid != null ? _mapGrid : GetComponentInChildren<MapGrid>();
            if (_mapGrid == null)
            {
                Debug.LogError("BattleGameObject requires a MapGrid child.");
                Debug.Break();
            }
            if (_battleConditions == null)
            {
                Debug.LogError("BattleGameObject requires BattleConditions to be set.");
                Debug.Break();
            }
        }

        public void IncrementTurnCount()
        {
            _currentTurnCount++;
        }

        public void ResetTurnCount()
        {
            _currentTurnCount = 0;
        }

        public int Turns()
        {
            return _currentTurnCount;
        }
    }
}
