using System.Collections.Generic;
using Turnroot.Gameplay.Brain.Components.Battle;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Gameplay.Maps;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat
{
    public partial class BattleGameObject : MonoBehaviour
    {
        #region Brain Connection

        public OperationResult ConnectToBrainEvents()
        {
            if (Brain == null)
            {
                return OperationResult.Failure("Brain reference is null");
            }

            if (_isConnectedToBrain)
            {
                return OperationResult.Failure("Already connected to Brain events");
            }

            InitializeContextWithBrain();
            SubscribeToBrainEvents();

            _isConnectedToBrain = true;
            return OperationResult.SuccessResult();
        }

        private void SubscribeToBrainEvents()
        {
            Brain.OnTurnEnded += HandleTurnEnded;
            Brain.OnAllyDamaged += HandleAllyDamaged;
            Brain.OnEnemyDamaged += HandleEnemyDamaged;
            Brain.OnUnitDefeated += HandleUnitDefeated;
            Brain.OnUnitMoved += HandleUnitMoved;
            Brain.OnBattleCompleted += HandleExitBattle;

            Brain.Subscribe<UnitSpawnedEvent>(HandleUnitSpawnedEvent, EventPriority.Normal);
            Brain.Subscribe<UnitDefeatedEvent>(HandleUnitDefeatedEvent, EventPriority.Normal);
        }

        public void DisconnectFromBrainEvents()
        {
            if (Brain == null || !_isConnectedToBrain)
            {
                return;
            }

            Brain.OnTurnEnded -= HandleTurnEnded;
            Brain.OnAllyDamaged -= HandleAllyDamaged;
            Brain.OnEnemyDamaged -= HandleEnemyDamaged;
            Brain.OnUnitDefeated -= HandleUnitDefeated;
            Brain.OnUnitMoved -= HandleUnitMoved;
            Brain.OnBattleCompleted -= HandleExitBattle;

            Brain.Unsubscribe<UnitSpawnedEvent>(HandleUnitSpawnedEvent);
            Brain.Unsubscribe<UnitDefeatedEvent>(HandleUnitDefeatedEvent);

            UnsubscribeFromMapChanges();
            _isConnectedToBrain = false;
        }

        private void UnsubscribeFromMapChanges()
        {
            try
            {
                if (MapGrid != null)
                {
                    MapGrid.OnStateVersionChanged -= HandleMapStateChanged;
                }
            }
            catch { }
        }

        public OperationResult ConnectBattleConditionsToContext()
        {
            try
            {
                foreach (var condition in _battleConditions)
                {
                    condition.battleContext = Context;
                    ResolveConditionReferences(condition);
                }
                return OperationResult.SuccessResult();
            }
            catch (System.Exception ex)
            {
                return OperationResult.Failure($"Failed to connect conditions: {ex.Message}");
            }
        }

        private OperationResult ResolveConditionReferences(BattleCondition condition)
        {
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
                return OperationResult.Failure(
                    $"Failed to resolve references for {condition?.Name}: {ex.Message}"
                );
            }
            return OperationResult.SuccessResult();
        }

        public OperationResult AddConditionAtRuntime(BattleCondition condition)
        {
            if (condition == null)
            {
                return OperationResult.Failure("Condition is null");
            }

            try
            {
                var list = new List<BattleCondition>(
                    _battleConditions ?? System.Array.Empty<BattleCondition>()
                );
                list.Add(condition);
                _battleConditions = list.ToArray();

                condition.battleContext = Context;
                ResolveConditionReferences(condition);
                condition?.InvalidateCache();
            }
            catch (System.Exception ex)
            {
                return OperationResult.Failure($"AddConditionAtRuntime failed: {ex.Message}");
            }
            return OperationResult.SuccessResult();
        }

        private OperationResult InitializeContextWithBrain()
        {
            if (
                !ValidationHelper.ValidateNotNull(
                    "BattleGameObject.ConnectionHelpers",
                    (Context, "Context"),
                    (Brain, "Brain")
                )
            )
            {
                return OperationResult.Failure(
                    "Cannot initialize context: Context or Brain is null"
                );
            }

            try
            {
                Context.Initialize(Brain, MapGrid);
                SubscribeToMapChanges();
                // Notify any subscribers that the battle map is ready
                Brain?.PublishBattleMapReady(MapGrid);
                GetComponent<TileHighlighter>().Initialize(Brain);
            }
            catch (System.Exception ex)
            {
                return OperationResult.Failure($"Failed to initialize context: {ex.Message}");
            }
            return OperationResult.SuccessResult();
        }

        private void SubscribeToMapChanges()
        {
            try
            {
                MapGrid.OnStateVersionChanged += HandleMapStateChanged;
            }
            catch { }
        }

        #endregion
    }
}
