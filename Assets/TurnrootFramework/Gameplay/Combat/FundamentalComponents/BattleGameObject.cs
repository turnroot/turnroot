using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components.Battle;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Environment;
using Turnroot.Gameplay.Combat.PreBattle;
using Turnroot.Gameplay.Combat.Precompute;
using Turnroot.Gameplay.Maps;
using UnityEngine;

namespace Turnroot.Gameplay.Combat
{
    public enum BattleExitType
    {
        Victory,
        Defeat,
        Retreat,
        Bookmark,
    }

    [RequireComponent(typeof(EnvironmentalConditions))]
    [RequireComponent(typeof(BattleContext))]
    [RequireComponent(typeof(BattlePreparationObject))]
    [RequireComponent(typeof(TileHighlighter))]
    [RequireComponent(typeof(BattlePrecomputeLoader))]
    public partial class BattleGameObject : MonoBehaviour
    {
        [field: SerializeField, HideInInspector]
        public BattleContext Context { get; private set; }

        [Header("Battle Teams"), HorizontalLine(color: EColor.Indigo)]
        [SerializeField]
        private bool _hasThirdParty;
        public bool HasThirdParty
        {
            get => _hasThirdParty;
            set => _hasThirdParty = value;
        }

        [ShowIf(nameof(HasThirdParty))]
        public bool ThirdPartyFightsAllies;

        [ShowIf(nameof(HasThirdParty))]
        public bool ThirdPartyFightsEnemies;

        [Range(1, 16)]
        public int MaxPlayerTeamUnits;

        [SerializeField]
        private List<CharacterData> _requiredPlayerUnits = new();
        public List<CharacterData> RequiredPlayerUnits
        {
            get => _requiredPlayerUnits;
            set => _requiredPlayerUnits = value;
        }

        public EnvironmentalConditions EnvironmentalConditions =>
            GetComponent<EnvironmentalConditions>();

        [SerializeField, SerializeReference]
        private BattleCondition[] _battleConditions;
        public BattleCondition[] BattleConditions => _battleConditions;

        [field: SerializeField]
        public MapGrid MapGrid { get; private set; }

        [Header("Roster Templates"), HorizontalLine(color: EColor.Blue)]
        [SerializeField]
        private GenericRoster _enemyRoster;

        [SerializeField]
        private GenericRoster _thirdPartyRoster;

        [SerializeField, HideInInspector]
        private int _currentTurnCount;

        [field: HideInInspector]
        public Brain.Brain Brain { get; set; }

        public PlayerTeamRosterInstance PlayerTeamRoster { get; private set; }
        public GenericRosterInstance EnemyTeamRoster { get; private set; }
        public GenericRosterInstance ThirdPartyTeamRoster { get; private set; }

        public LayerMask GroundLayerMask;

        private bool _isConnectedToBrain;

        #region Unity Lifecycle

        public void Awake()
        {
            ResetTurnCount();
            Context ??= GetComponent<BattleContext>();
            MapGrid ??= GetComponentInChildren<MapGrid>();

            ValidateRequiredComponents();
        }

        private void ValidateRequiredComponents()
        {
            if (MapGrid == null)
            {
                Debug.LogError("BattleGameObject requires a MapGrid child");
                Debug.Break();
            }

            if (_battleConditions == null)
            {
                Debug.LogError("BattleGameObject requires BattleConditions to be set");
                Debug.Break();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Context ??= GetComponent<BattleContext>();
            if (_battleConditions == null)
            {
                return;
            }

            foreach (var condition in _battleConditions)
            {
                if (condition == null)
                {
                    continue;
                }

                condition.battleContext = Context;

                try
                {
                    condition.ResolveRequiredConditions(_battleConditions);
                    if (condition is ConditionalGroupBattleCondition group)
                    {
                        group.ResolveChildConditions(_battleConditions);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning(
                        $"Failed to resolve condition {condition?.Name}: {ex.Message}"
                    );
                }
            }
        }
#endif

        #endregion

        #region Event Handlers

        private void HandleTurnEnded()
        {
            IncrementTurnCount();

            foreach (var condition in _battleConditions.OfType<SurviveTurnsBattleCondition>())
            {
                condition.OnTurnEnd();
            }

            foreach (var condition in _battleConditions.OfType<TimeLimitBattleCondition>())
            {
                condition.OnTurnEnd();
            }
        }

        private void HandleAllyDamaged(CharacterInstance unit, int damage)
        {
            foreach (
                var condition in _battleConditions.OfType<LimitTotalAllyDamageBattleCondition>()
            )
            {
                condition.OnAllyDamaged(damage);
            }
        }

        private void HandleEnemyDamaged(CharacterInstance unit, int damage)
        {
            foreach (
                var condition in _battleConditions.OfType<DealMinimumTotalEnemyDamageBattleCondition>()
            )
            {
                condition.OnEnemyDamaged(damage);
            }
        }

        private void HandleUnitDefeated(CharacterInstance unit)
        {
            InvalidateAllConditionCaches();
            ClearAICache();

            CheckDefeatConditions();
        }

        private void CheckDefeatConditions()
        {
            foreach (var condition in _battleConditions.OfType<DefeatAllEnemiesBattleCondition>())
            {
                condition.CheckCondition();
            }

            foreach (var condition in _battleConditions.OfType<DefeatEnemyBattleCondition>())
            {
                condition.CheckCondition();
            }

            foreach (var condition in _battleConditions.OfType<ProtectNPCsBattleCondition>())
            {
                condition.CheckCondition();
            }

#if TURNROOT_MONSTERS_MODULE
            foreach (var condition in _battleConditions.OfType<DefeatAllMonstersBattleCondition>())
            {
                // TODO: condition.CheckCondition();
            }

            foreach (var condition in _battleConditions.OfType<DefeatMonsterBattleCondition>())
                condition.CheckCondition();
#endif
        }

        private void HandleUnitMoved(CharacterInstance unit, Vector2Int newPos)
        {
            CheckMovementConditions(unit, newPos);
            ClearAICache();
        }

        private void CheckMovementConditions(CharacterInstance unit, Vector2Int pos)
        {
            foreach (var condition in _battleConditions)
            {
                if (condition is SurviveUntilAllyReachesTileBattleCondition reachTile)
                {
                    reachTile.CheckCondition();
                }

                if (condition is ReachTilesBattleCondition reachTiles)
                {
                    bool isPlayerUnit = PlayerTeamRoster?.Instances?.Contains(unit) ?? false;
                    if (isPlayerUnit)
                    {
                        reachTiles.OnUnitReachedTile(pos);
                    }
                }
            }
        }

        private void HandleExitBattle(BattleExitType exitType) => DisconnectFromBrainEvents();

        private void HandleUnitSpawnedEvent(UnitSpawnedEvent evt)
        {
            InvalidateAllConditionCaches();
            ClearAICache();

            foreach (var condition in _battleConditions.OfType<DefeatEnemyBattleCondition>())
            {
                condition.CheckCondition();
            }

            foreach (var condition in _battleConditions.OfType<DefeatAllEnemiesBattleCondition>())
            {
                condition.CheckCondition();
            }
        }

        private void HandleUnitDefeatedEvent(UnitDefeatedEvent evt) => HandleUnitDefeated(evt.Unit);

        private void InvalidateAllConditionCaches()
        {
            foreach (var condition in _battleConditions)
            {
                try
                {
                    condition?.InvalidateCache();
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning(
                        $"Failed to invalidate cache for {condition?.Name}: {ex.Message}"
                    );
                }
            }
        }

        private void ClearAICache()
        {
            try
            {
                Brain?.battleBrain?.ClearAICache();
            }
            catch { }
        }

        private void HandleMapStateChanged() => ClearAICache();

        private void OnDestroy() => DisconnectFromBrainEvents();

        #endregion

        #region Turn Management

        public void IncrementTurnCount() => _currentTurnCount++;

        public void ResetTurnCount() => _currentTurnCount = 0;

        public int Turns() => _currentTurnCount;

        #endregion
    }
}
