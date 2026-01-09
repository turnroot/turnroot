using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Environment;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.PreBattle
{
    [RequireComponent(typeof(EnvironmentalConditions))]
    public class BattlePreparationObject : MonoBehaviour
    {
        public Brain.Brain Brain { get; private set; }

        [HideInInspector]
        public EnvironmentalConditions EnvironmentalConditions { get; private set; }

        [HideInInspector]
        public int MaxPlayerTeamUnits;

        [SerializeField, HideInInspector]
        private System.Collections.Generic.List<CharacterData> _requiredPlayerUnits = new();

        public System.Collections.Generic.List<CharacterData> RequiredPlayerUnits =>
            _requiredPlayerUnits;

        public OperationResult Initialize(Brain.Brain brain)
        {
            Brain = brain;
            EnvironmentalConditions = GetComponentInChildren<EnvironmentalConditions>(true);

            // Copy MaxPlayerTeamUnits and RequiredPlayerUnits from a BattleGameObject when available.
            if (brain?.battleBrain?.BattleObject != null)
            {
                MaxPlayerTeamUnits = brain.battleBrain.BattleObject.MaxPlayerTeamUnits;
                _requiredPlayerUnits =
                    brain.battleBrain.BattleObject.RequiredPlayerUnits
                    ?? new System.Collections.Generic.List<CharacterData>();
            }
            else
            {
                var parentBattleObject = GetComponentInParent<BattleGameObject>();
                if (parentBattleObject != null)
                {
                    MaxPlayerTeamUnits = parentBattleObject.MaxPlayerTeamUnits;
                    _requiredPlayerUnits =
                        parentBattleObject.RequiredPlayerUnits
                        ?? new System.Collections.Generic.List<CharacterData>();
                }
            }

            return EnvironmentalConditions == null
                ? OperationResult.Failure("EnvironmentalConditions not found")
                : OperationResult.SuccessResult();
        }
    }
}
