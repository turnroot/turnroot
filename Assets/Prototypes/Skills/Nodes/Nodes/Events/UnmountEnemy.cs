using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Events/Unmount Enemy")]
[NodeLabel("Force an enemy to dismount from riding/flying. They can remount on their turn")]
public class UnmountEnemy : SkillNode
{
    [Input]
    public ExecutionFlow In;
}
