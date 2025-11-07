using Assets.Prototypes.Skills.Nodes;
using Assets.Prototypes.Characters;
using Assets.Prototypes.Characters.Stats;
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
                healthValue.value = GetRuntimeHealth();
            }
            
            return healthValue;
        }
        return null;
    }

    private float GetRuntimeHealth()
    {
        // Try to get the execution context from the graph
        if (graph is SkillGraph skillGraph)
        {
            // Access the context through the graph's active executor
            // The executor stores itself in the context with key "_executor"
            var contextFromGraph = GetContextFromGraph(skillGraph);
            if (contextFromGraph != null && contextFromGraph.Caster != null)
            {
                // Try to get CharacterInstance component from the caster GameObject
                var characterInstance = contextFromGraph.Caster.GetComponent<CharacterInstance>();
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
        return defaultHealth;
    }

    private SkillExecutionContext GetContextFromGraph(SkillGraph skillGraph)
    {
        // Use reflection to access the private activeExecutor field
        var executorField = typeof(SkillGraph).GetField("activeExecutor", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (executorField != null)
        {
            var executor = executorField.GetValue(skillGraph) as SkillGraphExecutor;
            if (executor != null)
            {
                return executor.GetContext();
            }
        }
        
        return null;
    }
}
