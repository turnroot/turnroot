using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Unit Health")]
[NodeLabel("Gets the health value of a unit")]
public class UnitHealth : SkillNode
{
    [Output]
    public FloatValue health;

    [Tooltip("Default health value used in editor mode")]
    public float defaultHealth = 100f;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "health")
        {
            FloatValue healthValue = new();
            
            // In editor mode, return the default value
            if (!Application.isPlaying)
            {
                healthValue.value = defaultHealth;
            }
            else
            {
                // At runtime, get actual health from the caster unit
                // TODO: User will add the logic to retrieve actual health from context.Caster
                healthValue.value = defaultHealth;
            }
            
            return healthValue;
        }
        return null;
    }
}
