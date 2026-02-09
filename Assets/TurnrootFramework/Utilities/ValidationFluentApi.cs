using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Turnroot.Utilities
{
    /// <summary>
    /// Fluent API for building validation chains that return OperationResult.
    /// Allows for readable, chainable validation patterns.
    ///
    /// Example usage:
    /// <code>
    /// return Validate
    ///     .That(unit).IsNotNull(nameof(unit))
    ///     .And(position).IsValid(p => p.x >= 0 && p.y >= 0, "Position out of bounds")
    ///     .ThenExecute(() => SpawnUnitInternal(unit, position));
    /// </code>
    /// </summary>
    public static class Validate
    {
        /// <summary>
        /// Starts a validation chain for a value.
        /// </summary>
        public static ValidationBuilder<T> That<T>(T value)
        {
            return new ValidationBuilder<T>(value);
        }
    }

    /// <summary>
    /// Builder class for constructing fluent validation chains.
    /// </summary>
    public class ValidationBuilder<T>
    {
        private readonly T _value;
        private OperationResult _currentResult;
        private string _caller;
        private string _filePath;
        private int _lineNumber;

        internal ValidationBuilder(
            T value,
            [CallerMemberName] string caller = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0
        )
        {
            _value = value;
            _currentResult = OperationResult.Successful();
            _caller = caller;
            _filePath = filePath;
            _lineNumber = lineNumber;
        }

        /// <summary>
        /// Validates that the value is not null.
        /// Only available when T is a reference type.
        /// </summary>
        public ValidationBuilder<T> IsNotNull(string paramName)
        {
            if (!_currentResult.Success)
            {
                return this; // Already failed, skip further validation
            }

            if (_value == null || EqualityComparer<T>.Default.Equals(_value, default(T)))
            {
                _currentResult = OperationResult.Failure(
                    $"{paramName} is null",
                    _caller,
                    _filePath,
                    _lineNumber
                );
            }

            return this;
        }

        /// <summary>
        /// Validates that a string value is not null or empty.
        /// </summary>
        public ValidationBuilder<T> IsNotNullOrEmpty(string paramName)
        {
            if (!_currentResult.Success)
            {
                return this;
            }

            var stringValue = _value as string;
            if (string.IsNullOrEmpty(stringValue))
            {
                _currentResult = OperationResult.Failure(
                    $"{paramName} is null or empty",
                    _caller,
                    _filePath,
                    _lineNumber
                );
            }

            return this;
        }

        /// <summary>
        /// Validates that a custom condition is true for the value.
        /// </summary>
        public ValidationBuilder<T> IsValid(Func<T, bool> predicate, string errorMessage)
        {
            if (!_currentResult.Success)
            {
                return this;
            }

            if (!predicate(_value))
            {
                _currentResult = OperationResult.Failure(
                    errorMessage,
                    _caller,
                    _filePath,
                    _lineNumber
                );
            }

            return this;
        }

        /// <summary>
        /// Chains additional validation for a different value.
        /// </summary>
        public ValidationBuilder<TNext> And<TNext>(
            TNext nextValue,
            [CallerMemberName] string caller = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0
        )
        {
            var nextBuilder = new ValidationBuilder<TNext>(nextValue, caller, filePath, lineNumber);
            nextBuilder._currentResult = _currentResult; // Carry forward any existing failure
            return nextBuilder;
        }

        /// <summary>
        /// Executes an action if all validations passed, returning the result wrapped in OperationResult.
        /// </summary>
        public OperationResult ThenExecute(Func<OperationResult> action)
        {
            if (!_currentResult.Success)
            {
                return _currentResult;
            }

            try
            {
                return action();
            }
            catch (Exception ex)
            {
                return OperationResult.Failure(
                    $"Execution failed: {ex.Message}",
                    _caller,
                    _filePath,
                    _lineNumber
                );
            }
        }

        /// <summary>
        /// Executes an action if all validations passed.
        /// </summary>
        public OperationResult ThenExecute(Action action)
        {
            if (!_currentResult.Success)
            {
                return _currentResult;
            }

            try
            {
                action();
                return OperationResult.Successful();
            }
            catch (Exception ex)
            {
                return OperationResult.Failure(
                    $"Execution failed: {ex.Message}",
                    _caller,
                    _filePath,
                    _lineNumber
                );
            }
        }

        /// <summary>
        /// Returns the final validation result without executing anything.
        /// Useful when you just want to validate without performing an action.
        /// </summary>
        public OperationResult Result()
        {
            return _currentResult;
        }

        /// <summary>
        /// Implicitly converts to OperationResult for convenience.
        /// </summary>
        public static implicit operator OperationResult(ValidationBuilder<T> builder)
        {
            return builder._currentResult;
        }
    }

    /// <summary>
    /// Additional validation extension methods for common scenarios.
    /// </summary>
    public static class ValidationExtensions
    {
        /// <summary>
        /// Validates that a value is within a range.
        /// </summary>
        public static ValidationBuilder<T> IsInRange<T>(
            this ValidationBuilder<T> builder,
            T min,
            T max,
            string paramName
        )
            where T : IComparable<T>
        {
            return builder.IsValid(
                value => value.CompareTo(min) >= 0 && value.CompareTo(max) <= 0,
                $"{paramName} must be between {min} and {max}"
            );
        }

        /// <summary>
        /// Validates that a value is greater than a minimum.
        /// </summary>
        public static ValidationBuilder<T> IsGreaterThan<T>(
            this ValidationBuilder<T> builder,
            T min,
            string paramName
        )
            where T : IComparable<T>
        {
            return builder.IsValid(
                value => value.CompareTo(min) > 0,
                $"{paramName} must be greater than {min}"
            );
        }

        /// <summary>
        /// Validates that a value is greater than or equal to a minimum.
        /// </summary>
        public static ValidationBuilder<T> IsGreaterThanOrEqual<T>(
            this ValidationBuilder<T> builder,
            T min,
            string paramName
        )
            where T : IComparable<T>
        {
            return builder.IsValid(
                value => value.CompareTo(min) >= 0,
                $"{paramName} must be greater than or equal to {min}"
            );
        }

        /// <summary>
        /// Validates that a collection is not null or empty.
        /// </summary>
        public static ValidationBuilder<T> IsNotNullOrEmpty<T>(
            this ValidationBuilder<T> builder,
            string paramName
        )
            where T : System.Collections.IEnumerable
        {
            return builder.IsValid(
                value =>
                {
                    if (value == null)
                    {
                        return false;
                    }
                    var enumerator = value.GetEnumerator();
                    return enumerator.MoveNext();
                },
                $"{paramName} is null or empty"
            );
        }
    }
}
