using UnityEngine;

namespace Assets.Prototypes.Gameplay.Combat.FundamentalComponents.Battles.Environment
{
    [CreateAssetMenu(
        fileName = "EnvironmentalConditions",
        menuName = "Turnroot/Gameplay/Combat/Environmental Conditions"
    )]
    public class EnvironmentalConditions : ScriptableObject
    {
        public bool IsNight;
        public bool IsRaining;
        public bool IsFoggy;
        public bool IsDesert;
        public bool IsSnowing;
        public bool IsIndoors;
    }
}
