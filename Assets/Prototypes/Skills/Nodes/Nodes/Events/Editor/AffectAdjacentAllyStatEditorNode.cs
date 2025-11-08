#if UNITY_EDITOR
using Assets.Prototypes.Skills.Nodes.Events;
using Assets.Prototypes.Skills.Nodes.Events.Editor;
using XNodeEditor;

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
