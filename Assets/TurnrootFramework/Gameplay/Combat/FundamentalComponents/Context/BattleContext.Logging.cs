using Turnroot.Utilities;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    internal static class BattleContextLogging
    {
        public static void LogInfo(this BattleContext context, string message) =>
            $"BattleContext: {message}".LogInfo();

        public static void LogWarning(this BattleContext context, string message) =>
            $"BattleContext: {message}".LogWarning();

        public static void LogError(this BattleContext context, string message) =>
            $"BattleContext: {message}".LogError();
    }
}
