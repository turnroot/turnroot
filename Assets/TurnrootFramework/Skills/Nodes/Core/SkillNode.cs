using System;
using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.Events;
using XNode;

namespace Turnroot.Skills.Nodes
{
    /// <summary>
    /// Base class for all skill nodes. Provides execution flow and data evaluation.
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
        public Turnroot.Gameplay.Combat.FundamentalComponents.Battles.BattleContext GetContextFromGraph(
            SkillGraph skillGraph
        )
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

        public virtual void Execute(
            Turnroot.Gameplay.Combat.FundamentalComponents.Battles.BattleContext context
        ) { }

        public override object GetValue(NodePort port) => null;

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
                Debug.LogWarning($"{nodeName}: No context provided");
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
                context?.UnitInstance,
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
            if (context?.Targets == null || context.Targets.Count == 0)
            {
                Debug.LogWarning($"{nodeName}: No targets in context");
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
                foreach (var target in context.Targets)
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
                var target = context.Targets[0];
                if (target != null)
                {
                    action(target);
                    return 1;
                }
                Debug.LogWarning($"{nodeName}: First target is null");
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
        )
        {
            return ExecuteOnTargets(context, true, action, nodeName);
        }

        /// <summary>
        /// Executes an action on the first target in the context.
        /// </summary>
        protected bool ExecuteOnFirstTarget(
            BattleContext context,
            Action<CharacterInstance> action,
            string nodeName = null
        )
        {
            return ExecuteOnTargets(context, false, action, nodeName) == 1;
        }

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
            Turnroot.Characters.CharacterInstance character,
            string statName,
            bool isBoundedStat,
            float changeAmount,
            string nodeName = "Node"
        )
        {
            if (character == null)
            {
                Debug.LogWarning($"{nodeName}: Character is null");
                return false;
            }

            Turnroot.Characters.Stats.BaseCharacterStat stat = null;

            if (isBoundedStat)
            {
                if (
                    System.Enum.TryParse<Turnroot.Characters.Stats.BoundedStatType>(
                        statName,
                        out var boundedType
                    )
                )
                {
                    stat = character.GetBoundedStat(boundedType);
                }
                else
                {
                    Debug.LogWarning($"{nodeName}: Invalid bounded stat type: {statName}");
                    return false;
                }
            }
            else
            {
                if (
                    System.Enum.TryParse<Turnroot.Characters.Stats.UnboundedStatType>(
                        statName,
                        out var unboundedType
                    )
                )
                {
                    stat = character.GetUnboundedStat(unboundedType);
                }
                else
                {
                    Debug.LogWarning($"{nodeName}: Invalid unbounded stat type: {statName}");
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
                Debug.LogWarning($"{nodeName}: {statType} stat {statName} not found on character");
                return false;
            }
        }

        #endregion

        #region Combat Helper Methods

        /// <summary>
        /// Deals damage to a character by reducing their health stat.
        /// </summary>
        /// <param name="target">The character to damage.</param>
        /// <param name="damage">The amount of damage to deal.</param>
        /// <param name="nodeName">Name of the node for logging purposes.</param>
        /// <returns>True if damage was successfully applied, false otherwise.</returns>
        protected bool DealDamage(
            Turnroot.Characters.CharacterInstance target,
            float damage,
            string nodeName = null
        )
        {
            nodeName ??= GetType().Name;

            if (target == null)
            {
                Debug.LogWarning($"{nodeName}: Target is null");
                return false;
            }

            var healthStat = target.GetBoundedStat(
                Turnroot.Characters.Stats.BoundedStatType.Health
            );
            if (healthStat != null)
            {
                float newHealth = healthStat.Current - damage;
                healthStat.SetCurrent(newHealth);
                Debug.Log($"{nodeName}: Dealt {damage} damage (new HP: {healthStat.Current})");
                return true;
            }
            else
            {
                Debug.LogWarning($"{nodeName}: Could not find health stat on target");
                return false;
            }
        }

        /// <summary>
        /// Kills a character by setting their health to 0.
        /// </summary>
        /// <param name="target">The character to kill.</param>
        /// <param name="nodeName">Name of the node for logging purposes.</param>
        /// <returns>True if the kill was successful, false otherwise.</returns>
        protected bool KillCharacter(
            Turnroot.Characters.CharacterInstance target,
            string nodeName = null
        )
        {
            nodeName ??= GetType().Name;

            if (target == null)
            {
                Debug.LogWarning($"{nodeName}: Target is null");
                return false;
            }

            var healthStat = target.GetBoundedStat(
                Turnroot.Characters.Stats.BoundedStatType.Health
            );
            if (healthStat != null)
            {
                healthStat.SetCurrent(0);
                Debug.Log($"{nodeName}: Killed target (health set to 0)");
                return true;
            }
            else
            {
                Debug.LogWarning($"{nodeName}: Could not find health stat on target");
                return false;
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
                    Debug.Log($"Replacing existing connection to input port {to.fieldName}");
                }
            }

            base.OnCreateConnection(from, to);
        }
    }

    [System.Serializable]
    public struct ExecutionFlow { }

    [System.Serializable]
    public struct BoolValue
    {
        public bool value;
    }

    [System.Serializable]
    public struct FloatValue
    {
        public float value;
    }

    [System.Serializable]
    public struct StringValue
    {
        public string value;
    }
}
