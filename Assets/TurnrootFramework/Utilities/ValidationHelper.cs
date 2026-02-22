namespace Turnroot.Utilities
{
    /// <summary>
    /// Helper class for common validation patterns to reduce code duplication.
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// Validates that an object is not null. Logs a warning if null.
        /// </summary>
        /// <param name="obj">The object to validate.</param>
        /// <param name="objectName">Name of the object for the warning message.</param>
        /// <param name="context">Optional context for where the validation occurred.</param>
        /// <returns>True if the object is not null, false otherwise.</returns>
        public static bool ValidateNotNull(object obj, string objectName, string context = null)
        {
            if (obj == null)
            {
                var message = string.IsNullOrEmpty(context)
                    ? $"{objectName} is null"
                    : $"{context}: {objectName} is null";
                message.LogWarning();
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validates multiple objects are not null. Logs a warning for any that are null.
        /// Backwards-compatible wrapper that returns bool.
        /// </summary>
        /// <param name="context">Context for where the validation occurred.</param>
        /// <param name="validations">Tuples of (object, name) to validate.</param>
        /// <returns>True if all objects are not null, false if any are null.</returns>
        public static bool ValidateNotNull(
            string context,
            params (object obj, string name)[] validations
        ) => ValidateNotNull(context, out _, validations);

        /// <summary>
        /// Validates multiple objects are not null and returns the missing names.
        /// </summary>
        /// <param name="context">Context for where the validation occurred.</param>
        /// <param name="missing">Out list of names that were null.</param>
        /// <param name="validations">Tuples of (object, name) to validate.</param>
        /// <returns>True if all objects are not null, false if any are null.</returns>
        public static bool ValidateNotNull(
            string context,
            out System.Collections.Generic.List<string> missing,
            params (object obj, string name)[] validations
        )
        {
            missing = new System.Collections.Generic.List<string>();
            bool allValid = true;
            foreach (var (obj, name) in validations)
            {
                if (obj == null)
                {
                    var message = string.IsNullOrEmpty(context)
                        ? $"{name} is null"
                        : $"{context}: {name} is null";
                    message.LogWarning();
                    missing.Add(name);
                    allValid = false;
                }
            }
            return allValid;
        }

        /// <summary>
        /// Validates that a string is not null or empty.
        /// </summary>
        /// <param name="str">The string to validate.</param>
        /// <param name="stringName">Name of the string for the warning message.</param>
        /// <param name="context">Optional context for where the validation occurred.</param>
        /// <returns>True if the string is not null or empty, false otherwise.</returns>
        public static bool ValidateNotNullOrEmpty(
            string str,
            string stringName,
            string context = null
        )
        {
            if (string.IsNullOrEmpty(str))
            {
                var message = string.IsNullOrEmpty(context)
                    ? $"{stringName} is null or empty"
                    : $"{context}: {stringName} is null or empty";
                message.LogWarning();
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validates that a collection is not null or empty.
        /// </summary>
        /// <param name="collection">The collection to validate.</param>
        /// <param name="collectionName">Name of the collection for the warning message.</param>
        /// <param name="context">Optional context for where the validation occurred.</param>
        /// <returns>True if the collection is not null or empty, false otherwise.</returns>
        public static bool ValidateNotNullOrEmpty<T>(
            System.Collections.Generic.ICollection<T> collection,
            string collectionName,
            string context = null
        )
        {
            if (collection == null || collection.Count == 0)
            {
                var message = string.IsNullOrEmpty(context)
                    ? $"{collectionName} is null or empty"
                    : $"{context}: {collectionName} is null or empty";
                message.LogWarning();
                return false;
            }
            return true;
        }
    }
}
