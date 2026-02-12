using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Utilities;

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
                var existing = _persistence.RecallCharacter(template);
                if (existing != null)
                {
                    EnsureDefaultClassAndPersist(existing);
                    return existing;
                }

                var newUnique = CharacterInstance.Create(template);
                EnsureDefaultClassAndPersist(newUnique);
                return newUnique;
            }

            var created = CharacterInstance.Create(template);
            EnsureDefaultClassAndPersist(created);
            return created;
        }

        private void EnsureDefaultClassAndPersist(CharacterInstance instance)
        {
            if (instance == null)
            {
                return;
            }

            if (instance.CurrentClass != null && instance.CurrentClass.ClassData != null)
            {
                return;
            }

            var classToApply =
                instance.CharacterTemplate.StartingClass
                ?? GameSettings.GameplayGeneralSettings.Instance.GetDefaultStartingClass();

            if (classToApply != null)
            {
                var res = instance.ChangeClass(classToApply, applyClassChangeBonuses: false);
                if (res.Success)
                {
                    _persistence.SaveCharacter(instance, updateIndex: false);
                }
                else
                {
                    $"CharacterFactory: Failed to assign default class for {instance.Id}: {res.ErrorMessage}".LogWarning(
                        "CharacterFactory"
                    );
                }
            }
        }
    }
}
