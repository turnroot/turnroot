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

        public void PublishSkillTriggered(CharacterInstance character, Skill skill) =>
            OnSkillTriggered?.Invoke(character, skill);

        public void PublishSkillEquipped(CharacterInstance character, Skill skill) =>
            OnSkillEquipped?.Invoke(character, skill);

        public void PublishSkillUnequipped(CharacterInstance character, Skill skill) =>
            OnSkillUnequipped?.Invoke(character, skill);

        #endregion
    }
}
