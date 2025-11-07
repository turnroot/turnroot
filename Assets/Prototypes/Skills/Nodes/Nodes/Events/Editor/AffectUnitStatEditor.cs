#if UNITY_EDITOR
using Assets.Prototypes.Skills.Nodes.Events;
using Assets.Prototypes.Skills.Nodes.Events.Editor;
using XNodeEditor;

/// <summary>
/// Custom editor for AffectUnitStat nodes.
/// Modifies stats on the executing unit.
/// </summary>
[CustomNodeEditor(typeof(AffectUnitStat))]
public class AffectUnitStatEditor : AffectStatNodeEditorBase
{
    // All functionality is inherited from AffectStatNodeEditorBase
}
#endif
