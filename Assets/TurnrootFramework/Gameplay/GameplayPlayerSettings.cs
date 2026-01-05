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
        public bool TutorialPrompts = true;
        public DifficultyLevel GameDifficulty = DifficultyLevel.Normal;

        public enum DifficultyLevel
        {
            Easy,
            Normal,
            Hard,
            Extreme,
        }

        public float Brightness = 1.0f;
        public float Gamma = 1.0f;
        public float Quality = 0.3f;
        public bool Subtitles = true;
        public bool Bloom = true;
        public bool DepthOfField = true;
        public bool AnimatedCameraMovement = true;
    }
}
