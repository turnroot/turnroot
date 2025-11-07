using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Events/Steal")]
[NodeLabel("Steal an object from the enemy")]
public class Steal : SkillNode
{
    [Input]
    public ExecutionFlow In;
}
