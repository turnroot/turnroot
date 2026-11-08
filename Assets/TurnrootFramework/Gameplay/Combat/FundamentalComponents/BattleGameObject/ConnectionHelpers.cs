using System.Collections.Generic;
using Turnroot.Gameplay.Brain.Components.Battle;
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

        /// <summary>
        /// Connects battle logic to Brain and optionally initializes the map presentation path.
        /// </summary>
        /// <param name="initializePresentation">
        /// Set to false for logic-only tests that do not provide tile-highlighting assets.
        /// </param>
        public OperationResult ConnectToBrainEvents(bool initializePresentation = true)
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

            var initializationResult = InitializeContextWithBrain(initializePresentation);
            if (!initializationResult.Success)
            {
                return initializationResult;
            }

            SubscribeToBrainEvents();

            _isConnectedToBrain = true;
            return OperationResult.Successful();
        }

        private void SubscribeToBrainEvents()
        {
            Brain.OnBattleRoundEnded += HandleBattleRoundEnded;
            Brain.OnAllyDamaged += HandleAllyDamaged;
            Brain.OnEnemyDamaged += HandleEnemyDamaged;
            Brain.OnUnitDefeated += HandleUnitDefeated;
            Brain.OnUnitMoved += HandleUnitMoved;
            Brain.OnUnitSpawned += HandleUnitSpawned;
            Brain.OnBattleCompleted += HandleExitBattle;
            Brain.OnBattleInputEnabled += HandleBattleInputEnabled;
            Brain.OnBattleInputDisabled += HandleBattleInputDisabled;

        }

        public void DisconnectFromBrainEvents()
        {
            if (Brain == null || !_isConnectedToBrain)
            {
                return;
            }

            Brain.OnBattleRoundEnded -= HandleBattleRoundEnded;
            Brain.OnAllyDamaged -= HandleAllyDamaged;
            Brain.OnEnemyDamaged -= HandleEnemyDamaged;
            Brain.OnUnitDefeated -= HandleUnitDefeated;
            Brain.OnUnitMoved -= HandleUnitMoved;
            Brain.OnUnitSpawned -= HandleUnitSpawned;
            Brain.OnBattleCompleted -= HandleExitBattle;
            Brain.OnBattleInputEnabled -= HandleBattleInputEnabled;
            Brain.OnBattleInputDisabled -= HandleBattleInputDisabled;

            UnsubscribeFromMapChanges();
            _isConnectedToBrain = false;
        }

        private void HandleBattleInputEnabled()
        {
            if (Brain?.battleBrain != null)
            {
                Brain.battleBrain.IsInputEnabled = true;
            }
        }

        private void HandleBattleInputDisabled()
        {
            if (Brain?.battleBrain != null)
            {
                Brain.battleBrain.IsInputEnabled = false;
            }
        }

        private void UnsubscribeFromMapChanges()
        {
            if (MapGrid != null)
            {
                MapGrid.OnStateVersionChanged -= HandleMapStateChanged;
            }
        }

        public OperationResult ConnectBattleConditionsToContext()
        {
            try
            {
                foreach (var condition in BattleConditions)
                {
                    if (condition == null)
                    {
                        continue;
                    }

                    condition.battleContext = Context;
                    var result = ResolveConditionReferences(condition);
                    if (!result.Success)
                    {
                        return result;
                    }
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
                var result = condition.ResolveRequiredConditions(BattleConditions);
                if (!result.Success)
                {
                    return result;
                }

                if (condition is ConditionalGroupBattleCondition group)
                {
                    return group.ResolveChildConditions(BattleConditions);
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
                    BattleConditions ?? System.Array.Empty<BattleCondition>()
                )
                {
                    condition,
                };
                BattleConditions = list.ToArray();

                condition.battleContext = Context;
                var result = ResolveConditionReferences(condition);
                if (!result.Success)
                {
                    return result;
                }
                condition?.InvalidateCache();
            }
            catch (System.Exception ex)
            {
                return OperationResult.Failure($"AddConditionAtRuntime failed: {ex.Message}");
            }
            return OperationResult.Successful();
        }

        private OperationResult InitializeContextWithBrain(bool initializePresentation)
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
                // Use PreparationObject.MapGrid if available for consistency
                var mapGridToUse = Brain?.battleBrain.PreparationObject?.MapGrid ?? MapGrid;
                Context.Initialize(Brain, mapGridToUse);
                SubscribeToMapChanges();
                if (initializePresentation)
                {
                    Brain.PublishBattleMapReady(mapGridToUse);
                    TileHighlighter.Initialize(Brain, mapGridToUse);
                }

                if (TryGetComponent<BattlePrecomputeLoader>(out var loader))
                {
                    var initRes = loader.Initialize(Brain, Context);
                    if (!initRes.Success)
                    {
                        return OperationResult.Failure(
                            $"BattlePrecomputeLoader initialization failed: {initRes.ErrorMessage}"
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
