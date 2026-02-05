using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Gameplay.Maps;
using Turnroot.Serialization;
using Turnroot.Utilities;
using UnityEngine;

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

        internal static T TryExecute<T>(Func<T> action, T defaultValue, string errorMessage)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                TurnrootLogger.Log(
                    $"{errorMessage}: {ex.Message}",
                    TurnrootLogger.LogLevel.Warning
                );
                return defaultValue;
            }
        }

        internal static void TryExecute(Action action, string errorMessage)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                TurnrootLogger.Log(
                    $"{errorMessage}: {ex.Message}",
                    TurnrootLogger.LogLevel.Warning
                );
            }
        }

        #endregion
    }
}
