using System;
using System.Collections.Generic;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Conditions.Specific
{
    /// <summary>
    /// Condition to interact with some MapGridPointFeatures.
    /// </summary>
    [Serializable]
    public class InteractWithMapGridPointFeaturesBattleCondition : BattleCondition
    {
        [SerializeField]
        public string[] FeatureIDsToInteractWith;

        [SerializeField]
        public bool allFeatures = true;

        public InteractWithMapGridPointFeaturesBattleCondition(
            string name,
            string description,
            string[] featureIDsToInteractWith,
            bool allFeatures = true
        )
            : base(name, description)
        {
            FeatureIDsToInteractWith = featureIDsToInteractWith ?? Array.Empty<string>();
            this.allFeatures = allFeatures;
        }

        public InteractWithMapGridPointFeaturesBattleCondition()
            : base("Interact With Features", "Interact with the listed MapGridPoint features")
        {
            FeatureIDsToInteractWith = Array.Empty<string>();
            allFeatures = true;
        }

        public void CheckCondition(List<string> interactedFeatureIDs)
        {
            if (allFeatures)
            {
                foreach (var featureID in FeatureIDsToInteractWith)
                {
                    if (!interactedFeatureIDs.Contains(featureID))
                    {
                        return;
                    }
                }
                ConditionMet();
            }
            else
            {
                foreach (var featureID in FeatureIDsToInteractWith)
                {
                    if (interactedFeatureIDs.Contains(featureID))
                    {
                        ConditionMet();
                        return;
                    }
                }
            }
        }
    }
}
