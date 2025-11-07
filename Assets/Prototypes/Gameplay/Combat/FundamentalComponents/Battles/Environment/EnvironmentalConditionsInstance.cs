using UnityEngine;

namespace Assets.Prototypes.Gameplay.Combat.FundamentalComponents.Battles.Environment
{
    /// <summary>
    /// Instance of environmental conditions during a battle.
    /// Holds runtime data about current weather, terrain effects, etc.
    /// </summary>
    public class EnvironmentalConditionsInstance : MonoBehaviour
    {
        [SerializeField]
        private EnvironmentalConditions sourceConditions;

        // Runtime state - can be modified during battle
        public bool IsNight { get; set; }
        public bool IsRaining { get; set; }
        public bool IsFoggy { get; set; }
        public bool IsDesert { get; set; }
        public bool IsSnowing { get; set; }
        public bool IsIndoors { get; set; }

        private void Awake()
        {
            // Initialize from the source ScriptableObject if provided
            if (sourceConditions != null)
            {
                InitializeFromSource();
            }
        }

        /// <summary>
        /// Initialize runtime state from the source EnvironmentalConditions ScriptableObject.
        /// </summary>
        public void InitializeFromSource()
        {
            if (sourceConditions == null)
                return;

            IsNight = sourceConditions.IsNight;
            IsRaining = sourceConditions.IsRaining;
            IsFoggy = sourceConditions.IsFoggy;
            IsDesert = sourceConditions.IsDesert;
            IsSnowing = sourceConditions.IsSnowing;
            IsIndoors = sourceConditions.IsIndoors;
        }

        /// <summary>
        /// Set the source EnvironmentalConditions and initialize from it.
        /// </summary>
        public void SetSource(EnvironmentalConditions conditions)
        {
            sourceConditions = conditions;
            InitializeFromSource();
        }
    }
}
