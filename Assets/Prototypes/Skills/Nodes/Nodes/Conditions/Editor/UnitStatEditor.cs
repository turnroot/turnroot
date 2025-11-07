#if UNITY_EDITOR
using UnityEditor;
using XNodeEditor;

/// <summary>
/// Custom editor for UnitStat nodes.
/// Gets stats from the unit instance (the caster).
/// </summary>
[CustomNodeEditor(typeof(UnitStat))]
public class UnitStatEditor : StatNodeEditorBase
{
    // All functionality is inherited from StatNodeEditorBase
}
#endif
