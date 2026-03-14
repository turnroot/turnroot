using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Docks
{
    public class Dock : MonoBehaviour
    {
        public DockShip[] AllShips;

        [HideInInspector]
        public DockShip[] AllShipsDocked;

        public int MaxNumberOfDockedShips = 3;

        private void Start()
        {
            UpdateDailyVoyageStatuses();
        }

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
        }

        public DockShipStatus[] PublishDockedShipStatuses()
        {
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
                if (AllShips[i].IsDocked)
                {
                    AllShips[i].Ship.SetActive(true);
                }
                else
                {
                    AllShips[i].Ship.SetActive(false);
                }
            }
            return dockedStatuses;
        }
    }
}
