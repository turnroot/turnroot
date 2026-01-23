using Turnroot.Gameplay.Brain.Components;
using Turnroot.Gameplay.PlayerSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Handles persistence and management of player settings through LTM
    /// </summary>
    public class PlayerSettingsPersistence
    {
        private readonly LongTermMemory _longTermMemory;
        private readonly GamewideContextBrain _brain;

        public GameplayPlayerSettings PlayerSettings { get; private set; }

        public PlayerSettingsPersistence(LongTermMemory longTermMemory, GamewideContextBrain brain)
        {
            _longTermMemory = longTermMemory;
            _brain = brain;
        }

        public void Initialize()
        {
            PlayerSettings = GameplayPlayerSettings.Instance;
            if (PlayerSettings == null)
            {
#if UNITY_EDITOR
                Debug.LogError(
                    "PlayerSettingsPersistence: Could not find GameplayPlayerSettings instance"
                );
#endif
                return;
            }

            // Load saved settings from LTM
            LoadPlayerSettingsFromLTM();
        }

        private OperationResult LoadPlayerSettingsFromLTM()
        {
            if (_longTermMemory == null)
            {
                return OperationResult.Failure(
                    "PlayerSettingsPersistence: No LongTermMemory available for loading settings"
                );
            }

            var settingsData = _longTermMemory.Recall("PlayerSettings");
            if (string.IsNullOrEmpty(settingsData))
            {
                return OperationResult.Failure(
                    "PlayerSettingsPersistence: No saved player settings found in LTM"
                );
            }

            try
            {
                var decode =
                    GamewideContextBrainHelpers.DecodeInstanceFromString<PlayerSettingsSaveData>(
                        _brain,
                        settingsData
                    );
                if (decode.Success && decode.Value != null)
                {
                    ApplySettingsData(decode.Value);
                    return OperationResult.Successful();
                }
            }
            catch (System.Exception ex)
            {
                return OperationResult.Failure(
                    $"PlayerSettingsPersistence: Load player settings failed: {ex.Message}"
                );
            }
            return OperationResult.Failure(
                "PlayerSettingsPersistence: Failed to decode player settings from LTM"
            );
        }

        private void ApplySettingsData(PlayerSettingsSaveData data)
        {
            if (PlayerSettings == null)
            {
                return;
            }

            PlayerSettings.TutorialPrompts = data.TutorialPrompts;
            PlayerSettings.GameDifficulty = data.GameDifficulty;
            PlayerSettings.SpeedSetting = data.SpeedSetting;
            PlayerSettings.SkipEnemyTurnAnimations = data.SkipEnemyTurnAnimations;
            PlayerSettings.BattleGridSetting = data.BattleGridSetting;
            PlayerSettings.BattleGridStyle = data.BattleGridStyle;
            PlayerSettings.StartUnitSetting = data.StartUnitSetting;
            PlayerSettings.Brightness = data.Brightness;
            PlayerSettings.Contrast = data.Contrast;
            PlayerSettings.Quality = data.Quality;
            PlayerSettings.Subtitles = data.Subtitles;
            PlayerSettings.Bloom = data.Bloom;
            PlayerSettings.DepthOfField = data.DepthOfField;
            PlayerSettings.LensFlare = data.LensFlare;
            PlayerSettings.AnimatedCameraMovement = data.AnimatedCameraMovement;
            PlayerSettings.AutoEndTurn = data.AutoEndTurn;
            PlayerSettings.Permadeath = data.Permadeath;
            PlayerSettings.DisableTurnwheel = data.DisableTurnwheel;
            PlayerSettings.DisableTacticalLens = data.DisableTacticalLens;
            PlayerSettings.MusicWhenPaused = data.MusicWhenPaused;
            PlayerSettings.MusicVolume = data.MusicVolume;
            PlayerSettings.SfxVolume = data.SfxVolume;
            PlayerSettings.VoiceVolume = data.VoiceVolume;
            PlayerSettings.PreferredBattleMusic = data.PreferredBattleMusic;
        }

        public OperationResult SavePlayerSettings()
        {
            if (PlayerSettings == null)
            {
                return OperationResult.Failure(
                    "PlayerSettingsPersistence: No player settings to save"
                );
            }

            try
            {
                var saveData = new PlayerSettingsSaveData
                {
                    TutorialPrompts = PlayerSettings.TutorialPrompts,
                    GameDifficulty = PlayerSettings.GameDifficulty,
                    SpeedSetting = PlayerSettings.SpeedSetting,
                    SkipEnemyTurnAnimations = PlayerSettings.SkipEnemyTurnAnimations,
                    BattleGridSetting = PlayerSettings.BattleGridSetting,
                    BattleGridStyle = PlayerSettings.BattleGridStyle,
                    StartUnitSetting = PlayerSettings.StartUnitSetting,
                    Brightness = PlayerSettings.Brightness,
                    Contrast = PlayerSettings.Contrast,
                    Quality = PlayerSettings.Quality,
                    Subtitles = PlayerSettings.Subtitles,
                    Bloom = PlayerSettings.Bloom,
                    LensFlare = PlayerSettings.LensFlare,
                    DepthOfField = PlayerSettings.DepthOfField,
                    AnimatedCameraMovement = PlayerSettings.AnimatedCameraMovement,
                    AutoEndTurn = PlayerSettings.AutoEndTurn,
                    Permadeath = PlayerSettings.Permadeath,
                    DisableTurnwheel = PlayerSettings.DisableTurnwheel,
                    DisableTacticalLens = PlayerSettings.DisableTacticalLens,
                    MusicWhenPaused = PlayerSettings.MusicWhenPaused,
                    MusicVolume = PlayerSettings.MusicVolume,
                    SfxVolume = PlayerSettings.SfxVolume,
                    VoiceVolume = PlayerSettings.VoiceVolume,
                    PreferredBattleMusic = PlayerSettings.PreferredBattleMusic,
                };

                var encode = GamewideContextBrainHelpers.EncodeInstanceToString(_brain, saveData);
                _longTermMemory?.Remember("PlayerSettings", encode.Value);
                return OperationResult.Successful();
            }
            catch (System.Exception ex)
            {
                return OperationResult.Failure(
                    $"PlayerSettingsPersistence: Save player settings failed: {ex.Message}"
                );
            }
        }

        public void UpdatePlayerSetting(string settingName, object value)
        {
            if (PlayerSettings == null)
            {
                return;
            }

            try
            {
                switch (settingName.ToLower())
                {
                    case "tutorialprompts":
                        if (value is bool tutorialPrompts)
                        {
                            PlayerSettings.TutorialPrompts = tutorialPrompts;
                        }
                        break;
                    case "gamedifficulty":
                        if (value is GameplayPlayerSettings.DifficultyLevel difficulty)
                        {
                            PlayerSettings.GameDifficulty = difficulty;
                        }
                        break;
                    case "speedsetting":
                        if (value is GameplayPlayerSettings.GameSpeed speed)
                        {
                            PlayerSettings.SpeedSetting = speed;
                        }
                        break;
                    case "skipenemyturnanimations":
                        if (value is GameplayPlayerSettings.SkipEnemyTurn skipEnemyTurn)
                        {
                            PlayerSettings.SkipEnemyTurnAnimations = skipEnemyTurn;
                        }
                        break;
                    case "battlegridsetting":
                        if (value is GameplayPlayerSettings.BattleGridDisplay battleGridDisplay)
                        {
                            PlayerSettings.BattleGridSetting = battleGridDisplay;
                        }
                        break;
                    case "battlegridstyle":
                        if (value is GameplayPlayerSettings.BattleGridDisplayStyle battleGridStyle)
                        {
                            PlayerSettings.BattleGridStyle = battleGridStyle;
                        }
                        break;
                    case "startunitsetting":
                        if (value is GameplayPlayerSettings.StartTurnUnit startTurnUnit)
                        {
                            PlayerSettings.StartUnitSetting = startTurnUnit;
                        }
                        break;
                    case "brightness":
                        if (value is float brightness)
                        {
                            // Brightness uses postExposure range -2..2
                            PlayerSettings.Brightness = Mathf.Clamp(brightness, -2f, 2f);
                        }
                        break;
                    case "contrast":
                        if (value is float contrast)
                        {
                            // Contrast stored as -50..50 per product decision
                            PlayerSettings.Contrast = Mathf.Clamp(contrast, -50f, 50f);
                        }
                        break;
                    case "quality":
                        if (value is float quality)
                        {
                            // Quantize incoming quality to one of {0, 0.1, 0.2, 0.3}
                            var q = Mathf.Clamp01(quality);
                            var quant = Mathf.Round(q * 10f) / 10f;
                            quant = Mathf.Clamp(quant, 0f, 0.3f);
                            PlayerSettings.Quality = quant;
                        }
                        break;
                    case "subtitles":
                        if (value is bool subtitles)
                        {
                            PlayerSettings.Subtitles = subtitles;
                        }
                        break;
                    case "bloom":
                        if (value is bool bloom)
                        {
                            PlayerSettings.Bloom = bloom;
                        }
                        break;
                    case "lensflare":
                        if (value is bool lensFlare)
                        {
                            PlayerSettings.LensFlare = lensFlare;
                        }
                        break;
                    case "depthoffield":
                        if (value is bool depthOfField)
                        {
                            PlayerSettings.DepthOfField = depthOfField;
                        }
                        break;
                    case "animatedcameramovement":
                        if (value is bool animatedCamera)
                        {
                            PlayerSettings.AnimatedCameraMovement = animatedCamera;
                        }
                        break;
                    case "autoendturn":
                        if (value is bool autoEndTurn)
                        {
                            PlayerSettings.AutoEndTurn = autoEndTurn;
                        }
                        break;
                    case "permadeath":
                        if (value is bool permadeath)
                        {
                            PlayerSettings.Permadeath = permadeath;
                        }
                        break;
                    case "disableturnwheel":
                        if (value is bool disableTurnwheel)
                        {
                            PlayerSettings.DisableTurnwheel = disableTurnwheel;
                        }
                        break;
                    case "disabletacticallens":
                        if (value is bool disableTacticalLens)
                        {
                            PlayerSettings.DisableTacticalLens = disableTacticalLens;
                        }
                        break;
                    case "musicwhenpaused":
                        if (value is bool musicWhenPaused)
                        {
                            PlayerSettings.MusicWhenPaused = musicWhenPaused;
                        }
                        break;
                    case "musicvolume":
                        if (value is float musicVolume)
                        {
                            PlayerSettings.MusicVolume = Mathf.Clamp01(musicVolume);
                        }
                        break;
                    case "sfxvolume":
                        if (value is float sfxVolume)
                        {
                            PlayerSettings.SfxVolume = Mathf.Clamp01(sfxVolume);
                        }
                        break;
                    case "voicevolume":
                        if (value is float voiceVolume)
                        {
                            PlayerSettings.VoiceVolume = Mathf.Clamp01(voiceVolume);
                        }
                        break;
                    case "preferredbattlemusic":
                        if (value is Audio.PreferredBattleMusic.SongChoice preferredBattleMusic)
                        {
                            PlayerSettings.PreferredBattleMusic = preferredBattleMusic;
                        }
                        break;
                    default:
                        return;
                }

                // Auto-save after each setting change
                SavePlayerSettings();

                TurnrootLogger.Log($"Updated setting {settingName} to {value}");
            }
            catch (System.Exception ex)
            {
                TurnrootLogger.Log(
                    $"Failed to update setting {settingName}: {ex.Message}",
                    TurnrootLogger.LogLevel.Error
                );
            }
        }
    }

    /// <summary>
    /// Serializable DTO for player settings saves - includes all enum types
    /// </summary>
    [System.Serializable]
    public class PlayerSettingsSaveData
    {
        // Gameplay settings
        public bool TutorialPrompts = true;
        public GameplayPlayerSettings.DifficultyLevel GameDifficulty = GameplayPlayerSettings
            .DifficultyLevel
            .Normal;
        public GameplayPlayerSettings.GameSpeed SpeedSetting = GameplayPlayerSettings
            .GameSpeed
            .Normal;
        public GameplayPlayerSettings.SkipEnemyTurn SkipEnemyTurnAnimations = GameplayPlayerSettings
            .SkipEnemyTurn
            .Movement;
        public GameplayPlayerSettings.BattleGridDisplay BattleGridSetting = GameplayPlayerSettings
            .BattleGridDisplay
            .AlwaysOn;
        public GameplayPlayerSettings.BattleGridDisplayStyle BattleGridStyle =
            GameplayPlayerSettings.BattleGridDisplayStyle.Subtle;
        public GameplayPlayerSettings.StartTurnUnit StartUnitSetting = GameplayPlayerSettings
            .StartTurnUnit
            .Avatar;

        // Graphics settings
        // Brightness maps to URP Color Adjustments.postExposure (-2..2). Default neutral = 0.
        public float Brightness = 0.0f;

        // Contrast maps to URP Color Adjustments.contrast (-50..50). Default neutral = 0.
        public float Contrast = 0.0f;

        public float Quality = 0.3f;
        public bool Subtitles = true;
        public bool Bloom = true;
        public bool LensFlare = false;
        public bool DepthOfField = true;
        public bool AnimatedCameraMovement = true;
        public bool AutoEndTurn = true;

        // Additional gameplay settings
        public bool Permadeath = false;
        public bool DisableTurnwheel = false;
        public bool DisableTacticalLens = false;
        public bool MusicWhenPaused = true;

        // Audio settings
        public float MusicVolume = 0.8f;
        public float SfxVolume = 0.8f;
        public float VoiceVolume = 0.8f;
        public Audio.PreferredBattleMusic.SongChoice PreferredBattleMusic = Turnroot
            .Audio
            .PreferredBattleMusic
            .SongChoice
            .Default;
    }
}
