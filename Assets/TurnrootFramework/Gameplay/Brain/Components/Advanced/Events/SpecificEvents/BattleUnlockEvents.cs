using System;

namespace Turnroot.Gameplay.Brain
{
    public partial class Brain
    {
        #region Battle Unlock Events

        /// <summary>
        /// Fired when a battle is unlocked from a conversation or other gameplay trigger.
        /// The argument is the battle scene name configured on the unlock node.
        /// </summary>
        public event Action<string> OnBattleUnlocked;

        public void PublishBattleUnlocked(string battleSceneName)
        {
            if (string.IsNullOrWhiteSpace(battleSceneName))
            {
                return;
            }

            OnBattleUnlocked?.Invoke(battleSceneName);
        }

        #endregion
    }
}
