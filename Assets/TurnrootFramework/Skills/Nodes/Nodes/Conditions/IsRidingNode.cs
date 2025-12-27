using Turnroot.GameSettings;

namespace Turnroot.Skills.Nodes.Conditions
{
    [CreateNodeMenu("Conditions/Status/Is Riding")]
    [NodeLabel("Checks if the unit is riding")]
    public class IsRidingNode : MovementTypeNodeBase
    {
        protected override MovementType TargetMovementType => MovementType.Riding;
    }
}
