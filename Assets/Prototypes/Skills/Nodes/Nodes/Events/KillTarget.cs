using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Events/Kill Target")]
public class KillTarget : SkillNode
{
    [Input]
    public ExecutionFlow In;
}
