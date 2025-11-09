using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
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
            if (OnNodeExecute == null)
            {
                OnNodeExecute = new UnityEvent();
            }
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
        )
        { }

        public override object GetValue(NodePort port)
        {
            return null;
        }

        #region Helper Methods

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
