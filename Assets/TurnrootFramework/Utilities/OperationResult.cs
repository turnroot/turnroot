using System;
using Turnroot.Services;

namespace Turnroot.Utilities
{
    /// <summary>
    /// Represents the result of an operation that does not return a value.
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
        /// Optional exception that caused this operation to fail.
        /// </summary>
        public Exception Exception { get; private set; }

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
        /// Creates a failed operation result with an exception.
        /// </summary>
        public static OperationResult Failure(Exception exception) =>
            new()
            {
                Success = false,
                ErrorMessage = exception?.Message ?? "Unknown error",
                Exception = exception,
                Validation = ValidationResult.Failure(exception?.Message ?? "Unknown error"),
            };

        /// <summary>
        /// Creates a failed operation result with both message and exception.
        /// </summary>
        public static OperationResult Failure(string errorMessage, Exception exception) =>
            new()
            {
                Success = false,
                ErrorMessage = errorMessage,
                Exception = exception,
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

    /// <summary>
    /// Represents the result of an operation that returns a value of type T.
    /// Use this pattern throughout the codebase for consistent error handling.
    /// </summary>
    /// <typeparam name="T">The type of the value returned on success.</typeparam>
    public struct OperationResult<T>
    {
        /// <summary>
        /// Whether the operation succeeded.
        /// </summary>
        public bool Success { get; private set; }

        /// <summary>
        /// The value returned by the operation. Only valid if Success is true.
        /// </summary>
        public T Value { get; private set; }

        /// <summary>
        /// Error message if the operation failed, null or empty if successful.
        /// </summary>
        public string Error { get; private set; }

        /// <summary>
        /// Optional exception that caused this operation to fail.
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// Creates a successful operation result with a value.
        /// </summary>
        public static OperationResult<T> SuccessResult(T value) =>
            new() { Success = true, Value = value };

        /// <summary>
        /// Creates a failed operation result with an error message.
        /// </summary>
        public static OperationResult<T> Failure(string errorMessage) =>
            new()
            {
                Success = false,
                Error = errorMessage,
                Value = default,
            };

        /// <summary>
        /// Creates a failed operation result with an exception.
        /// </summary>
        public static OperationResult<T> Failure(Exception exception) =>
            new()
            {
                Success = false,
                Error = exception?.Message ?? "Unknown error",
                Exception = exception,
                Value = default,
            };

        /// <summary>
        /// Creates a failed operation result with both message and exception.
        /// </summary>
        public static OperationResult<T> Failure(string errorMessage, Exception exception) =>
            new()
            {
                Success = false,
                Error = errorMessage,
                Exception = exception,
                Value = default,
            };

        /// <summary>
        /// Implicitly converts a value to a successful OperationResult.
        /// </summary>
        public static implicit operator OperationResult<T>(T value) => SuccessResult(value);

        /// <summary>
        /// Converts this generic result to a non-generic OperationResult.
        /// Useful when you need to discard the value but keep success/error info.
        /// </summary>
        public OperationResult ToNonGeneric() =>
            Success ? OperationResult.SuccessResult() : OperationResult.Failure(Error, Exception);

        /// <summary>
        /// Gets the value if successful, otherwise returns the default value.
        /// </summary>
        public T GetValueOrDefault(T defaultValue = default) => Success ? Value : defaultValue;

        /// <summary>
        /// Tries to get the value, returning whether the operation was successful.
        /// </summary>
        public bool TryGetValue(out T value)
        {
            value = Value;
            return Success;
        }
    }
}
