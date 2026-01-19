#if UNITY_EDITOR
using Turnroot.Skills.Nodes.Events;
using Turnroot.Skills.Nodes.Events.Editor;

namespace Turnroot.Skills.Nodes
{
    /// <summary>
    /// Custom editor for AffectUnitStat nodes.
    /// Modifies stats on the executing unit.
    /// </summary>
    [CustomNodeEditor(typeof(AffectUnitStatNode))]
    public class AffectUnitStatEditorNode : AffectStatNodeEditorBase
    {
        // All functionality is inherited from AffectStatNodeEditorBase
    }
}
#endif
