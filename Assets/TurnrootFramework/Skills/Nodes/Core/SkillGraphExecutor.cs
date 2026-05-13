using System.Collections.Generic;
using System.Linq;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
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
            context = battleContext;
            // Always start with a clean interruption flag so skills sharing a context
            // don't bleed state from one execution into the next.
            context.Flags.IsInterrupted = false;
            visitedNodes = new HashSet<SkillNode>();
            currentNode = null;

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

            return OperationResult.Successful();
        }

        /// <summary>
        /// Continue execution from the given node to its connected next nodes.
        /// Called by nodes when they're ready to proceed (after animations, etc).
        /// Stops immediately if <see cref="CombatFlags.IsInterrupted"/> is set (e.g. by
        /// <see cref="Flow.FlowIfNode"/> on a false condition).
        /// Recurses into each next node so the full chain executes without every node
        /// needing to call <see cref="Proceed"/> manually.
        /// </summary>
        public void ContinueFromNode(SkillNode node)
        {
            if (node == null || context.Flags.IsInterrupted)
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
                            continue;
                        }
                        ExecuteNode(nextNode);
                        // Recurse so the full downstream chain runs without each node
                        // needing to call Proceed() synchronously.
                        ContinueFromNode(nextNode);
                    }
                }
            }
        }

        /// <summary>
        /// Executes a subchain starting from <paramref name="startNode"/> with a fresh
        /// visited-nodes set, preventing re-entry into the nodes listed in
        /// <paramref name="ancestorsToSkip"/>. Resets <see cref="CombatFlags.IsInterrupted"/>
        /// for the subchain so that each iteration of a loop node (e.g. ForEachEnemyNode)
        /// starts clean. All state is restored on exit.
        /// </summary>
        public void ExecuteSubchain(SkillNode startNode, HashSet<SkillNode> ancestorsToSkip)
        {
            if (startNode == null)
            {
                return;
            }

            var savedVisited = visitedNodes;
            var savedCurrent = currentNode;
            bool savedInterrupted = context.Flags.IsInterrupted;

            // Seed the fresh visited set with ancestors so we can't loop back into them.
            visitedNodes = new HashSet<SkillNode>(ancestorsToSkip);
            context.Flags.IsInterrupted = false;

            try
            {
                ExecuteNode(startNode);
                ContinueFromNode(startNode);
            }
            finally
            {
                visitedNodes = savedVisited;
                currentNode = savedCurrent;
                context.Flags.IsInterrupted = savedInterrupted;
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

        /// <summary>
        /// Marks a node as already visited so the current execution pass skips it.
        /// Used by <see cref="Flow.ForEachEnemyNode"/> to prevent the outer
        /// <see cref="ContinueFromNode"/> from re-running subchain entry points that
        /// were already executed per-enemy inside the loop.
        /// </summary>
        public void MarkVisited(SkillNode node)
        {
            if (node != null)
            {
                visitedNodes?.Add(node);
            }
        }
    }
}
