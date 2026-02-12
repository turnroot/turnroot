using Turnroot.Utilities;

namespace Turnroot.Gameplay.Combat
{
    internal static class BattleGameObjectLogging
    {
        public static void LogInfo(this BattleGameObject bg, string message) =>
            $"BattleGameObject: {message}".LogInfo();

        public static void LogWarning(this BattleGameObject bg, string message) =>
            $"BattleGameObject: {message}".LogWarning();

        public static void LogError(this BattleGameObject bg, string message) =>
            $"BattleGameObject: {message}".LogError();
    }
}
