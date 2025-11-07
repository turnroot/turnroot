using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes
{
    /// <summary>
    /// A visual node graph that defines skill behavior as a sequence of connected nodes.
    /// Use SkillGraphExecutor to run this graph at runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSkillGraph", menuName = "Turnroot/Skills/Skill Graph")]
    public class SkillGraph : NodeGraph
    {
        private SkillGraphExecutor activeExecutor;

        /// <summary>
        /// Execute this graph with the given context.
        /// Creates an executor and runs all connected nodes starting from entry points.
        /// </summary>
        public void Execute(SkillExecutionContext context)
        {
            activeExecutor = new SkillGraphExecutor(this);
            activeExecutor.Execute(context);
        }

        /// <summary>
        /// Proceed to the next node(s) in the execution flow.
        /// Call this from UnityEvents (e.g., animation events) to advance execution.
        /// </summary>
        public void Proceed()
        {
            if (activeExecutor != null)
            {
                activeExecutor.Proceed();
            }
            else
            {
                Debug.LogWarning("Cannot proceed: No active executor. Creating a new executor.");
                activeExecutor = new SkillGraphExecutor(this);
                activeExecutor.Execute(new SkillExecutionContext());
                activeExecutor.Proceed();
            }
        }
    }
}
