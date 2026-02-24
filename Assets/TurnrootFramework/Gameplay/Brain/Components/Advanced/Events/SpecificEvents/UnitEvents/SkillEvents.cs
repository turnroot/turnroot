using System;
using Turnroot.Characters;
using Turnroot.Skills;

namespace Turnroot.Gameplay.Brain
{
    public partial class Brain
    {
        #region Skill Events

        public event Action<CharacterInstance, Skill> OnSkillTriggered;
        public event Action<CharacterInstance, Skill> OnSkillEquipped;
        public event Action<CharacterInstance, Skill> OnSkillUnequipped;

        // Fired when a skill is identified as activating at the start of a battle (before the graph
        // is actually executed). This allows listeners to react to battle‑start effects separately
        // from generic skill activations.
        public event Action<CharacterInstance, Skill> OnBattleStartSkill;

        public void PublishSkillTriggered(CharacterInstance character, Skill skill) =>
            OnSkillTriggered?.Invoke(character, skill);

        public void PublishSkillEquipped(CharacterInstance character, Skill skill) =>
            OnSkillEquipped?.Invoke(character, skill);

        public void PublishSkillUnequipped(CharacterInstance character, Skill skill) =>
            OnSkillUnequipped?.Invoke(character, skill);

        public void PublishBattleStartSkill(CharacterInstance character, Skill skill) =>
            OnBattleStartSkill?.Invoke(character, skill);

        #endregion
    }
}
