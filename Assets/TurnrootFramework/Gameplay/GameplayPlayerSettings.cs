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

        public enum DifficultyLevel
        {
            Easy,
            Normal,
            Hard,
            Extreme,
        }

        public enum GameSpeed
        {
            Normal,
            Fast,
            VeryFast,
        }

        public enum SkipEnemyTurn
        {
            Movement,
            AllExceptAttacks,
            EntireTurn,
            None,
        }

        public enum BattleGridDisplay
        {
            UnitSelected,
            AlwaysOn,
            AlwaysOff,
        }

        public enum BattleGridDisplayStyle
        {
            Subtle,
            Colorful,
            Intense,
        }

        public enum StartTurnUnit
        {
            Avatar,
            LastUnit,
            LowestHpUnit,
        }

        public enum InputControlType
        {
            Keyboard,
            Gamepad,
        }

        public float ExploreMouseSensitivity = .5f;
        public bool InvertExploreMouse = false;
        public float ExploreMouseSpeed = .5f;
        public float ExploreMovementSpeed = .5f;

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

        public float Brightness = 0.0f;
        public float Contrast = 0.0f;
        public float Quality = 1f;
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
