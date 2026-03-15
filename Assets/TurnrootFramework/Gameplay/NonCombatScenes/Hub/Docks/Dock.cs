using System.Collections.Generic;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Docks
{
    public class Dock : MonoBehaviour
    {
        public DockShip[] AllShips;

        /// <summary>
        /// Maximum number of ships allowed per dock side (left/right).
        /// If more ships are docked on a side than this, the oldest docked ships are sent back to sea.
        /// </summary>
        public int MaxDockedShipsPerSide = 3;

        private readonly System.Collections.Generic.List<DockShip> _leftDockedShips = new();
        private readonly System.Collections.Generic.List<DockShip> _rightDockedShips = new();

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
            EnforceSideCapacity(_leftDockedShips, MaxDockedShipsPerSide);
            EnforceSideCapacity(_rightDockedShips, MaxDockedShipsPerSide);

            // If total docked ships still exceeds twice the side max (rare, but possible if MaxDockedShipsPerSide is changed at runtime), enforce overall limit.
            int totalCapacity = MaxDockedShipsPerSide * 2;
            int totalDocked = _leftDockedShips.Count + _rightDockedShips.Count;
            if (totalDocked <= totalCapacity)
            {
                return;
            }

            var allDocked = new System.Collections.Generic.List<DockShip>(_leftDockedShips);
            allDocked.AddRange(_rightDockedShips);
            allDocked.Sort((a, b) => b.CurrentDockedTime.CompareTo(a.CurrentDockedTime));

            for (int i = totalCapacity; i < allDocked.Count; i++)
            {
                allDocked[i].ForceSendToSea();
            }

            // Rebuild lists after forcing excess ships to sea.
            RefreshDockLists();
        }

        private void EnforceSideCapacity(
            System.Collections.Generic.List<DockShip> ships,
            int capacity
        )
        {
            if (capacity <= 0 || ships.Count <= capacity)
            {
                return;
            }

            // Keep newest-docked ships, send the oldest ones out.
            ships.Sort((a, b) => b.CurrentDockedTime.CompareTo(a.CurrentDockedTime));
            for (int i = capacity; i < ships.Count; i++)
            {
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
                dockedStatuses[i] = new DockShipStatus
                {
                    ShipName = AllShips[i].ShipName,
                    IsDocked = AllShips[i].IsDocked,
                };
            }

            return dockedStatuses;
        }
    }
}
