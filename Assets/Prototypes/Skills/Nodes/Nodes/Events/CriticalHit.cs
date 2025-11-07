using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Events/Critical Hit")]
public class CriticalHit : SkillNode
{
    [Input]
    public ExecutionFlow In;
}
