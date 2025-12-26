using System;
using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.Events;
using XNode;

namespace Turnroot.Skills.Nodes
{
    /// <summary>
    /// Validation result for skill node execution.
    /// </summary>
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

        /// <summary>
        /// Retrieves the current execution context from the given SkillGraph instance.
        /// </summary>
        public BattleContext GetContextFromGraph(SkillGraph skillGraph)
        {
            // Use reflection to access the private activeExecutor field
            var executorField = typeof(SkillGraph).GetField(
                "activeExecutor",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );

            if (executorField != null)
            {
                var executor = executorField.GetValue(skillGraph) as SkillGraphExecutor;
                if (executor != null)
                {
                    return executor.GetContext(); // Assuming this method returns BattleContext now
                }
            }

            return null;
        }

        /// <summary>
        /// Template method for node execution. Performs validation then calls ExecuteImpl().
        /// Override this only if you need custom pre/post execution behavior.
        /// Most nodes should override ExecuteImpl() instead.
        /// </summary>
        public virtual void Execute(BattleContext context)
        {
            // Base validation - context must exist
            if (!ValidateContext(context))
            {
                return;
            }

            // Node-specific validation via template method
            var validationResult = ValidateRequirements(context);
            if (!validationResult.IsValid)
            {
                Debug.LogWarning(
                    $"{GetType().Name}: Validation failed - {validationResult.ErrorMessage}"
                );
                return;
            }

            // Execute the node's actual logic
            ExecuteImpl(context);

            // Fire execution event
            OnNodeExecute?.Invoke();
        }

        /// <summary>
        /// Override this method to add node-specific validation requirements.
        /// Called before ExecuteImpl(). Return Success() to proceed, or Failure() to abort.
        /// Common validations (context null check) are already handled in Execute().
        /// </summary>
        /// <param name="context">The battle context (guaranteed non-null when called).</param>
        /// <returns>Validation result indicating whether execution should proceed.</returns>
        protected virtual SkillNodeValidationResult ValidateRequirements(BattleContext context) =>
            SkillNodeValidationResult.Success();

        /// <summary>
        /// Override this method to implement the node's execution logic.
        /// Called after ValidateRequirements() passes. Context is guaranteed non-null.
        /// </summary>
        /// <param name="context">The validated battle context.</param>
        protected virtual void ExecuteImpl(BattleContext context)
        {
            // Default implementation does nothing.
            // Subclasses override this to provide actual functionality.
        }

        public override object GetValue(NodePort port) => null;

        #region Validation Result Helpers

        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        protected static SkillNodeValidationResult ValidationSuccess() =>
            SkillNodeValidationResult.Success();

        /// <summary>
        /// Creates a failed validation result with an error message.
        /// </summary>
        protected static SkillNodeValidationResult ValidationFailure(string error) =>
            SkillNodeValidationResult.Failure(error);

        /// <summary>
        /// Returns a validation result requiring a unit in the context.
        /// </summary>
        protected SkillNodeValidationResult RequireUnit(BattleContext context)
        {
            return context.Unit.UnitInstance == null
                ? ValidationFailure("UnitInstance is required but was null")
                : ValidationSuccess();
        }

        /// <summary>
        /// Returns a validation result requiring at least one target in the context.
        /// </summary>
        protected SkillNodeValidationResult RequireTargets(BattleContext context)
        {
            return context.Participants.Targets == null || context.Participants.Targets.Count == 0
                ? ValidationFailure("At least one target is required")
                : ValidationSuccess();
        }

        /// <summary>
        /// Returns a validation result requiring both a unit and at least one target.
        /// </summary>
        protected SkillNodeValidationResult RequireUnitAndTargets(BattleContext context)
        {
            var unitResult = RequireUnit(context);
            return !unitResult.IsValid ? unitResult : RequireTargets(context);
        }

        /// <summary>
        /// Returns a validation result requiring at least one ally in the context.
        /// </summary>
        protected SkillNodeValidationResult RequireAllies(BattleContext context)
        {
            return context.Participants.Allies == null || context.Participants.Allies.Count == 0
                ? ValidationFailure("At least one ally is required")
                : ValidationSuccess();
        }

        #endregion

        #region Context Validation Helpers

        /// <summary>
        /// Validates that the context is not null.
        /// </summary>
        /// <param name="context">The battle context to validate.</param>
        /// <param name="nodeName">Name of the node for logging purposes.</param>
        /// <returns>True if valid, false if null.</returns>
        protected bool ValidateContext(BattleContext context, string nodeName = null)
        {
            nodeName ??= GetType().Name;
            if (context == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"{nodeName}: No context provided");
#endif
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validates that the context and UnitInstance are not null.
        /// </summary>
        /// <param name="context">The battle context to validate.</param>
        /// <param name="nodeName">Name of the node for logging purposes.</param>
        /// <returns>True if valid, false if null.</returns>
        protected bool ValidateContextAndUnit(BattleContext context, string nodeName = null)
        {
            nodeName ??= GetType().Name;
            return ValidationHelper.ValidateNotNull(
                context?.Unit.UnitInstance,
                "UnitInstance",
                nodeName
            );
        }

        /// <summary>
        /// Validates that the context has at least one target.
        /// </summary>
        /// <param name="context">The battle context to validate.</param>
        /// <param name="nodeName">Name of the node for logging purposes.</param>
        /// <returns>True if targets exist, false otherwise.</returns>
        protected bool ValidateHasTargets(BattleContext context, string nodeName = null)
        {
            nodeName ??= GetType().Name;
            if (context?.Participants?.Targets == null || context.Participants.Targets.Count == 0)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"{nodeName}: No targets in context");
#endif
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validates both unit and targets exist in the context.
        /// </summary>
        protected bool ValidateContextUnitAndTargets(BattleContext context, string nodeName = null)
        {
            return ValidateContextAndUnit(context, nodeName)
                && ValidateHasTargets(context, nodeName);
        }

        #endregion

        #region Target Iteration Helpers

        /// <summary>
        /// Executes an action on targets based on whether to affect all or just the first.
        /// </summary>
        /// <param name="context">The battle context with targets.</param>
        /// <param name="affectAll">If true, affects all targets; if false, only the first.</param>
        /// <param name="action">The action to perform on each target.</param>
        /// <param name="nodeName">Name of the node for logging purposes.</param>
        /// <returns>The number of targets affected.</returns>
        protected int ExecuteOnTargets(
            BattleContext context,
            bool affectAll,
            Action<CharacterInstance> action,
            string nodeName = null
        )
        {
            nodeName ??= GetType().Name;

            if (!ValidateHasTargets(context, nodeName))
            {
                return 0;
            }

            if (affectAll)
            {
                int affectedCount = 0;
                foreach (var target in context.Participants.Targets)
                {
                    if (target != null)
                    {
                        action(target);
                        affectedCount++;
                    }
                }
                return affectedCount;
            }
            else
            {
                var target = context.Participants.Targets[0];
                if (target != null)
                {
                    action(target);
                    return 1;
                }
#if UNITY_EDITOR
                Debug.LogWarning($"{nodeName}: First target is null");
#endif
                return 0;
            }
        }

        /// <summary>
        /// Executes an action on all targets in the context.
        /// </summary>
        protected int ExecuteOnAllTargets(
            BattleContext context,
            Action<CharacterInstance> action,
            string nodeName = null
        ) => ExecuteOnTargets(context, true, action, nodeName);

        /// <summary>
        /// Executes an action on the first target in the context.
        /// </summary>
        protected bool ExecuteOnFirstTarget(
            BattleContext context,
            Action<CharacterInstance> action,
            string nodeName = null
        ) => ExecuteOnTargets(context, false, action, nodeName) == 1;

        #endregion

        #region Input Helper Methods

        /// <summary>
        /// Gets a float value from an input port, or returns the test value if not connected.
        /// </summary>
        protected float GetInputFloat(string portName, float testValue)
        {
            var port = GetInputPort(portName);
            if (port != null && port.IsConnected)
            {
                var inputValue = port.GetInputValue();
                if (inputValue is FloatValue floatValue)
                {
                    return floatValue.value;
                }
            }
            return testValue;
        }

        /// <summary>
        /// Gets a bool value from an input port, or returns the test value if not connected.
        /// </summary>
        protected bool GetInputBool(string portName, bool testValue)
        {
            var port = GetInputPort(portName);
            if (port != null && port.IsConnected)
            {
                var inputValue = port.GetInputValue();
                if (inputValue is BoolValue boolValue)
                {
                    return boolValue.value;
                }
            }
            return testValue;
        }

        /// <summary>
        /// Applies a stat change to a character. Handles both bounded and unbounded stats.
        /// </summary>
        /// <returns>True if the stat change was successful, false otherwise.</returns>
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
#if UNITY_EDITOR
                Debug.LogWarning($"{nodeName}: Character is null");
#endif
                return false;
            }

            Characters.Stats.BaseCharacterStat stat = null;

            if (isBoundedStat)
            {
                if (
                    Enum.TryParse<Characters.Stats.BoundedStatType>(
                        statName,
                        out var boundedType
                    )
                )
                {
                    stat = character.GetBoundedStat(boundedType);
                }
                else
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"{nodeName}: Invalid bounded stat type: {statName}");
#endif
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
#if UNITY_EDITOR
                    Debug.LogWarning($"{nodeName}: Invalid unbounded stat type: {statName}");
#endif
                    return false;
                }
            }

            if (stat != null)
            {
                float oldValue = stat.Current;
                stat.SetCurrent(stat.Current + changeAmount);
                Debug.Log(
                    $"{nodeName}: Changed {statName} by {changeAmount} (from {oldValue} to {stat.Current})"
                );
                return true;
            }
            else
            {
                string statType = isBoundedStat ? "bounded" : "unbounded";
#if UNITY_EDITOR
                Debug.LogWarning($"{nodeName}: {statType} stat {statName} not found on character");
#endif
                return false;
            }
        }

        #endregion

        #region Combat Helper Methods

        /// <summary>
        /// Deals damage to a target using the command pattern.
        /// This is the primary method for dealing damage - all damage goes through commands.
        /// </summary>
        /// <param name="context">The battle context (required).</param>
        /// <param name="target">The target to damage.</param>
        /// <param name="damage">Amount of damage to deal.</param>
        /// <returns>True if damage was successfully applied.</returns>
        protected bool DealDamage(BattleContext context, CharacterInstance target, int damage)
        {
            if (target == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"{GetType().Name}: Target is null");
#endif
                return false;
            }

            RequireContext(context);
            return context.DealDamage(context.Unit.UnitInstance, target, damage);
        }

        /// <summary>
        /// Deals damage to a target using the command pattern (float overload for compatibility).
        /// </summary>
        protected bool DealDamage(BattleContext context, CharacterInstance target, float damage) =>
            DealDamage(context, target, (int)damage);

        /// <summary>
        /// Kills a character using the command pattern (deals lethal damage).
        /// </summary>
        /// <param name="context">The battle context (required).</param>
        /// <param name="target">The character to kill.</param>
        /// <returns>True if the kill command executed successfully.</returns>
        protected bool KillCharacter(BattleContext context, CharacterInstance target)
        {
            if (target == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"{GetType().Name}: Target is null");
#endif
                return false;
            }

            RequireContext(context);

            var healthStat = target.GetBoundedStat(Characters.Stats.BoundedStatType.Health);
            int killDamage = healthStat != null ? (int)healthStat.Current + 1 : 9999;

            return context.DealDamage(context.Unit.UnitInstance, target, killDamage);
        }

        /// <summary>
        /// Ensures context and brain are available. Throws if not.
        /// Derived classes can use this to validate context before performing operations.
        /// </summary>
        protected void RequireContext(BattleContext context)
        {
            if (context?.Brain == null)
            {
                throw new System.InvalidOperationException(
                    $"{GetType().Name}: BattleContext with Brain is required for combat operations."
                );
            }
            if (context.Unit.UnitInstance == null)
            {
                throw new System.InvalidOperationException(
                    $"{GetType().Name}: Battlecontext.Unit.UnitInstance must be set for combat operations."
                );
            }
        }

        #endregion

        /// <summary>
        /// Validate connections to ensure type safety.
        /// Only allow connections between ports of the same type.
        /// </summary>
        public override void OnCreateConnection(NodePort from, NodePort to)
        {
            // Validate type compatibility
            if (from.ValueType != to.ValueType)
            {
                Debug.LogWarning(
                    $"Cannot connect {from.ValueType.Name} ({from.direction}) to {to.ValueType.Name} ({to.direction}). Types must match."
                );

                // Disconnect the invalid connection that was just created
                from.Disconnect(to);
                return;
            }

            // Check if the target port (input) already has a connection
            // Input ports should only have one connection
            if (to.direction == NodePort.IO.Input && to.ConnectionCount > 0)
            {
                // Check if it's already connected to this exact port
                if (!to.IsConnectedTo(from))
                {
                    // Clear existing connection before making new one
                    to.ClearConnections();
#if UNITY_EDITOR
                    Debug.Log($"Replacing existing connection to input port {to.fieldName}");
#endif
                }
            }

            base.OnCreateConnection(from, to);
        }
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
