using TMPro;
using Turnroot.Gameplay.Brain;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.UI.Components
{
    /// <summary>
    /// Defines the different phases of a battle turn (player, enemy, or third party).
    /// </summary>
    public enum BattlePreTurnPhase
    {
        PlayerTurn,
        EnemyTurn,
        ThirdPartyTurn,
    }

    /// <summary>
    /// Displays and manages the turn indicator UI showing which faction's turn it is in battle.
    /// </summary>
    [RequireComponent(typeof(PreparationObjectResolver))]
    public class BattlePreTurn : MonoBehaviour
    {
        public TextMeshProUGUI TurnText;
        private int TurnNumber;
        private BattlePreTurnPhase Phase;

        [HideInInspector]
        public BattleBrain battleBrain;

        private void OnEnable()
        {
            if (battleBrain?.Brain != null)
            {
                // Subscribe to PHASE changes, not individual unit turns
                battleBrain.Brain.OnPlayerTurnStarted += HandlePlayerPhaseStarted;
                battleBrain.Brain.OnEnemyTurnStarted += HandleEnemyPhaseStarted;
                battleBrain.Brain.OnThirdPartyTurnStarted += HandleThirdPartyPhaseStarted;
                battleBrain.Brain.OnTurnBegin += HandleTurnBegin;
            }
        }

        private void OnDisable()
        {
            if (battleBrain?.Brain != null)
            {
                battleBrain.Brain.OnPlayerTurnStarted -= HandlePlayerPhaseStarted;
                battleBrain.Brain.OnEnemyTurnStarted -= HandleEnemyPhaseStarted;
                battleBrain.Brain.OnThirdPartyTurnStarted -= HandleThirdPartyPhaseStarted;
                battleBrain.Brain.OnTurnBegin -= HandleTurnBegin;
            }
        }

        private void HandlePlayerPhaseStarted(Characters.CharacterInstance _)
        {
            // OnPlayerTurnStarted fires for EACH unit, but we only want to update on the FIRST one
            // (the phase change). Check if we're transitioning FROM a different phase.
            if (Phase != BattlePreTurnPhase.PlayerTurn)
            {
                Phase = BattlePreTurnPhase.PlayerTurn;
                UpdateTurnText();
            }
        }

        private void HandleEnemyPhaseStarted()
        {
            Phase = BattlePreTurnPhase.EnemyTurn;
            UpdateTurnText();
        }

        private void HandleThirdPartyPhaseStarted()
        {
            Phase = BattlePreTurnPhase.ThirdPartyTurn;
            UpdateTurnText();
        }

        private void HandleTurnBegin() => TurnNumber++;

        public void Initialize(BattleBrain brain)
        {
            battleBrain = brain;
            TurnNumber = 1; // Start at turn 1, not 0
            Phase = BattlePreTurnPhase.PlayerTurn;
            UpdateTurnText();
        }

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
                "BattlePreTurn: TurnText component is not assigned.".LogWarning();
            }
        }
    }
}

