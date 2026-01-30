using TMPro;
using Turnroot.Gameplay.Brain;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.UI.Components
{
    public enum BattlePreTurnPhase
    {
        PlayerTurn,
        EnemyTurn,
        ThirdPartyTurn,
    }

    [RequireComponent(typeof(PreparationObjectResolver))]
    public class BattlePreTurn : MonoBehaviour
    {
        public TextMeshProUGUI TurnText;
        private int TurnNumber;
        private BattlePreTurnPhase Phase;

        [HideInInspector]
        public BattleBrain battleBrain;

        public void Initialize(BattleBrain brain) => battleBrain = brain;

        public string GetTurnDescription()
        {
            return Phase switch
            {
                BattlePreTurnPhase.PlayerTurn => $"Player Turn {TurnNumber}",
                BattlePreTurnPhase.EnemyTurn => $"Enemy Turn {TurnNumber}",
                BattlePreTurnPhase.ThirdPartyTurn => $"Third Party Turn {TurnNumber}",
                _ => $"Turn {TurnNumber}",
            };
        }

        public void UpdateTurnText()
        {
            if (TurnText != null)
            {
                TurnText.text = GetTurnDescription();
            }
            else
            {
                TurnrootLogger.Log(
                    "BattlePreTurn: TurnText component is not assigned.",
                    TurnrootLogger.LogLevel.Warning
                );
            }
        }
    }
}
