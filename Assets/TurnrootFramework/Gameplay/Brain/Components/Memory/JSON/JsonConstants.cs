namespace Turnroot.Gameplay.Brain.Components.Memory.JSON
{
    /// <summary>
    /// Common JSON property names used by the memory serialization system. Having a
    /// single source of truth prevents typos and eases refactoring.
    /// </summary>
    public static class JsonConstants
    {
        public const string UnityMarker = "__unity";
        public const string Type = "type";
        public const string Name = "name";
        public const string AssetPath = "assetPath";
        public const string Guid = "guid";
        public const string Payload = "Payload";
        public const string Version = "Version";
    }
}
