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
        public List<Credits> GameCredits;
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
        public List<Studio> StudioCredits = new();
        public List<GraphicsPack> GraphicsPacksUsed = new();

        [Header("Localization"), HorizontalLine(color: EColor.Green)]
        public SystemLanguage DefaultLanguage = SystemLanguage.English;
        public List<SystemLanguage> SupportedLanguages = new();

        [Header("Accessibility"), HorizontalLine(color: EColor.Indigo)]
        public bool ColorblindFriendly = false;
        public bool NeedsFlashingLightsWarning = false;

        public bool HasSubtitles = true;

        [TextArea(3, 10)]
        public string CopyrightInfo;
    }
}
