using Turnroot.GameSettings;

namespace Turnroot.Skills.Nodes.Conditions
{
    /// <summary>
    /// Condition node that checks if a character has the Riding movement type.
    /// </summary>
    [CreateNodeMenu("Conditions/Status/Is Riding")]
    [NodeLabel("Checks if the unit is riding")]
    public class IsRidingNode : MovementTypeNodeBase
    {
        protected override MovementType TargetMovementType => MovementType.Riding;
    }
}
