using Turnroot.GameSettings;

namespace Turnroot.Skills.Nodes.Conditions
{
    /// <summary>
    /// Condition node that checks if a character has the Flying movement type.
    /// </summary>
    [CreateNodeMenu("Conditions/Status/Is Flying")]
    [NodeLabel("Checks if the unit is flying")]
    public class IsFlyingNode : MovementTypeNodeBase
    {
        protected override MovementType TargetMovementType => MovementType.Flying;
    }
}
