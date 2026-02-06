using Turnroot.Audio.PreferredBattleMusic;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.Gameplay.PlayerSettings
{
    /// <summary>
    /// Stores player-configurable settings for gameplay, graphics, and audio preferences.
    /// </summary>
    [CreateAssetMenu(
        fileName = "GameplayPlayerSettings",
        menuName = "Turnroot/Gameplay/Gameplay Player Settings"
    )]
    public class GameplayPlayerSettings : SingletonScriptableObject<GameplayPlayerSettings>
    {
        // there are no headers or decorators because the player will never interact with this directly

        /// <summary>
        /// Defines game difficulty levels.
        /// </summary>
        public enum DifficultyLevel
        {
            Easy,
            Normal,
            Hard,
            Extreme,
        }

        /// <summary>
        /// Defines gameplay animation speed settings.
        /// </summary>
        public enum GameSpeed
        {
            Normal,
            Fast,
            VeryFast,
        }

        /// <summary>
        /// Defines which parts of enemy turns to skip or fast-forward.
        /// </summary>
        public enum SkipEnemyTurn
        {
            Movement,
            AllExceptAttacks,
            EntireTurn,
            None,
        }

        /// <summary>
        /// Defines when the battle grid overlay is visible.
        /// </summary>
        public enum BattleGridDisplay
        {
            UnitSelected,
            AlwaysOn,
            AlwaysOff,
        }

        /// <summary>
        /// Defines visual style of the battle grid overlay.
        /// </summary>
        public enum BattleGridDisplayStyle
        {
            Subtle,
            Colorful,
            Intense,
        }

        /// <summary>
        /// Defines which unit is selected at the start of each turn.
        /// </summary>
        public enum StartTurnUnit
        {
            Avatar,
            LastUnit,
            LowestHpUnit,
        }

        /// <summary>
        /// Defines the preferred input method for controls.
        /// </summary>
        public enum InputControlType
        {
            Keyboard,
            Gamepad,
        }

        // Logical actions exposed to the UI (e.g., Select, Back, Details...)
        /// <summary>
        /// Defines logical input actions for UI navigation and interaction.
        /// </summary>
        public enum LogicalAction
        {
            Select,
            Back,
            Details,
            NavigateUp,
            NavigateDown,
            NavigateLeft,
            NavigateRight,
            NavigateVector,
        }

        // gameplay
        public bool Permadeath;
        public bool TutorialPrompts = true;
        public DifficultyLevel GameDifficulty = DifficultyLevel.Normal;
        public GameSpeed SpeedSetting = GameSpeed.Normal;
        public SkipEnemyTurn SkipEnemyTurnAnimations = SkipEnemyTurn.Movement;
        public bool AutoEndTurn = true;

        public StartTurnUnit StartUnitSetting = StartTurnUnit.Avatar;
        public InputControlType PreferredInputControl = InputControlType.Keyboard;
        public bool DisableTurnwheel = false;
        public bool DisableTacticalLens = false;
        public BattleGridDisplay BattleGridSetting = BattleGridDisplay.AlwaysOn;
        public BattleGridDisplayStyle BattleGridStyle = BattleGridDisplayStyle.Subtle;

        // graphics
        // Brightness mapped to URP Color Adjustments.postExposure (range: -2..2)
        public float Brightness = 0.0f;

        // Contrast mapped to URP Color Adjustments.contrast (range: -50..50)
        public float Contrast = 0.0f;
        public float Quality = 1f;

        /// <summary>
        /// Discrete quality step (0..3) mapped from stored gameplay quality values.
        /// PlayerSettings.Quality will be one of {0, 0.1, 0.2, 0.3} and we map
        /// those to steps 0..3 by multiplying by 10 and clamping to [0,3].
        /// </summary>
        public int QualityStep => Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(Quality) * 10f), 0, 3);
        public bool Subtitles = true;
        public bool Bloom = true;
        public bool LensFlare = false;
        public bool DepthOfField = true;
        public bool AnimatedCameraMovement = true;

        // audio
        public float MusicVolume = 0.8f;
        public float SfxVolume = 0.8f;
        public float VoiceVolume = 0.8f;
        public bool MusicWhenPaused = true;
        public SongChoice PreferredBattleMusic = SongChoice.Default;
    }
}
