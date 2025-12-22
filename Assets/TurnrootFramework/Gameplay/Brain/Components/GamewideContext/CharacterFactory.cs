using Turnroot.Characters;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Handles character instance creation.
    /// Single responsibility: create characters from templates.
    /// </summary>
    public class CharacterFactory
    {
        private readonly CharacterPersistence _persistence;

        public CharacterFactory(LongTermMemory ltm)
        {
            _persistence = new CharacterPersistence(ltm.GetComponent<Brain>());
        }

        public CharacterInstance CreateOrRecall(CharacterData template)
        {
            if (template == null)
                return null;

            if (template.IsUnique)
            {
                // Try to load existing unique character
                var existing = _persistence.RecallCharacter(template);
                if (existing != null)
                    return existing;

                // Create new and save
                var newUnique = CharacterInstance.Create(template);
                if (newUnique != null)
                {
                    _persistence.SaveCharacter(newUnique, updateIndex: true);
                }
                return newUnique;
            }

            // Non-unique: always create fresh
            return CharacterInstance.Create(template);
        }
    }
}
