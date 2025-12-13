namespace Turnroot.Characters
{
    /// <summary>
    /// Handles skill management for a character instance.
    /// </summary>
    public partial class CharacterInstance
    {
        #region Skills

        /// <summary>
        /// Add a skill from a template.
        /// </summary>
        internal void AddSkill(Skill skillTemplate)
        {
            var skillInstance = new SkillInstance(skillTemplate);
            _skillInstances.Add(skillInstance);
        }

        /// <summary>
        /// Remove a skill instance.
        /// </summary>
        internal void RemoveSkill(SkillInstance skillInstance) =>
            _skillInstances.Remove(skillInstance);

        #endregion
    }
}
