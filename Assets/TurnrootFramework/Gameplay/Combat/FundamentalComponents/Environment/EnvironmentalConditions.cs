using NaughtyAttributes;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Environment
{
    public class EnvironmentalConditions : MonoBehaviour
    {
        [Foldout("Temperature")]
        public bool IsVeryHot;

        [Foldout("Temperature")]
        public bool IsVeryCold;

        [Foldout("Time")]
        public bool IsNight;

        [Foldout("Time")]
        public bool IsSunset;

        [Foldout("Time")]
        public bool IsDawn;

        [Foldout("Weather")]
        public bool IsRaining;

        [Foldout("Weather")]
        public bool IsFoggy;

        [Foldout("Weather")]
        public bool IsStormy;

        [Foldout("Weather")]
        public bool IsWindy;

        [Foldout("Weather")]
        public bool HasSunlight = true;

        [Foldout("Weather")]
        public bool IsSnowing;

        [Foldout("Environment")]
        public bool IsUnderwater;

        [Foldout("Environment")]
        public bool IsRocky;

        [Foldout("Environment")]
        public bool IsSwampy;

        [Foldout("Environment")]
        public bool IsVolcanic;
    }
}
