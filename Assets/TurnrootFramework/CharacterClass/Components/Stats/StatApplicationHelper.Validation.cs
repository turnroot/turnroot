using Turnroot.Utilities;

namespace Turnroot.Characters.CharacterClass
{
    public static partial class StatApplicationHelper
    {
        #region Validation

        /// <summary>
        /// Validate required references for stat operations and return an OperationResult.
        /// Returns SuccessResult() when valid; Failure with missing fields otherwise.
        /// </summary>
        public static OperationResult ValidateReferences(
            CharacterInstance character,
            CharacterClassData classData,
            string operationName
        )
        {
            var checks = new (object obj, string name)[]
            {
                (character, nameof(character)),
                (classData, nameof(classData)),
            };

            bool ok = ValidationHelper.ValidateNotNull(operationName, out var missing, checks);
            if (!ok)
            {
                var msg = string.IsNullOrEmpty(operationName)
                    ? $"Missing required references: {string.Join(", ", missing)}"
                    : $"{operationName}: missing required references: {string.Join(", ", missing)}";
                return OperationResult.Failure(msg);
            }

            return OperationResult.Successful();
        }

        #endregion
    }
}