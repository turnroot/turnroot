using System;
using System.Runtime.CompilerServices;

namespace Turnroot.Utilities
{
    /// <summary>
    /// Provides guard clause helpers for common OperationResult validation patterns.
    /// Reduces repetitive null/empty checking code throughout the codebase.
    /// </summary>
    public static class OperationResultGuards
    {
        /// <summary>
        /// Validates that a reference type value is not null.
        /// </summary>
        /// <typeparam name="T">The type of value to check (must be a reference type)</typeparam>
        /// <param name="value">The value to validate</param>
        /// <param name="paramName">The name of the parameter being validated</param>
        /// <returns>Success if value is not null, otherwise Failure with descriptive message</returns>
        public static OperationResult RequireNotNull<T>(
            T value,
            string paramName,
            [CallerMemberName] string caller = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0
        )
            where T : class
        {
            if (value == null)
            {
                return OperationResult.Failure(
                    $"{paramName} is null",
                    caller,
                    callerFilePath,
                    callerLineNumber
                );
            }
            return OperationResult.Successful();
        }

        /// <summary>
        /// Validates that a string value is not null or empty.
        /// </summary>
        /// <param name="value">The string to validate</param>
        /// <param name="paramName">The name of the parameter being validated</param>
        /// <returns>Success if value has content, otherwise Failure with descriptive message</returns>
        public static OperationResult RequireNotNullOrEmpty(
            string value,
            string paramName,
            [CallerMemberName] string caller = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0
        )
        {
            if (string.IsNullOrEmpty(value))
            {
                return OperationResult.Failure(
                    $"{paramName} is null or empty",
                    caller,
                    callerFilePath,
                    callerLineNumber
                );
            }
            return OperationResult.Successful();
        }

        /// <summary>
        /// Validates that a string value is not null, empty, or whitespace.
        /// </summary>
        /// <param name="value">The string to validate</param>
        /// <param name="paramName">The name of the parameter being validated</param>
        /// <returns>Success if value has non-whitespace content, otherwise Failure with descriptive message</returns>
        public static OperationResult RequireNotNullOrWhiteSpace(
            string value,
            string paramName,
            [CallerMemberName] string caller = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0
        )
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return OperationResult.Failure(
                    $"{paramName} is null, empty, or whitespace",
                    caller,
                    callerFilePath,
                    callerLineNumber
                );
            }
            return OperationResult.Successful();
        }

        /// <summary>
        /// Validates that a custom condition is true.
        /// </summary>
        /// <param name="condition">The condition to validate</param>
        /// <param name="errorMessage">The error message to return if condition is false</param>
        /// <returns>Success if condition is true, otherwise Failure with the provided message</returns>
        public static OperationResult Require(
            bool condition,
            string errorMessage,
            [CallerMemberName] string caller = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0
        )
        {
            if (!condition)
            {
                return OperationResult.Failure(
                    errorMessage,
                    caller,
                    callerFilePath,
                    callerLineNumber
                );
            }
            return OperationResult.Successful();
        }

        /// <summary>
        /// Validates that a custom condition evaluated from a value is true.
        /// </summary>
        /// <typeparam name="T">The type of value to validate</typeparam>
        /// <param name="value">The value to validate</param>
        /// <param name="predicate">The condition to evaluate on the value</param>
        /// <param name="errorMessage">The error message to return if predicate returns false</param>
        /// <returns>Success if predicate returns true, otherwise Failure with the provided message</returns>
        public static OperationResult RequireThat<T>(
            T value,
            Func<T, bool> predicate,
            string errorMessage,
            [CallerMemberName] string caller = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0
        )
        {
            if (!predicate(value))
            {
                return OperationResult.Failure(
                    errorMessage,
                    caller,
                    callerFilePath,
                    callerLineNumber
                );
            }
            return OperationResult.Successful();
        }

        /// <summary>
        /// Combines multiple validation results. Returns the first failure encountered, or success if all pass.
        /// </summary>
        /// <param name="results">The validation results to combine</param>
        /// <returns>The first failure, or success if all validations passed</returns>
        public static OperationResult All(params OperationResult[] results)
        {
            foreach (var result in results)
            {
                if (!result.Success)
                {
                    return result;
                }
            }
            return OperationResult.Successful();
        }
    }
}
