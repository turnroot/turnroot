using System.Collections.Generic;
using Turnroot.Gameplay.Brain.Components.Battle;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Gameplay.Combat.Precompute;
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
            var validation = OperationResultGuards.RequireNotNull(Brain, nameof(Brain));
            if (!validation.Success)
            {
                return validation;
            }

            if (_isConnectedToBrain)
            {
                return OperationResult.Successful();
            }

            InitializeContextWithBrain();
            SubscribeToBrainEvents();

            _isConnectedToBrain = true;
            return OperationResult.Successful();
        }

        private void SubscribeToBrainEvents()
        {
            Brain.OnTurnEnded += HandleTurnEnded;
            Brain.OnAllyDamaged += HandleAllyDamaged;
            Brain.OnEnemyDamaged += HandleEnemyDamaged;
            Brain.OnUnitDefeated += HandleUnitDefeated;
            Brain.OnUnitMoved += HandleUnitMoved;
            Brain.OnBattleCompleted += HandleExitBattle;
            Brain.OnBattleInputEnabled += () => Brain.battleBrain.IsInputEnabled = true;
            Brain.OnBattleInputDisabled += () => Brain.battleBrain.IsInputEnabled = false;

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
            Brain.OnBattleInputEnabled -= () => Brain.battleBrain.IsInputEnabled = true;
            Brain.OnBattleInputDisabled -= () => Brain.battleBrain.IsInputEnabled = false;

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
                return OperationResult.Successful();
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
            return OperationResult.Successful();
        }

        public OperationResult AddConditionAtRuntime(BattleCondition condition)
        {
            var validation = OperationResultGuards.RequireNotNull(condition, nameof(condition));
            if (!validation.Success)
            {
                return validation;
            }

            try
            {
                var list = new List<BattleCondition>(
                    _battleConditions ?? System.Array.Empty<BattleCondition>()
                )
                {
                    condition,
                };
                _battleConditions = list.ToArray();

                condition.battleContext = Context;
                ResolveConditionReferences(condition);
                condition?.InvalidateCache();
            }
            catch (System.Exception ex)
            {
                return OperationResult.Failure($"AddConditionAtRuntime failed: {ex.Message}");
            }
            return OperationResult.Successful();
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
                GetComponent<TileHighlighter>().Initialize(Brain, MapGrid);

                // Ensure BattlePrecomputeLoader is initialized with the Brain (robustness against subscription order)
                var loader = GetComponent<BattlePrecomputeLoader>();
                if (loader != null)
                {
                    var initRes = loader.Initialize(Brain, Context);
                    if (!initRes.Success)
                    {
                        TurnrootLogger.Log(
                            $"BattleGameObject: Failed to initialize BattlePrecomputeLoader: {initRes.ErrorMessage}",
                            TurnrootLogger.LogLevel.Warning
                        );
                    }
                }
            }
            catch (System.Exception ex)
            {
                return OperationResult.Failure($"Failed to initialize context: {ex.Message}");
            }
            return OperationResult.Successful();
        }

        private void SubscribeToMapChanges() =>
            MapGrid.OnStateVersionChanged += HandleMapStateChanged;

        #endregion
    }
}
