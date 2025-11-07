using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Map Day")]
[NodeLabel("Checks if it is daytime on the map")]
public class MapDay : SkillNode
{
    [Output]
    public BoolValue isDay;

    [Tooltip("Test value used in editor mode")]
    public bool editorTestValue = true;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "isDay")
        {
            BoolValue dayValue = new();
            
            // In editor mode, return the test value
            if (!Application.isPlaying)
            {
                dayValue.value = editorTestValue;
            }
            else
            {
                // At runtime, get actual time of day state
                // TODO: User will implement time of day system and add logic here
                dayValue.value = true;
            }
            
            return dayValue;
        }
        return null;
    }
}
