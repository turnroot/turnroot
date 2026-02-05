using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Gameplay.Maps;

namespace Turnroot.Gameplay.Brain
{
    public static partial class GamewideContextBrainHelpers
    {
        public enum ExploredState
        {
            NotExplored,
            PartiallyExplored,
            FullyExplored,
        }

        public enum ExploredQuadrant
        {
            LeftHalf,
            RightHalf,
            TopLeft,
            BottomLeft,
            TopRight,
            BottomRight,
        }

        [Serializable]
        public struct ExploredPartial
        {
            public Dictionary<ExploredQuadrant, ExploredState> statuses;
            public MapGrid map;
        }

        [Serializable]
        public class SerializedWrapper
        {
            public string TypeName;
            public string Payload;
            public string Hash;
            public string Version;
        }

        #region Serialization Settings

        public static JsonSerializerSettings GetJsonSerializerSettings()
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                NullValueHandling = NullValueHandling.Include,
            };
            settings.Converters.Add(new UnityObjectJsonConverter());
            settings.Converters.Add(new CharacterInstanceJsonConverter());
            settings.Converters.Add(new ObjectItemInstanceJsonConverter());
            return settings;
        }

        #endregion

        #region Utilities

        public static string DesignateInstanceType<T>() => typeof(T).FullName;

        #endregion
    }
}
