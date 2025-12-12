using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Characters.CharacterClass;
using UnityEngine;

namespace Turnroot.Services
{
    /// <summary>
    /// Centralized validation service for game entities and operations.
    /// Provides consistent validation logic across the codebase.
    /// </summary>
    public class ValidationService
    {
        private static ValidationService _instance;
        public static ValidationService Instance => _instance ??= new ValidationService();

        /// <summary>
        /// Validates a character instance.
        /// </summary>
        public ValidationResult ValidateCharacter(
            CharacterInstance character,
            string context = null
        )
        {
            if (character == null)
            {
                return ValidationResult.Failure($"{context ?? "Validation"}: Character is null");
            }

            if (character.CharacterTemplate == null)
            {
                return ValidationResult.Failure(
                    $"{context ?? "Validation"}: Character template is null"
                );
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates a class data instance.
        /// </summary>
        public ValidationResult ValidateClass(CharacterClassData classData, string context = null)
        {
            if (classData == null)
            {
                return ValidationResult.Failure($"{context ?? "Validation"}: Class data is null");
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates character and class together.
        /// </summary>
        public ValidationResult ValidateCharacterAndClass(
            CharacterInstance character,
            CharacterClassData classData,
            string context = null
        )
        {
            var characterResult = ValidateCharacter(character, context);
            if (!characterResult.IsValid)
            {
                return characterResult;
            }

            var classResult = ValidateClass(classData, context);
            if (!classResult.IsValid)
            {
                return classResult;
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates a collection is not null or empty.
        /// </summary>
        public ValidationResult ValidateCollection<T>(
            ICollection<T> collection,
            string collectionName,
            string context = null
        )
        {
            if (collection == null)
            {
                return ValidationResult.Failure(
                    $"{context ?? "Validation"}: {collectionName} is null"
                );
            }

            if (collection.Count == 0)
            {
                return ValidationResult.Failure(
                    $"{context ?? "Validation"}: {collectionName} is empty"
                );
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates that an object reference is not null.
        /// </summary>
        public ValidationResult ValidateNotNull(
            object obj,
            string objectName,
            string context = null
        )
        {
            if (obj == null)
            {
                return ValidationResult.Failure($"{context ?? "Validation"}: {objectName} is null");
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates that a string is not null or empty.
        /// </summary>
        public ValidationResult ValidateString(string str, string stringName, string context = null)
        {
            if (string.IsNullOrEmpty(str))
            {
                return ValidationResult.Failure(
                    $"{context ?? "Validation"}: {stringName} is null or empty"
                );
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates multiple conditions at once.
        /// </summary>
        public ValidationResult ValidateAll(params ValidationResult[] results)
        {
            foreach (var result in results)
            {
                if (!result.IsValid)
                {
                    return result;
                }
            }

            return ValidationResult.Success();
        }
    }

    /// <summary>
    /// Result of a validation operation.
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; }
        public string ErrorMessage { get; }

        private ValidationResult(bool isValid, string errorMessage = null)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }

        public static ValidationResult Success() => new ValidationResult(true);

        public static ValidationResult Failure(string errorMessage)
        {
            if (!string.IsNullOrEmpty(errorMessage))
            {
                Debug.LogWarning(errorMessage);
            }
            return new ValidationResult(false, errorMessage);
        }

        public void LogIfInvalid()
        {
            if (!IsValid && !string.IsNullOrEmpty(ErrorMessage))
            {
                Debug.LogWarning(ErrorMessage);
            }
        }
    }
}
