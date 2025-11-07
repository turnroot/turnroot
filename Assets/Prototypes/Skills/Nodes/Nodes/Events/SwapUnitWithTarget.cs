using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Events/Swap Unit With Target")]
public class SwapUnitWithTarget : SkillNode
{
    [Input]
    public ExecutionFlow In;
}
