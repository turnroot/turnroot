#if UNITY_EDITOR
using UnityEditor;
using XNodeEditor;

/// <summary>
/// Custom editor for EnemyStat nodes.
/// Gets stats from the first enemy target (Targets[0]).
/// </summary>
[CustomNodeEditor(typeof(EnemyStatNode))]
public class EnemyStatEditorNode : StatNodeEditorBase
{
    // All functionality is inherited from StatNodeEditorBase
}
#endif
