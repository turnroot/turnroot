using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components.Battle;
using Turnroot.Gameplay.Combat;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Gameplay.Maps;
using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Turnroot.Gameplay.Brain
{
    public partial class BattleBrain : BrainComponent
    {
        #region Turn Management

        public void AdvanceBattleUnitOrPhase()
        {
            if (!turnRotisserie.AdvanceToNextUnitOrPhase())
            {
                "BattleBrain: Failed to advance to the next unit or faction phase!".LogError();
            }
        }

        #endregion
    }
}