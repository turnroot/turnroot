namespace Turnroot.Skills.Nodes.Conditions
{
    [CreateNodeMenu("Conditions/Status/Is Flying")]
    [NodeLabel("Checks if the unit is flying")]
    public class IsFlyingNode : MovementTypeNodeBase
    {
        protected override MovementType TargetMovementType => MovementType.Flying;
    }
}
