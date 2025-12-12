using Turnroot.Services;
using UnityEngine;

namespace Turnroot.Gameplay.Objects
{
    /// <summary>
    /// Represents the result of an item operation.
    /// Bridges to ValidationResult pattern for consistency.
    /// </summary>
    public struct OperationResult
    {
        /// <summary>
        /// Whether the operation succeeded.
        /// </summary>
        public bool Success;

        /// <summary>
        /// Error message if the operation failed, null or empty if successful.
        /// </summary>
        public string ErrorMessage;

        /// <summary>
        /// Optional validation result that caused this operation to fail.
        /// </summary>
        public ValidationResult Validation { get; private set; }

        /// <summary>
        /// Creates a successful operation result.
        /// </summary>
        public static OperationResult SuccessResult() =>
            new() { Success = true, Validation = ValidationResult.Success() };

        /// <summary>
        /// Creates a failed operation result with an error message.
        /// </summary>
        public static OperationResult Failure(string errorMessage) =>
            new()
            {
                Success = false,
                ErrorMessage = errorMessage,
                Validation = ValidationResult.Failure(errorMessage),
            };

        /// <summary>
        /// Creates a failed operation result from a ValidationResult.
        /// </summary>
        public static OperationResult FromValidation(ValidationResult validation) =>
            validation.IsValid
                ? SuccessResult()
                : new()
                {
                    Success = false,
                    ErrorMessage = validation.ErrorMessage,
                    Validation = validation,
                };

        /// <summary>
        /// Implicitly converts ValidationResult to OperationResult.
        /// </summary>
        public static implicit operator OperationResult(ValidationResult validation) =>
            FromValidation(validation);
    }
}
