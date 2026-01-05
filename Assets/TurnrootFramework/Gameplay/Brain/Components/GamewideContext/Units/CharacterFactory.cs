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
            {
                return null;
            }

            if (template.IsUnique)
            {
                // Try to load existing unique character
                var existing = _persistence.RecallCharacter(template);
                if (existing != null)
                {
                    return existing;
                }

                // Create new but DO NOT persist here - caller decides when to save
                var newUnique = CharacterInstance.Create(template);
                return newUnique;
            }

            // Non-unique: always create a fresh instance
            return CharacterInstance.Create(template);
        }
    }
}
