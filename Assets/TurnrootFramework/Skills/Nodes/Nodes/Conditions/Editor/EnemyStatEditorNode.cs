#if UNITY_EDITOR
using Turnroot.Skills.Nodes.Conditions;

/// <summary>
/// Custom editor for EnemyStat nodes.
/// Gets stats from the first enemy target (Targets[0]).
/// </summary>
namespace Turnroot.Skills.Nodes
{
    [CustomNodeEditor(typeof(EnemyStatNode))]
    public class EnemyStatEditorNode : StatNodeEditorBase
    {
        // All functionality is inherited from StatNodeEditorBase
    }
}
#endif
