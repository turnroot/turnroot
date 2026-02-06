using NaughtyAttributes;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Environment
{
    /// <summary>
    /// Manages environmental conditions for battles including temperature, time of day, weather, and terrain types.
    /// </summary>
    public class EnvironmentalConditions : MonoBehaviour
    {
        [InfoBox("Only one of 'Very Hot' and 'Very Cold' can be enabled at a time")]
        [Foldout("Temperature")]
        [SerializeField]
        private bool _isVeryHot;

        public bool IsVeryHot
        {
            get => _isVeryHot;
            set => _isVeryHot = value && !_isVeryCold;
        }

        [Foldout("Temperature")]
        [SerializeField]
        private bool _isVeryCold;

        public bool IsVeryCold
        {
            get => _isVeryCold;
            set => _isVeryCold = value && !_isVeryHot;
        }

        [InfoBox(
            "Only one of 'Night', 'Sunset', and 'Dawn' can be enabled at a time. Enable none to default to Day."
        )]
        [Foldout("Time")]
        [SerializeField]
        private bool _isNight;

        public bool IsNight
        {
            get => _isNight;
            set => _isNight = value && !IsDawn && !IsSunset;
        }

        [Foldout("Time")]
        [SerializeField]
        private bool _isSunset;

        public bool IsSunset
        {
            get => _isSunset;
            set => _isSunset = value && !IsNight && !IsDawn;
        }

        [Foldout("Time")]
        [SerializeField]
        private bool _isDawn;

        public bool IsDawn
        {
            get => _isDawn;
            set => _isDawn = value && !IsNight && !IsSunset;
        }

        [Foldout("Weather")]
        public bool IsRaining;

        [Foldout("Weather")]
        public bool IsFoggy;

        [Foldout("Weather")]
        public bool IsStormy;

        [Foldout("Weather")]
        public bool IsWindy;

        [InfoBox(
            "'HasSunlight' will be disabled if 'Foggy' or 'Stormy' is enabled, or if it is 'Night'"
        )]
        [Foldout("Weather")]
        [SerializeField]
        private bool _hasSunlight;

        public bool HasSunlight
        {
            get => _hasSunlight;
            set => _hasSunlight = value && !IsFoggy && !IsStormy;
        }

        [InfoBox("'Snowing' cannot be enabled when 'Very Hot' is set")]
        [Foldout("Weather")]
        [SerializeField]
        private bool _isSnowing;

        public bool IsSnowing
        {
            get => _isSnowing;
            set => _isSnowing = value && !_isVeryHot;
        }

        private void OnValidate()
        {
            // Temperature: only one of very hot / very cold
            if (_isVeryHot && _isVeryCold)
            {
                _isVeryCold = false;
            }

            // Time: only one of night, dawn, sunset
            if (_isNight)
            {
                _isSunset = false;
                _isDawn = false;
                _hasSunlight = false;
            }
            if (_isSunset)
            {
                _isNight = false;
                _isDawn = false;
            }
            if (_isDawn)
            {
                _isNight = false;
                _isSunset = false;
            }

            // Sunlight: not with fog or storm
            if (_hasSunlight && (IsFoggy || IsStormy))
            {
                _hasSunlight = false;
            }

            // Snowing: not when very hot
            if (_isSnowing && _isVeryHot)
            {
                _isSnowing = false;
            }

            // Only one of underwater, underground, desert, rocky, swampy, volcanic
            EnforceExclusiveEnvironment();
        }

        private void EnforceExclusiveEnvironment()
        {
            // Count how many environment flags are set
            int count = 0;
            int firstActiveIndex = -1;

            var flags = new[]
            {
                IsUnderwater,
                IsUnderground,
                IsDesert,
                IsRocky,
                IsSwampy,
                IsVolcanic,
            };

            for (int i = 0; i < flags.Length; i++)
            {
                if (flags[i])
                {
                    if (firstActiveIndex == -1)
                    {
                        firstActiveIndex = i;
                    }
                    count++;
                }
            }

            if (count <= 1)
            {
                return; // Only one or none active, no action needed
            }

            // More than one active - disable all except the first
            if (firstActiveIndex != 0)
            {
                IsUnderwater = false;
            }

            if (firstActiveIndex != 1)
            {
                IsUnderground = false;
            }

            if (firstActiveIndex != 2)
            {
                IsDesert = false;
            }

            if (firstActiveIndex != 3)
            {
                IsRocky = false;
            }

            if (firstActiveIndex != 4)
            {
                IsSwampy = false;
            }

            if (firstActiveIndex != 5)
            {
                IsVolcanic = false;
            }
        }

        [Foldout("Environment")]
        public bool IsUnderwater;

        [Foldout("Environment")]
        public bool IsUnderground;

        [Foldout("Environment")]
        public bool IsDesert;

        [Foldout("Environment")]
        public bool IsRocky;

        [Foldout("Environment")]
        public bool IsSwampy;

        [Foldout("Environment")]
        public bool IsVolcanic;
    }
}
