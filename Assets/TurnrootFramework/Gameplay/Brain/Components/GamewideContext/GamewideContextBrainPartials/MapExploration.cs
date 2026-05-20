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
        public void RegisterMapExplorationStatus(GamewideContextBrainHelpers.ExploredStatus status)
        {
            if (status.map == null || string.IsNullOrEmpty(status.map.MapName))
            {
                "RegisterMapExplorationStatus: status must have a valid map and MapName".LogWarning();
                return;
            }

            MapExplorationStatuses ??= new List<GamewideContextBrainHelpers.ExploredStatus>();

            var existingIndex = MapExplorationStatuses.FindIndex(s =>
                s.map != null && s.map.MapName == status.map.MapName
            );

            if (existingIndex >= 0)
            {
                MapExplorationStatuses[existingIndex] = status;
            }
            else
            {
                MapExplorationStatuses.Add(status);
            }
        }

        public void SaveMapExplorationStatus()
        {
            foreach (var status in MapExplorationStatuses)
            {
                SaveMapExplorationStatus(status);
            }
        }

        public void SaveMapExplorationStatus(GamewideContextBrainHelpers.ExploredStatus status)
        {
            if (status.map == null || string.IsNullOrEmpty(status.map.MapName))
            {
                "SaveMapExplorationStatus: status must have a valid map with MapName".LogWarning();
                return;
            }

            var encode = GamewideContextBrainHelpers.EncodeInstanceToString(this, status);
            if (!encode.Success)
            {
                $"Failed to encode exploration status for map {status.map.MapName}: {encode.Error}".LogError();
                return;
            }

            var key = BuildExplorationStatusKey(status.map.MapName);
            _ltm.Remember(key, encode.Value);
        }

        public void PopulateMapExplorationStatusesFromLtm()
        {
            var keys = _ltm.RecallKeysByPrefix(LtmKeys.ExploredPartial);
            if (keys == null)
            {
                return;
            }

            MapExplorationStatuses ??= new List<GamewideContextBrainHelpers.ExploredStatus>();

            foreach (var key in keys)
            {
                var encoded = _ltm.Recall(key);

                if (!string.IsNullOrEmpty(encoded))
                {
                    var decoded =
                        GamewideContextBrainHelpers.DecodeInstanceFromString<GamewideContextBrainHelpers.ExploredStatus>(
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

                var fallbackStatus = new GamewideContextBrainHelpers.ExploredStatus
                {
                    map = string.IsNullOrEmpty(suffix) ? null : Resources.Load<MapGrid>(suffix),
                };

                MapExplorationStatuses.Add(fallbackStatus);
            }
        }

        private string BuildExplorationStatusKey(string mapId) =>
            $"{LtmKeys.ExploredPartial}.{mapId}";
        #endregion
    }
}
