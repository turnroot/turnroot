using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Static partial class providing wrapper encoding/decoding between Base64 and JSON.
    /// </summary>
    public static partial class GamewideContextBrainHelpers
    {
        #region Wrapper Encoding/Decoding

        public static OperationResult<SerializedWrapper> DecodeWrapperFromBase64(string encoded)
        {
            if (string.IsNullOrEmpty(encoded))
            {
                return OperationResult<SerializedWrapper>.Failure(
                    "Encoded string is null or empty."
                );
            }

            try
            {
                var wrapperJson = DeviceDataCipher.DecryptFromBase64(encoded);
                var wrapper = JsonConvert.DeserializeObject<SerializedWrapper>(wrapperJson);
                return OperationResult<SerializedWrapper>.SuccessResult(wrapper);
            }
            catch (Exception ex)
            {
                return OperationResult<SerializedWrapper>.Failure(
                    $"Failed to decode wrapper: {ex.Message}",
                    ex
                );
            }
        }

        public static OperationResult<string> EncodeWrapperToBase64(SerializedWrapper wrapper)
        {
            if (wrapper == null)
            {
                return OperationResult<string>.Failure("Wrapper is null.");
            }

            try
            {
                var json = JsonConvert.SerializeObject(wrapper, Formatting.None);
                var encoded = DeviceDataCipher.EncryptToBase64(json);
                return OperationResult<string>.SuccessResult(encoded);
            }
            catch (Exception ex)
            {
                return OperationResult<string>.Failure(
                    $"Failed to encode wrapper: {ex.Message}",
                    ex
                );
            }
        }

        public static OperationResult<JObject> DecodeWrapperAsJObject(string encoded)
        {
            if (string.IsNullOrEmpty(encoded))
            {
                return OperationResult<JObject>.Failure("Encoded string is null or empty.");
            }

            try
            {
                var wrapperJson = DeviceDataCipher.DecryptFromBase64(encoded);
                var obj = JObject.Parse(wrapperJson);
                return OperationResult<JObject>.SuccessResult(obj);
            }
            catch (Exception ex)
            {
                return OperationResult<JObject>.Failure(
                    $"Failed to decode wrapper as JObject: {ex.Message}",
                    ex
                );
            }
        }

        public static OperationResult<string> EncodeJObjectToBase64(JObject wrapper)
        {
            if (wrapper == null)
            {
                return OperationResult<string>.Failure("JObject wrapper is null.");
            }

            try
            {
                var json = wrapper.ToString(Formatting.None);
                var encoded = DeviceDataCipher.EncryptToBase64(json);
                return OperationResult<string>.SuccessResult(encoded);
            }
            catch (Exception ex)
            {
                return OperationResult<string>.Failure(
                    $"Failed to encode JObject: {ex.Message}",
                    ex
                );
            }
        }

        #endregion
    }
}
