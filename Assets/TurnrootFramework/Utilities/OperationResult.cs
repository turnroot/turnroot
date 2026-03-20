using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Turnroot.Services;

namespace Turnroot.Utilities
{
    public struct OperationResult
    {
        public bool Success;
        public string ErrorMessage;
        public Exception Exception { get; private set; }
        public ValidationResult Validation { get; private set; }
        public string CallerMemberName { get; private set; }
        public string CallerFilePath { get; private set; }
        public int CallerLineNumber { get; private set; }

        private static void LogFailure(
            string message,
            Exception ex,
            string caller,
            string file,
            int line
        )
        {
            var fullMessage =
                $"OperationResult Failure: {message} (Caller: {caller} @ {file}:{line})"
                + (ex != null ? $"\nException: {ex}" : string.Empty);
            try
            {
                fullMessage.LogError("OperationResult.LogFailure");
            }
            catch { }
            try
            {
                Trace.TraceError(fullMessage);
            }
            catch { }
        }

        public static OperationResult Successful() =>
            new() { Success = true, Validation = ValidationResult.Success() };

        public static OperationResult Failure(
            string errorMessage,
            [CallerMemberName] string caller = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0
        )
        {
            LogFailure(errorMessage, null, caller, callerFilePath, callerLineNumber);
            return new()
            {
                Success = false,
                ErrorMessage = errorMessage,
                Validation = ValidationResult.Failure(errorMessage),
                CallerMemberName = caller,
                CallerFilePath = callerFilePath,
                CallerLineNumber = callerLineNumber,
            };
        }

        public static OperationResult Failure(
            Exception exception,
            [CallerMemberName] string caller = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0
        )
        {
            var message = exception?.Message ?? "Unknown error";
            LogFailure(message, exception, caller, callerFilePath, callerLineNumber);
            return new()
            {
                Success = false,
                ErrorMessage = message,
                Exception = exception,
                Validation = ValidationResult.Failure(message),
                CallerMemberName = caller,
                CallerFilePath = callerFilePath,
                CallerLineNumber = callerLineNumber,
            };
        }

        public static OperationResult Failure(
            string errorMessage,
            Exception exception,
            [CallerMemberName] string caller = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0
        )
        {
            LogFailure(errorMessage, exception, caller, callerFilePath, callerLineNumber);
            return new()
            {
                Success = false,
                ErrorMessage = errorMessage,
                Exception = exception,
                Validation = ValidationResult.Failure(errorMessage),
                CallerMemberName = caller,
                CallerFilePath = callerFilePath,
                CallerLineNumber = callerLineNumber,
            };
        }

        public static OperationResult FromValidation(
            ValidationResult validation,
            [CallerMemberName] string caller = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0
        ) =>
            validation.IsValid
                ? Successful()
                : Failure(validation.ErrorMessage, caller, callerFilePath, callerLineNumber);

        public static implicit operator OperationResult(ValidationResult validation) =>
            FromValidation(validation);
    }

    public struct OperationResult<T>
    {
        public bool Success { get; private set; }
        public T Value { get; private set; }
        public string Error { get; private set; }
        public Exception Exception { get; private set; }
        public string CallerMemberName { get; private set; }
        public string CallerFilePath { get; private set; }
        public int CallerLineNumber { get; private set; }

        private static void LogFailureGeneric(
            string message,
            Exception ex,
            string caller,
            string file,
            int line
        )
        {
            var fullMessage =
                $"OperationResult<T> Failure: {message} (Caller: {caller} @ {file}:{line})"
                + (ex != null ? $"\nException: {ex}" : string.Empty);
            try
            {
                fullMessage.LogError("OperationResult<T>.LogFailureGeneric");
            }
            catch { }
            try
            {
                Trace.TraceError(fullMessage);
            }
            catch { }
        }

        public static OperationResult<T> SuccessResult(T value) =>
            new() { Success = true, Value = value };

        public static OperationResult<T> Failure(
            string errorMessage,
            [CallerMemberName] string caller = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0
        )
        {
            LogFailureGeneric(errorMessage, null, caller, callerFilePath, callerLineNumber);
            return new()
            {
                Success = false,
                Error = errorMessage,
                Value = default,
                CallerMemberName = caller,
                CallerFilePath = callerFilePath,
                CallerLineNumber = callerLineNumber,
            };
        }

        public static OperationResult<T> Failure(
            Exception exception,
            [CallerMemberName] string caller = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0
        )
        {
            var message = exception?.Message ?? "Unknown error";
            LogFailureGeneric(message, exception, caller, callerFilePath, callerLineNumber);
            return new()
            {
                Success = false,
                Error = message,
                Exception = exception,
                Value = default,
                CallerMemberName = caller,
                CallerFilePath = callerFilePath,
                CallerLineNumber = callerLineNumber,
            };
        }

        public static OperationResult<T> Failure(
            string errorMessage,
            Exception exception,
            [CallerMemberName] string caller = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0
        )
        {
            LogFailureGeneric(errorMessage, exception, caller, callerFilePath, callerLineNumber);
            return new()
            {
                Success = false,
                Error = errorMessage,
                Exception = exception,
                Value = default,
                CallerMemberName = caller,
                CallerFilePath = callerFilePath,
                CallerLineNumber = callerLineNumber,
            };
        }

        public static implicit operator OperationResult<T>(T value) => SuccessResult(value);

        public OperationResult ToNonGeneric() =>
            Success ? OperationResult.Successful() : OperationResult.Failure(Error, Exception);

        public T GetValueOrDefault(T defaultValue = default) => Success ? Value : defaultValue;

        public bool TryGetValue(out T value)
        {
            value = Value;
            return Success;
        }
    }
}
