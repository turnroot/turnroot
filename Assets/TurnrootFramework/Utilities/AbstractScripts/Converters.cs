namespace Turnroot.Utilities
{
    public class Converters
    {
        public static string SecondsToHoursAndMinutes(int totalSeconds)
        {
            int hours = totalSeconds / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            return $"{hours}h {minutes}m";
        }

        public static int PosMod(int a, int b) => ((a % b) + b) % b;
    }
}
