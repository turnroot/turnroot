using Turnroot.Audio.PreferredBattleMusic;
using UnityEngine;

namespace Turnroot.Gameplay.PlayerSettings
{
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
            Auto, // Automatically detect based on last input
            Keyboard,
            Gamepad,
        }

        // gameplay
        public bool Permadeath;
        public bool TutorialPrompts = true;
        public DifficultyLevel GameDifficulty = DifficultyLevel.Normal;
        public GameSpeed SpeedSetting = GameSpeed.Normal;
        public SkipEnemyTurn SkipEnemyTurnAnimations = SkipEnemyTurn.Movement;
        public bool AutoEndTurn = true;

        public StartTurnUnit StartUnitSetting = StartTurnUnit.Avatar;
        public InputControlType PreferredInputControl = InputControlType.Auto;
        public bool DisableTurnwheel = false;
        public bool DisableTacticalLens = false;
        public BattleGridDisplay BattleGridSetting = BattleGridDisplay.AlwaysOn;
        public BattleGridDisplayStyle BattleGridStyle = BattleGridDisplayStyle.Subtle;

        // graphics
        public float Brightness = 1.0f;
        public float Gamma = 1.0f;
        public float Quality = 0.3f;
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
