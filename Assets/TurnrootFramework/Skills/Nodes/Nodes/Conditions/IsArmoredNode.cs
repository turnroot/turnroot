using Turnroot.GameSettings;

namespace Turnroot.Skills.Nodes.Conditions
{
    /// <summary>
    /// Condition node that checks if a character has the Armored movement type.
    /// </summary>
    [CreateNodeMenu("Conditions/Status/Is Armored")]
    [NodeLabel("Checks if the unit is armored")]
    public class IsArmoredNode : MovementTypeNodeBase
    {
        protected override MovementType TargetMovementType => MovementType.Armored;
    }
}
