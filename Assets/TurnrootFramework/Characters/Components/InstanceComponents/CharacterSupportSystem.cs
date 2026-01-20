using Turnroot.Characters.Components.Support;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Characters
{
    /// <summary>
    /// Handles support relationships between characters.
    /// </summary>
    public partial class CharacterInstance
    {
        #region Support Relationships
        public SupportRelationshipInstance GetSupportRelationship(CharacterData character) =>
            _supportRelationships.Find(s => s.Character == character);

        public OperationResult AddSupportRelationship(SupportRelationship template)
        {
            bool ok = ValidationHelper.ValidateNotNull(
                "CharacterInstance.AddSupportRelationship",
                out var missing,
                (template, nameof(template)),
                (template?.Character, "template.Character")
            );

            if (!ok)
            {
                return OperationResult.Failure(
                    $"AddSupportRelationship failed: missing {string.Join(", ", missing)}"
                );
            }

            // Validate that the support relationship is not with the same character
            if (template.Character == _characterTemplate)
            {
                return OperationResult.Failure(
                    $"Cannot add support relationship with the same character ({template.Character.name})"
                );
            }

            // Check if relationship already exists
            if (GetSupportRelationship(template.Character) == null)
            {
                _supportRelationships.Add(new SupportRelationshipInstance(template));
            }

            return OperationResult.Successful();
        }

        internal OperationResult IncreaseSupport(CharacterData character, int amount)
        {
            bool ok = ValidationHelper.ValidateNotNull(
                "CharacterInstance.IncreaseSupport",
                out var missing,
                (character, nameof(character))
            );

            if (!ok)
            {
                return OperationResult.Failure(
                    $"IncreaseSupport failed: missing {string.Join(", ", missing)}"
                );
            }

            var relationship = GetSupportRelationship(character);
            if (relationship != null)
            {
                relationship.Increase(amount);
                return OperationResult.Successful();
            }

            TurnrootLogger.Log(
                $"Support relationship with {character.name} does not exist. Creating new relationship."
            );
            var res = AddSupportRelationship(new SupportRelationship { Character = character });
            if (!res.Success)
            {
                return res;
            }
            GetSupportRelationship(character)?.Increase(amount);
            return OperationResult.Successful();
        }

        public OperationResult RemoveSupportRelationship(CharacterData character)
        {
            bool ok = ValidationHelper.ValidateNotNull(
                "CharacterInstance.RemoveSupportRelationship",
                out var missing,
                (character, nameof(character))
            );

            if (!ok)
            {
                return OperationResult.Failure(
                    $"RemoveSupportRelationship failed: missing {string.Join(", ", missing)}"
                );
            }

            _ = _supportRelationships.RemoveAll(s => s.Character == character);
            return OperationResult.Successful();
        }

        /* ---------------------- Recruitment helpers ---------------------- */
        public bool IsCharacterRecruitable(CharacterData character)
        {
            if (character == null)
            {
                return false;
            }

            var rel = GetSupportRelationship(character);
            return rel != null ? rel.GetIsRecruitable() : character.IsRecruitable;
        }

        public OperationResult SetCharacterRecruitable(CharacterData character, bool isRecruitable)
        {
            bool ok = ValidationHelper.ValidateNotNull(
                "CharacterInstance.SetCharacterRecruitable",
                out var missing,
                (character, nameof(character))
            );

            if (!ok)
            {
                return OperationResult.Failure(
                    $"SetCharacterRecruitable failed: missing {string.Join(", ", missing)}"
                );
            }

            var rel = GetSupportRelationship(character);
            if (rel == null)
            {
                var res = AddSupportRelationship(new SupportRelationship { Character = character });
                if (!res.Success)
                {
                    return res;
                }

                rel = GetSupportRelationship(character);
            }
            rel.SetIsRecruitableOverride(isRecruitable);
            return OperationResult.Successful();
        }

        public float GetCharacterRecruitmentChance(CharacterData character)
        {
            if (character == null)
            {
                return 0f;
            }

            var rel = GetSupportRelationship(character);
            return rel != null ? rel.GetRecruitmentChance() : character.RecruitmentChance;
        }

        public OperationResult SetCharacterRecruitmentChance(CharacterData character, float chance)
        {
            bool ok = ValidationHelper.ValidateNotNull(
                "CharacterInstance.SetCharacterRecruitmentChance",
                out var missing,
                (character, nameof(character))
            );

            if (!ok)
            {
                return OperationResult.Failure(
                    $"SetCharacterRecruitmentChance failed: missing {string.Join(", ", missing)}"
                );
            }

            var rel = GetSupportRelationship(character);
            if (rel == null)
            {
                var res = AddSupportRelationship(new SupportRelationship { Character = character });
                if (!res.Success)
                {
                    return res;
                }

                rel = GetSupportRelationship(character);
            }
            rel.SetRecruitmentChanceOverride(Mathf.Clamp(chance, 0f, 100f));
            return OperationResult.Successful();
        }

        public float GetCharacterRecruitmentChanceIncreasePerConversation(CharacterData character)
        {
            if (character == null)
            {
                return 0f;
            }

            var rel = GetSupportRelationship(character);
            return rel != null
                ? rel.GetRecruitmentChanceIncreasePerConversation()
                : character.RecruitmentChanceIncreasePerConversation;
        }

        public OperationResult SetCharacterRecruitmentChanceIncreasePerConversation(
            CharacterData character,
            float increase
        )
        {
            bool ok = ValidationHelper.ValidateNotNull(
                "CharacterInstance.SetCharacterRecruitmentChanceIncreasePerConversation",
                out var missing,
                (character, nameof(character))
            );

            if (!ok)
            {
                return OperationResult.Failure(
                    $"SetCharacterRecruitmentChanceIncreasePerConversation failed: missing {string.Join(", ", missing)}"
                );
            }

            var rel = GetSupportRelationship(character);
            if (rel == null)
            {
                var res = AddSupportRelationship(new SupportRelationship { Character = character });
                if (!res.Success)
                {
                    return res;
                }

                rel = GetSupportRelationship(character);
            }
            rel.SetRecruitmentChanceIncreasePerConversationOverride(
                Mathf.Clamp(increase, 0f, 100f)
            );
            return OperationResult.Successful();
        }

        public bool GetCharacterRequiresMinSupportLevel(CharacterData character)
        {
            if (character == null)
            {
                return false;
            }

            var rel = GetSupportRelationship(character);
            return rel != null
                ? rel.GetRequiresMinSupportLevel()
                : character.RequiresMinSupportLevel;
        }

        public OperationResult SetCharacterRequiresMinSupportLevel(
            CharacterData character,
            bool requires
        )
        {
            bool ok = ValidationHelper.ValidateNotNull(
                "CharacterInstance.SetCharacterRequiresMinSupportLevel",
                out var missing,
                (character, nameof(character))
            );

            if (!ok)
            {
                return OperationResult.Failure(
                    $"SetCharacterRequiresMinSupportLevel failed: missing {string.Join(", ", missing)}"
                );
            }

            var rel = GetSupportRelationship(character);
            if (rel == null)
            {
                var res = AddSupportRelationship(new SupportRelationship { Character = character });
                if (!res.Success)
                {
                    return res;
                }

                rel = GetSupportRelationship(character);
            }
            rel.SetRequiresMinSupportLevelOverride(requires);
            return OperationResult.Successful();
        }

        public OperationResult ClearRecruitmentOverrides(CharacterData character)
        {
            bool ok = ValidationHelper.ValidateNotNull(
                "CharacterInstance.ClearRecruitmentOverrides",
                out var missing,
                (character, nameof(character))
            );

            if (!ok)
            {
                return OperationResult.Failure(
                    $"ClearRecruitmentOverrides failed: missing {string.Join(", ", missing)}"
                );
            }

            var rel = GetSupportRelationship(character);
            if (rel == null)
            {
                return OperationResult.Failure("No support relationship found for character.");
            }

            rel.ClearIsRecruitableOverride();
            rel.ClearRecruitmentChanceOverride();
            rel.ClearRecruitmentChanceIncreasePerConversationOverride();
            rel.ClearRequiresMinSupportLevelOverride();
            return OperationResult.Successful();
        }

        #endregion
    }
}
