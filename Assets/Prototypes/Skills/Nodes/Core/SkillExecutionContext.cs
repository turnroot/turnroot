using System.Collections.Generic;
using Assets.Prototypes.Characters;
using UnityEngine;

namespace Assets.Prototypes.Skills.Nodes
{
    /// <summary>
    /// Runtime context passed through skill node execution.
    /// Contains all the dynamic data that nodes need to evaluate at runtime.
    /// </summary>
    public class SkillExecutionContext
    {
        public Skill Skill { get; set; }
        public CharacterInstance UnitInstance { get; set; }
        public List<CharacterInstance> Targets { get; set; }
        public SkillGraph SkillGraph { get; set; }
        public EnvironmentalConditions EnvironmentalConditions { get; set; }
        public Dictionary<string, object> CustomData { get; private set; }

        public bool IsInterrupted { get; set; }

        public SkillExecutionContext()
        {
            CustomData = new Dictionary<string, object>();
            Targets = new List<CharacterInstance>();
        }

        // Get a custom data value, or default if not found
        public T GetCustomData<T>(string key, T defaultValue = default)
        {
            if (CustomData.TryGetValue(key, out object value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }

        // Set a custom data value
        public void SetCustomData(string key, object value)
        {
            CustomData[key] = value;
        }
    }
}
