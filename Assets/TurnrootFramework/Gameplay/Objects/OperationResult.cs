using UnityEngine;

namespace Turnroot.Gameplay.Objects
{
    /// <summary>
    /// Represents the result of an item operation.
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
        /// Creates a successful operation result.
        /// </summary>
        public static OperationResult SuccessResult() => new() { Success = true };

        /// <summary>
        /// Creates a failed operation result with an error message.
        /// </summary>
        public static OperationResult Failure(string errorMessage) =>
            new() { Success = false, ErrorMessage = errorMessage };
    }
}
