using System;
using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine.Events;
using XNode;

namespace Turnroot.Skills.Nodes
{
    public struct SkillNodeValidationResult
    {
        public bool IsValid;
        public string ErrorMessage;

        public static SkillNodeValidationResult Success() =>
            new() { IsValid = true, ErrorMessage = null };

        public static SkillNodeValidationResult Failure(string error) =>
            new() { IsValid = false, ErrorMessage = error };
    }

    /// <summary>
    /// Base class for all skill nodes. Provides execution flow and data evaluation.
    /// Uses template method pattern: Execute() validates, then calls ExecuteImpl().
    /// Subclasses should override ExecuteImpl() and optionally ValidateRequirements().
    /// </summary>
    public abstract class SkillNode : Node
    {
        public UnityEvent OnNodeExecute;

        protected override void Init()
        {
            base.Init();
            OnNodeExecute ??= new UnityEvent();
        }

        public BattleContext GetContextFromGraph(SkillGraph skillGraph)
        {
            var executorField = typeof(SkillGraph).GetField(
                "activeExecutor",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );

            var executor = executorField?.GetValue(skillGraph) as SkillGraphExecutor;
            return executor?.GetContext();
        }

        #region Execution Template
        public virtual void Execute(BattleContext context)
        {
            if (!ValidateContext(context))
            {
                return;
            }

            var validationResult = ValidateRequirements(context);
            if (!validationResult.IsValid)
            {
                LogWarning($"Validation failed - {validationResult.ErrorMessage}");
                return;
            }

            ExecuteImpl(context);
            OnNodeExecute?.Invoke();
        }

        protected virtual SkillNodeValidationResult ValidateRequirements(BattleContext context) =>
            SkillNodeValidationResult.Success();

        protected virtual void ExecuteImpl(BattleContext context) { }

        public override object GetValue(NodePort port) => null;
        #endregion

        #region Validation Helpers
        protected static SkillNodeValidationResult ValidationSuccess() =>
            SkillNodeValidationResult.Success();

        protected static SkillNodeValidationResult ValidationFailure(string error) =>
            SkillNodeValidationResult.Failure(error);

        protected SkillNodeValidationResult RequireUnit(BattleContext context) =>
            context.Unit.UnitInstance == null
                ? ValidationFailure("UnitInstance is required but was null")
                : ValidationSuccess();

        protected SkillNodeValidationResult RequireTargets(BattleContext context) =>
            context.Participants.Targets == null || context.Participants.Targets.Count == 0
                ? ValidationFailure("At least one target is required")
                : ValidationSuccess();

        protected SkillNodeValidationResult RequireUnitAndTargets(BattleContext context)
        {
            var unitResult = RequireUnit(context);
            return !unitResult.IsValid ? unitResult : RequireTargets(context);
        }

        protected SkillNodeValidationResult RequireAllies(BattleContext context) =>
            context.Participants.Allies == null || context.Participants.Allies.Count == 0
                ? ValidationFailure("At least one ally is required")
                : ValidationSuccess();

        protected bool ValidateContext(BattleContext context, string nodeName = null)
        {
            if (context != null)
            {
                return true;
            }

            LogWarning("No context provided", nodeName);
            return false;
        }

        protected bool ValidateContextAndUnit(BattleContext context, string nodeName = null) =>
            ValidationHelper.ValidateNotNull(
                context?.Unit.UnitInstance,
                "UnitInstance",
                nodeName ?? GetType().Name
            );

        protected bool ValidateHasTargets(BattleContext context, string nodeName = null)
        {
            if (context?.Participants?.Targets != null && context.Participants.Targets.Count > 0)
            {
                return true;
            }

            LogWarning("No targets in context", nodeName);
            return false;
        }

        protected bool ValidateContextUnitAndTargets(
            BattleContext context,
            string nodeName = null
        ) => ValidateContextAndUnit(context, nodeName) && ValidateHasTargets(context, nodeName);
        #endregion

        #region Target Iteration
        protected int ExecuteOnTargets(
            BattleContext context,
            bool affectAll,
            Action<CharacterInstance> action,
            string nodeName = null
        )
        {
            if (!ValidateHasTargets(context, nodeName))
            {
                return 0;
            }

            if (affectAll)
            {
                int count = 0;
                foreach (var target in context.Participants.Targets)
                {
                    if (target != null)
                    {
                        action(target);
                        count++;
                    }
                }
                return count;
            }

            var firstTarget = context.Participants.Targets[0];
            if (firstTarget != null)
            {
                action(firstTarget);
                return 1;
            }

            LogWarning("First target is null", nodeName);
            return 0;
        }

        protected int ExecuteOnAllTargets(
            BattleContext context,
            Action<CharacterInstance> action,
            string nodeName = null
        ) => ExecuteOnTargets(context, true, action, nodeName);

        protected bool ExecuteOnFirstTarget(
            BattleContext context,
            Action<CharacterInstance> action,
            string nodeName = null
        ) => ExecuteOnTargets(context, false, action, nodeName) == 1;
        #endregion

        #region Input Helpers
        protected float GetInputFloat(string portName, float testValue)
        {
            var port = GetInputPort(portName);
            if (port?.IsConnected == true)
            {
                if (port.GetInputValue() is FloatValue floatValue)
                {
                    return floatValue.value;
                }
            }
            return testValue;
        }

        protected bool GetInputBool(string portName, bool testValue)
        {
            var port = GetInputPort(portName);
            if (port?.IsConnected == true)
            {
                if (port.GetInputValue() is BoolValue boolValue)
                {
                    return boolValue.value;
                }
            }
            return testValue;
        }
        #endregion

        #region Stat Management
        protected bool ApplyStatChange(
            CharacterInstance character,
            string statName,
            bool isBoundedStat,
            float changeAmount,
            string nodeName = "Node"
        )
        {
            if (character == null)
            {
                LogWarning("Character is null", nodeName);
                return false;
            }

            Characters.Stats.BaseCharacterStat stat = null;

            if (isBoundedStat)
            {
                if (Enum.TryParse<Characters.Stats.BoundedStatType>(statName, out var boundedType))
                {
                    stat = character.GetBoundedStat(boundedType);
                }
                else
                {
                    LogWarning($"Invalid bounded stat type: {statName}", nodeName);
                    return false;
                }
            }
            else
            {
                if (
                    Enum.TryParse<Characters.Stats.UnboundedStatType>(
                        statName,
                        out var unboundedType
                    )
                )
                {
                    stat = character.GetUnboundedStat(unboundedType);
                }
                else
                {
                    LogWarning($"Invalid unbounded stat type: {statName}", nodeName);
                    return false;
                }
            }

            if (stat == null)
            {
                LogWarning(
                    $"{(isBoundedStat ? "Bounded" : "Unbounded")} stat {statName} not found",
                    nodeName
                );
                return false;
            }

            float oldValue = stat.Current;
            stat.SetCurrent(stat.Current + changeAmount);
            TurnrootLogger.Log(
                $"{nodeName}: Changed {statName} by {changeAmount} (from {oldValue} to {stat.Current})"
            );
            return true;
        }
        #endregion

        #region Combat Operations
        protected bool DealDamage(BattleContext context, CharacterInstance target, int damage)
        {
            if (target == null)
            {
                LogWarning("Target is null");
                return false;
            }

            RequireContext(context);
            return context.DealDamage(context.Unit.UnitInstance, target, damage);
        }

        protected bool DealDamage(BattleContext context, CharacterInstance target, float damage) =>
            DealDamage(context, target, (int)damage);

        protected bool KillCharacter(BattleContext context, CharacterInstance target)
        {
            if (target == null)
            {
                LogWarning("Target is null");
                return false;
            }

            RequireContext(context);

            var healthStat = target.GetBoundedStat(Characters.Stats.BoundedStatType.Health);
            int killDamage = healthStat != null ? (int)healthStat.Current + 1 : 9999;

            return context.DealDamage(context.Unit.UnitInstance, target, killDamage);
        }

        protected void RequireContext(BattleContext context)
        {
            if (context?.Brain == null)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name}: BattleContext with Brain is required for combat operations."
                );
            }
            if (context.Unit.UnitInstance == null)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name}: BattleContext.Unit.UnitInstance must be set for combat operations."
                );
            }
        }
        #endregion

        #region Connection Validation
        public override void OnCreateConnection(NodePort from, NodePort to)
        {
            if (from.ValueType != to.ValueType)
            {
                LogWarning(
                    $"Cannot connect {from.ValueType.Name} ({from.direction}) to {to.ValueType.Name} ({to.direction}). Types must match."
                );
                from.Disconnect(to);
                return;
            }

            if (
                to.direction == NodePort.IO.Input
                && to.ConnectionCount > 0
                && !to.IsConnectedTo(from)
            )
            {
                to.ClearConnections();
                TurnrootLogger.Log($"Replacing existing connection to input port {to.fieldName}");
            }

            base.OnCreateConnection(from, to);
        }
        #endregion

        #region Logging
        private void LogWarning(string message, string nodeName = null)
        {
            nodeName ??= GetType().Name;
            TurnrootLogger.Log($"{nodeName}: {message}", TurnrootLogger.LogLevel.Warning);
        }
        #endregion
    }

    [Serializable]
    public struct ExecutionFlow { }

    [Serializable]
    public struct BoolValue
    {
        public bool value;
    }

    [Serializable]
    public struct FloatValue
    {
        public float value;
    }

    [Serializable]
    public struct StringValue
    {
        public string value;
    }
}
