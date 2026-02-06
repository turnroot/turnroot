using System.Collections.Generic;
using Turnroot.Gameplay.Maps;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Partial class managing map exploration status tracking and persistence.
    /// </summary>
    public partial class GamewideContextBrain
    {
        #region Map Exploration Management
        public void RegisterMapExplorationPartial(
            GamewideContextBrainHelpers.ExploredPartial partial
        )
        {
            if (partial.map == null || string.IsNullOrEmpty(partial.map.MapName))
            {
                TurnrootLogger.Log(
                    "RegisterMapExplorationPartial: partial must have a valid map and MapName",
                    TurnrootLogger.LogLevel.Warning
                );
                return;
            }

            MapExplorationStatuses ??= new List<GamewideContextBrainHelpers.ExploredPartial>();

            var existingIndex = MapExplorationStatuses.FindIndex(p =>
                p.map != null && p.map.MapName == partial.map.MapName
            );

            if (existingIndex >= 0)
            {
                MapExplorationStatuses[existingIndex] = partial;
            }
            else
            {
                MapExplorationStatuses.Add(partial);
            }
        }

        public void SaveMapExplorationStatus()
        {
            foreach (var status in MapExplorationStatuses)
            {
                SaveMapExplorationStatus(status);
            }
        }

        public void SaveMapExplorationStatus(GamewideContextBrainHelpers.ExploredPartial partial)
        {
            if (partial.map == null || string.IsNullOrEmpty(partial.map.MapName))
            {
                TurnrootLogger.Log(
                    "SaveMapExplorationStatus: partial must have a valid map with MapName",
                    TurnrootLogger.LogLevel.Warning
                );
                return;
            }

            var encode = GamewideContextBrainHelpers.EncodeInstanceToString(this, partial);
            if (!encode.Success)
            {
                TurnrootLogger.Log(
                    $"Failed to encode exploration partial for map {partial.map.MapName}: {encode.Error}",
                    TurnrootLogger.LogLevel.Error
                );
                return;
            }

            var key = BuildExplorationPartialKey(partial.map.MapName);
            _ltm.Remember(key, encode.Value);
        }

        public void PopulateMapExplorationStatusesFromLtm()
        {
            var keys = _ltm.RecallKeysByPrefix(LtmKeys.ExploredPartial);
            if (keys == null)
            {
                return;
            }

            MapExplorationStatuses ??= new List<GamewideContextBrainHelpers.ExploredPartial>();

            foreach (var key in keys)
            {
                var encoded = _ltm.Recall(key);

                if (!string.IsNullOrEmpty(encoded))
                {
                    var decoded =
                        GamewideContextBrainHelpers.DecodeInstanceFromString<GamewideContextBrainHelpers.ExploredPartial>(
                            this,
                            encoded
                        );

                    if (decoded.Success)
                    {
                        MapExplorationStatuses.Add(decoded.Value);
                        continue;
                    }
                }

                var suffix =
                    key.Length > LtmKeys.ExploredPartial.Length + 1
                        ? key.Substring(LtmKeys.ExploredPartial.Length + 1)
                        : string.Empty;

                var fallbackPartial = new GamewideContextBrainHelpers.ExploredPartial
                {
                    statuses =
                        new Dictionary<
                            GamewideContextBrainHelpers.ExploredQuadrant,
                            GamewideContextBrainHelpers.ExploredState
                        >(),
                    map = string.IsNullOrEmpty(suffix) ? null : Resources.Load<MapGrid>(suffix),
                };

                MapExplorationStatuses.Add(fallbackPartial);
            }
        }

        private string BuildExplorationPartialKey(string mapId) =>
            $"{LtmKeys.ExploredPartial}.{mapId}";
        #endregion
    }
}
