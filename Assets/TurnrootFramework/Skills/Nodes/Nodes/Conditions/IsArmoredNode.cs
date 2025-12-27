using Turnroot.GameSettings;

namespace Turnroot.Skills.Nodes.Conditions
{
    [CreateNodeMenu("Conditions/Status/Is Armored")]
    [NodeLabel("Checks if the unit is armored")]
    public class IsArmoredNode : MovementTypeNodeBase
    {
        protected override MovementType TargetMovementType => MovementType.Armored;
    }
}
