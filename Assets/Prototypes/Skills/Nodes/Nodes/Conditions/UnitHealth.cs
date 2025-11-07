using Assets.Prototypes.Characters;
using Assets.Prototypes.Characters.Stats;
using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Unit Health")]
[NodeLabel("Gets the health value of a unit")]
public class UnitHealth : SkillNode
{
    [Output]
    public FloatValue health;

    [Tooltip("Test health value used in editor mode")]
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
                // At runtime, get actual health from the UnitInstance
                healthValue.value = GetRuntimeHealth();
            }

            return healthValue;
        }
        return null;
    }

    private float GetRuntimeHealth()
    {
        if (graph is SkillGraph skillGraph)
        {
            // Access the context through the graph's active executor
            // The executor stores itself in the context with key "_executor"
            var contextFromGraph = GetContextFromGraph(skillGraph);
            if (contextFromGraph != null && contextFromGraph.UnitInstance != null)
            {
                // Try to get CharacterInstance component from the UnitInstance GameObject
                var characterInstance = contextFromGraph.UnitInstance;
                if (characterInstance != null)
                {
                    var healthStat = characterInstance.GetBoundedStat(BoundedStatType.Health);
                    if (healthStat != null)
                    {
                        return healthStat.Current;
                    }
                }
            }
        }

        // Fallback to default if context or health stat not available
        Debug.LogWarning("UnitHealth: Unable to retrieve runtime health, returning default value.");
        return defaultHealth;
    }
}
