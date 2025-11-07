using UnityEngine;
using UnityEngine.Events;
using XNode;

namespace Assets.Prototypes.Skills.Nodes
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
        protected SkillExecutionContext GetContextFromGraph(SkillGraph skillGraph)
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
                    return executor.GetContext();
                }
            }

            return null;
        }

        public virtual void Execute(SkillExecutionContext context) { }

        public override object GetValue(NodePort port)
        {
            return null;
        }

        /// <summary>
        /// Validate connections to ensure type safety.
        /// Only allow connections between ports of the same type.
        /// </summary>
        public override void OnCreateConnection(NodePort from, NodePort to)
        {
            if (from.ValueType != to.ValueType)
            {
                Debug.LogWarning(
                    $"Cannot connect {from.ValueType.Name} ({from.direction}) to {to.ValueType.Name} ({to.direction}). Types must match."
                );
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
