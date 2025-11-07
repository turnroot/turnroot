using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Events/Take Another Turn")]
public class TakeAnotherTurn : SkillNode
{
    [Input]
    public ExecutionFlow In;
}
