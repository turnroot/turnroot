using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Gameplay.Maps;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public static partial class GamewideContextBrainHelpers
    {
        public enum QuadrantExploredState
        {
            NotExplored,
            PartiallyExplored,
            FullyExplored,
        }

        public enum MapQuadrant
        {
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight,
        }

        [Serializable]
        public struct ExploreStatusSprites
        {
            public Sprite TopLeftExploredSprite;
            public Sprite TopLeftUnexploredSprite;
            public Sprite BottomLeftExploredSprite;
            public Sprite BottomLeftUnexploredSprite;
            public Sprite TopRightExploredSprite;
            public Sprite TopRightUnexploredSprite;
            public Sprite BottomRightExploredSprite;
            public Sprite BottomRightUnexploredSprite;
        }

        [Serializable]
        public struct ExploredStatus
        {
            public QuadrantExploredState TopLeft;
            public QuadrantExploredState BottomLeft;
            public QuadrantExploredState TopRight;
            public QuadrantExploredState BottomRight;
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
