#if UNITY_EDITOR
using Turnroot.Skills.Nodes.Conditions;

/// <summary>
/// Custom editor for UnitStat nodes.
/// Gets stats from the unit instance (the caster).
/// </summary>
///
namespace Turnroot.Skills.Nodes
{
    [CustomNodeEditor(typeof(UnitStatNode))]
    public class UnitStatEditorNode : StatNodeEditorBase
    {
        // All functionality is inherited from StatNodeEditorBase
    }
}
#endif
