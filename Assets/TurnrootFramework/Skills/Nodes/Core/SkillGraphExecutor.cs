using System.Collections.Generic;
using System.Linq;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes
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

            context.Skill.CurrentSkillGraph = graph;
            context.SetCustomData("_executor", this);

            // Find all entry point nodes (nodes with no input connections)
            var entryNodes = FindEntryNodes();

            foreach (var entryNode in entryNodes)
            {
                ExecuteNode(entryNode);
                ContinueFromNode(entryNode);
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
        }

        /// <summary>
        /// Execute a specific node and follow its execution chain.
        /// </summary>
        private OperationResult ExecuteNode(SkillNode node)
        {
            var validation = OperationResultGuards.RequireNotNull(node, nameof(node));
            if (!validation.Success)
            {
                return validation;
            }

            if (visitedNodes.Contains(node))
            {
                $"Circular execution detected at node {node.name}. Stopping execution.".LogWarning();
                return OperationResult.Failure("Circular execution detected");
            }

            visitedNodes.Add(node);
            currentNode = node;

            node.OnNodeExecute?.Invoke();

            var execResult = node.ExecuteWithResult(context);
            if (!execResult.Success)
            {
                $"Error executing node {node.name}: {execResult.ErrorMessage}".LogError();
                context.Flags.IsInterrupted = true;
                return execResult;
            }

            currentNode = node;
            return OperationResult.Successful();
        }

        /// <summary>
        /// Continue execution from the given node to its connected next nodes.
        /// Called by nodes when they're ready to proceed (after animations, etc).
        /// </summary>
        public void ContinueFromNode(SkillNode node)
        {
            if (node == null)
            {
                return;
            }

            // Get execution output ports
            var outputs = GetExecutionOutputPorts(node);

            foreach (var port in outputs)
            {
                if (!port.IsConnected)
                {
                    continue;
                }

                var connections = port.GetConnections();
                foreach (var connection in connections)
                {
                    if (connection.node is SkillNode nextNode)
                    {
                        // skip nodes we've already visited to avoid spinning on cycles
                        if (visitedNodes.Contains(nextNode))
                        {
                            $"SkillGraphExecutor: skipping already visited {nextNode.name}".LogWarning();
                            continue;
                        }
                        ExecuteNode(nextNode);
                    }
                }
            }
        }

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
                    // only consider nodes that actually participate in flow
                    bool hasAnyExecPort = skillNode.Ports.Any(p =>
                        p.ValueType == typeof(ExecutionFlow)
                    );
                    if (!hasAnyExecPort)
                    {
                        continue; // value-only nodes should not be entry points
                    }

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

        public BattleContext GetContext() => context;
    }
}
