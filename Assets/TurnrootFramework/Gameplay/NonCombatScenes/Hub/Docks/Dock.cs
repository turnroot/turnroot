using System.Collections.Generic;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Docks
{
    public class Dock : MonoBehaviour
    {
        public DockShip[] AllShips;

        [Tooltip(
            "Maximum number of ships allowed per dock side (left/right). If more ships are docked on a side than this, the oldest docked ships are sent back to sea."
        )]
        public int MaxDockedShipsPerSide = 3;

        private readonly List<DockShip> _leftDockedShips = new();
        private readonly List<DockShip> _rightDockedShips = new();

        public IReadOnlyList<DockShip> LeftDockedShips => _leftDockedShips;
        public IReadOnlyList<DockShip> RightDockedShips => _rightDockedShips;

        public void UpdateDailyVoyageStatuses()
        {
            if (AllShips == null)
            {
                return;
            }

            for (int i = 0; i < AllShips.Length; i++)
            {
                if (AllShips[i] == null)
                {
                    continue;
                }

                AllShips[i].CheckIsDockedAndUpdateVoyageStatusByOneDay();
            }

            RefreshDockLists();
            EnforceDockCapacity();
        }

        /// <summary>
        /// Call on hub load (even when daily updates were already processed) to restore
        /// runtime dock lists from each ship's persisted IsDocked state and re-enforce capacity.
        /// </summary>
        public void EnforceCapacityOnLoad()
        {
            RefreshDockLists();
            EnforceDockCapacity();
        }

        private void RefreshDockLists()
        {
            _leftDockedShips.Clear();
            _rightDockedShips.Clear();

            if (AllShips == null)
            {
                return;
            }

            for (int i = 0; i < AllShips.Length; i++)
            {
                var ship = AllShips[i];
                if (ship == null || !ship.IsDocked)
                {
                    continue;
                }

                if (ship.Side == DockShip.DockSide.Left)
                {
                    _leftDockedShips.Add(ship);
                }
                else
                {
                    _rightDockedShips.Add(ship);
                }
            }
        }

        private void EnforceDockCapacity()
        {
            EnforceSideCapacity(_leftDockedShips, MaxDockedShipsPerSide, "Left");
            // Refresh lists so the total check below sees the up-to-date state after
            // per-side evictions — otherwise stale counts trigger a redundant second pass.
            RefreshDockLists();

            EnforceSideCapacity(_rightDockedShips, MaxDockedShipsPerSide, "Right");
            RefreshDockLists();

            int totalCapacity = MaxDockedShipsPerSide * 2;
            int totalDocked = _leftDockedShips.Count + _rightDockedShips.Count;
            if (totalDocked <= totalCapacity)
            {
                return;
            }

            var allDocked = new List<DockShip>(_leftDockedShips);
            allDocked.AddRange(_rightDockedShips);
            // Sort ascending: lowest CurrentDockedTime (newest arrivals) first — keep those,
            // evict ships that have been docked the longest.
            allDocked.Sort((a, b) => a.CurrentDockedTime.CompareTo(b.CurrentDockedTime));

            for (int i = totalCapacity; i < allDocked.Count; i++)
            {
                $"Dock: total capacity exceeded — sending '{allDocked[i].ShipName}' to sea.".LogInfo();
                allDocked[i].ForceSendToSea();
            }

            RefreshDockLists();
        }

        private void EnforceSideCapacity(List<DockShip> ships, int capacity, string side)
        {
            if (capacity <= 0 || ships.Count <= capacity)
            {
                return;
            }

            // Sort ascending by CurrentDockedTime: newest arrivals (lowest time) first.
            // Keep the first 'capacity' ships; evict those that have been docked the longest.
            ships.Sort((a, b) => a.CurrentDockedTime.CompareTo(b.CurrentDockedTime));
            for (int i = capacity; i < ships.Count; i++)
            {
                $"Dock: {side} side over capacity — sending '{ships[i].ShipName}' (dockedTime={ships[i].CurrentDockedTime}) to sea.".LogInfo();
                ships[i].ForceSendToSea();
            }
        }

        public DockShipStatus[] PublishDockedShipStatuses()
        {
            RefreshDockLists();

            if (AllShips == null)
            {
                return System.Array.Empty<DockShipStatus>();
            }

            DockShipStatus[] dockedStatuses = new DockShipStatus[AllShips.Length];
            for (int i = 0; i < AllShips.Length; i++)
            {
                if (AllShips[i] == null)
                {
                    continue;
                }

                dockedStatuses[i] = new DockShipStatus
                {
                    ShipName = AllShips[i].ShipName,
                    IsDocked = AllShips[i].IsDocked,
                };
            }

            return dockedStatuses;
        }

        public void RefreshShipsForNewDay(GameDate currentDay)
        {
            foreach (var ship in AllShips)
            {
                if (ship == null)
                {
                    $"Warning: Null ship reference in dock when refreshing for new day. Skipping.".LogWarning();
                    continue;
                }
                if (ship.IsDocked)
                {
                    ship.RefreshShipForNewDay(currentDay);
                }
            }
        }
    }
}
