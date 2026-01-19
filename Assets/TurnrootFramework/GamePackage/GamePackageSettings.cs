using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NaughtyAttributes;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.GamePackage
{
    [Serializable]
    public struct Credits
    {
        public string role;
        public string name;
    }

    [Serializable]
    public struct Studio
    {
        public string name;
        public Sprite logo;
    }

    [Serializable]
    public struct GraphicsPack
    {
        public string packName;
        public string packCreator;
    }

    [CreateAssetMenu(
        fileName = "GamePackageSettings",
        menuName = "Turnroot/Game Settings/Game Package Settings"
    )]
    public class GamePackageSettings : SingletonScriptableObject<GamePackageSettings>
    {
        [Header("General Information"), HorizontalLine(color: EColor.Red)]
        public string GameName;

        [TextArea(3, 10)]
        public string GameDescription;
        public string GameTagline;
        public string GameUrl;
        public bool HasNsfwContent = false;
        public bool ContainsAds = false;

        [TextArea(3, 10)]
        public string PregameDisclaimer;
        public List<Credits> GameCredits = new();
        public List<GraphicsPack> GraphicsPacksUsed = new();

        [Button]
        public void GetModules() => Application.OpenURL("https://"); // TODO: link to asset store

        [Header("Modules"), HorizontalLine(color: EColor.Yellow)]
        [field: ReadOnly]
        public bool HubModule =
#if TURNROOT_HUB_MODULE
            true;
#else
            false;
#endif

        [field: ReadOnly]
        public bool BloodlinesModule =
#if TURNROOT_BLOODLINES_MODULE
            true;
#else
            false;
#endif

        [field: ReadOnly]
        public bool RetroModule =
#if TURNROOT_RETRO_MODULE
            true;
#else
            false;
#endif

        [field: ReadOnly]
        public bool UnwindModule =
#if TURNROOT_UNWIND_MODULE
            true;
#else
            false;
#endif

        [field: ReadOnly]
        public bool TroopsModule =
#if TURNROOT_TROOPS_MODULE
            true;
#else
            false;
#endif

        [field: ReadOnly]
        public bool MonstersModule =
#if TURNROOT_MONSTERS_MODULE
            true;
#else
            false;
#endif

        [SerializeField, Header("Versioning"), HorizontalLine(color: EColor.Blue)]
        public string VersionText = "1.0.0";

        [Header("Platform Settings"), HorizontalLine(color: EColor.Gray)]
        public List<RuntimePlatform> SupportedPlatforms = new();

        [Header("Localization"), HorizontalLine(color: EColor.Green)]
        public SystemLanguage DefaultLanguage = SystemLanguage.English;
        public List<SystemLanguage> SupportedLanguages = new();

        [Header("Accessibility"), HorizontalLine(color: EColor.Indigo)]
        public bool ColorblindFriendly = false;
        public bool NeedsFlashingLightsWarning = false;

        [Header("Legal Information"), HorizontalLine(color: EColor.Orange)]
        public List<Studio> Publishers = new();
        public List<Studio> Studios = new();

        [TextArea(3, 10)]
        public string CopyrightInfo;

        private void OnValidate()
        {
            // Validate URLs when changed in editor
            if (!string.IsNullOrEmpty(GameUrl) && !ValidateUrl(GameUrl))
            {
                Debug.LogWarning($"Invalid Game URL: {GameUrl}");
            }

            // Validate version number format (semantic versioning: x.y.z or x.y.z-suffix)
            if (!string.IsNullOrEmpty(VersionText))
            {
                VersionText = VersionText.Trim();
                if (!ValidateVersion(VersionText))
                {
                    Debug.LogWarning(
                        $"Invalid Version format: {VersionText}. Expected format: x.y.z (e.g., 1.2.3) or x.y.z-suffix (e.g., 1.2.3-beta)"
                    );
                }
            }
        }

        private bool ValidateUrl(string url) => Uri.IsWellFormedUriString(url, UriKind.Absolute);

        private bool ValidateVersion(string version)
        {
            // Semantic versioning regex: major.minor.patch with optional pre-release suffix
            string pattern =
                @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?$";
            return Regex.IsMatch(version, pattern);
        }
    }
}
