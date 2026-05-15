using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Utility
{
    /// <summary>
    /// A comment/annotation node with no ports and no runtime behavior.
    /// Use it to leave notes inside a skill graph.
    /// </summary>
    [CreateNodeMenu("Utility/Sticky Note")]
    public class StickyNoteNode : SkillNode
    {
        [TextArea(3, 20)]
        public string note = "";

        // No ports — GetValue is never called, but must be implemented.
        public override object GetValue(NodePort port) => null;

        // Never executed by the graph executor.
        protected override void ExecuteImpl(BattleContext context) { }
    }
}
