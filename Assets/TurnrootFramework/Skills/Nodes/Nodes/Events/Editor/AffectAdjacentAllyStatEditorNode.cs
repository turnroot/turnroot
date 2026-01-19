#if UNITY_EDITOR
using Turnroot.Skills.Nodes.Events;
using Turnroot.Skills.Nodes.Events.Editor;

/// <summary>
/// Custom editor for AffectAdjacentAllyStat nodes.
/// Modifies stats on adjacent allied units.
/// </summary>
[CustomNodeEditor(typeof(AffectAdjacentAllyStatNode))]
public class AffectAdjacentAllyStatEditorNode : AffectStatNodeEditorBase
{
    // All functionality is inherited from AffectStatNodeEditorBase
}
#endif
