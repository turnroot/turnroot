using Turnroot.Characters;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    public partial class BattleContext
    {
        #region Last Attacker Tracking

        public CharacterInstance GetLastAttacker(CharacterInstance target) =>
            target != null && _lastAttackerByTarget.TryGetValue(target.Id, out var a) ? a : null;

        public void RegisterLastAttacker(CharacterInstance target, CharacterInstance attacker)
        {
            if (target == null)
            {
                return;
            }

            if (attacker == null)
            {
                _lastAttackerByTarget.Remove(target.Id);
            }
            else
            {
                _lastAttackerByTarget[target.Id] = attacker;
            }
        }

        public void ClearLastAttackHistory() => _lastAttackerByTarget.Clear();

        #endregion
    }
}
