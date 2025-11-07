using Assets.Prototypes.Characters.Stats;
using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Combat/Enemy Would Kill Unit")]
[NodeLabel("Check if incoming damage would be lethal")]
public class EnemyWouldKillUnit : SkillNode
{
    [Input]
    [Tooltip("The amount of incoming damage")]
    public FloatValue incomingDamage;

    [Output]
    public BoolValue result;

    [Tooltip("Test value for damage in editor mode")]
    public float testDamage = 50f;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "result")
        {
            BoolValue wouldKill = new();

            // In editor mode, use test value
            if (!Application.isPlaying)
            {
                wouldKill.value = testDamage >= 100f; // Arbitrary test
            }
            else
            {
                // At runtime, check if damage would kill unit
                if (graph is SkillGraph skillGraph)
                {
                    var context = GetContextFromGraph(skillGraph);
                    if (context != null && context.UnitInstance != null)
                    {
                        // Get incoming damage
                        float damage = testDamage;
                        var damagePort = GetInputPort("incomingDamage");
                        if (damagePort != null && damagePort.IsConnected)
                        {
                            var inputValue = damagePort.GetInputValue();
                            if (inputValue is FloatValue damageValue)
                            {
                                damage = damageValue.value;
                            }
                        }

                        // Check unit's current HP
                        var healthStat = context.UnitInstance.GetBoundedStat(
                            BoundedStatType.Health
                        );

                        if (healthStat != null)
                        {
                            // Would this damage reduce HP to 0 or below?
                            wouldKill.value = (healthStat.Current - damage) <= 0;
                        }
                        else
                        {
                            Debug.LogWarning("EnemyWouldKillUnit: Unit has no Health stat");
                            wouldKill.value = false;
                        }
                    }
                }
            }

            return wouldKill;
        }

        return null;
    }
}
