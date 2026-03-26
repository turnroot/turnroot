using System.Collections.Generic;
using TMPro;
using Turnroot.Characters;
using Turnroot.Gameplay.NonCombatScenes.Hub.Abstract;
using Turnroot.Gameplay.Objects;
using Turnroot.GameSettings;
using Turnroot.UI;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using Turnroot.Utilities.Ui;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Docks
{
    [RequireComponent(typeof(DockShip))]
    public class DockShipUi : MonoBehaviour
    {
        private DockShip dockShip;

        public UIFade DockShipUiFade;

        private void Awake()
        {
            dockShip = GetComponent<DockShip>();
            if (dockShip == null)
            {
                $"DockShipUi on '{name}' could not find DockShip component.".LogError();
            }
        }

        public void RefreshDockShipDisplay()
        {
            if (dockShip == null)
            {
                return;
            }
        }
    }
}
