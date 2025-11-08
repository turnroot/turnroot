using System.Collections.Generic;
using Assets.Prototypes.Gameplay.Combat.FundamentalComponents.Battles;
using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes
{
    /// <summary>
    /// Executes a SkillGraph by running nodes sequentially starting from entry points.
    /// Handles the execution flow through the visual node graph, including waiting for async operations.
    /// </summary>
    public class SkillGraphExecutor
    {
        private SkillGraph graph;
        private BattleContext context;
        private HashSet<SkillNode> visitedNodes;
        private SkillNode currentNode;

        public SkillGraphExecutor(SkillGraph graph)
        {
            this.graph = graph;
        }

        /// <summary>
        /// Execute the entire skill graph with the given context.
        /// Starts from entry nodes (nodes with no incoming execution connections).
        /// </summary>
        public void Execute(BattleContext battleContext)
        {
            this.context = battleContext;
            this.visitedNodes = new HashSet<SkillNode>();
            this.currentNode = null;

            context.CurrentSkillGraph = graph;
            // Store executor in context so nodes can signal completion
            context.SetCustomData("_executor", this);

            // Find all entry point nodes (nodes with no input connections)
            var entryNodes = FindEntryNodes();

            if (entryNodes.Count == 0)
            {
                Debug.LogWarning(
                    $"SkillGraph has no entry nodes. Add a node with no incoming execution connection."
                );
                return;
            }

            // Execute from each entry point
            foreach (var entryNode in entryNodes)
            {
                ExecuteNode(entryNode);
            }
        }

        /// <summary>
        /// Proceed to the next node(s) from the current node.
        /// Call this from UnityEvents to advance execution after waiting for something (animation, etc).
        /// </summary>
        public void Proceed()
        {
            if (currentNode != null)
            {
                ContinueFromNode(currentNode);
                currentNode = null;
            }
            else
            {
                Debug.LogWarning("No current node to proceed from.");
            }
        }

        /// <summary>
        /// Execute a specific node and follow its execution chain.
        /// </summary>
        private void ExecuteNode(SkillNode node)
        {
            if (node == null)
                return;

            // Prevent infinite loops from circular connections
            if (visitedNodes.Contains(node))
            {
                Debug.LogWarning(
                    $"Circular execution detected at node {node.name}. Stopping execution."
                );
                return;
            }

            visitedNodes.Add(node);

            // Check if execution was interrupted
            if (context.IsInterrupted)
            {
                Debug.Log($"Execution interrupted at node {node.name}");
                return;
            }

            // Fire the node's event
            node.OnNodeExecute?.Invoke();

            // Execute this node
            try
            {
                node.Execute(context);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error executing node {node.name}: {e.Message}\n{e.StackTrace}");
                context.IsInterrupted = true;
                return;
            }

            // Store as current node - execution will wait here until Proceed() is called
            currentNode = node;
        }

        /// <summary>
        /// Continue execution from the given node to its connected next nodes.
        /// Called by nodes when they're ready to proceed (after animations, etc).
        /// </summary>
        public void ContinueFromNode(SkillNode node)
        {
            if (node == null)
                return;

            // Get execution output ports
            var outputs = GetExecutionOutputPorts(node);

            foreach (var port in outputs)
            {
                if (!port.IsConnected)
                    continue;

                var connections = port.GetConnections();
                foreach (var connection in connections)
                {
                    if (connection.node is SkillNode nextNode)
                    {
                        ExecuteNode(nextNode);
                    }
                }
            }
        }

        /// <summary>
        /// Get all execution output ports from a node.
        /// </summary>
        private List<NodePort> GetExecutionOutputPorts(SkillNode node)
        {
            var execPorts = new List<NodePort>();

            foreach (var port in node.Ports)
            {
                if (port.direction == NodePort.IO.Output && port.ValueType == typeof(ExecutionFlow))
                {
                    execPorts.Add(port);
                }
            }

            return execPorts;
        }

        /// <summary>
        /// Find all nodes that can be entry points (no incoming execution connections).
        /// </summary>
        private List<SkillNode> FindEntryNodes()
        {
            var entryNodes = new List<SkillNode>();

            foreach (var node in graph.nodes)
            {
                if (node is SkillNode skillNode)
                {
                    bool hasExecInput = false;

                    foreach (var port in skillNode.Ports)
                    {
                        if (
                            port.direction == NodePort.IO.Input
                            && port.ValueType == typeof(ExecutionFlow)
                            && port.IsConnected
                        )
                        {
                            hasExecInput = true;
                            break;
                        }
                    }

                    // Entry node if it has no incoming execution connection
                    if (!hasExecInput)
                    {
                        entryNodes.Add(skillNode);
                    }
                }
            }

            return entryNodes;
        }

        /// <summary>
        /// Get the current execution context (useful for debugging).
        /// </summary>
        public BattleContext GetContext()
        {
            return context;
        }
    }
}
