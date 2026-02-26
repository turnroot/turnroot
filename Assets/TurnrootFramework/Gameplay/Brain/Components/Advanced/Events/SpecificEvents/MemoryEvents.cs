using System;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.Brain
{
    public partial class Brain
    {
        #region Memory Events

        public event Action<string> OnIllegallyModifiedFileDetected;
        public event Action<int> OnLtmKeyCacheUpdated;

        public void PublishIllegalModification(string message) =>
            OnIllegallyModifiedFileDetected?.Invoke(message);

        public void PublishLtmKeyCacheUpdated(int version) => OnLtmKeyCacheUpdated?.Invoke(version);

        #endregion

        #region Memory Coders

        public string EncodeString(string value)
        {
            var res = DeviceDataCipher.EncryptToBase64(value);
            return res.Success ? res.Value : string.Empty;
        }

        public string DecodeString(string encodedString)
        {
            var res = DeviceDataCipher.DecryptFromBase64(encodedString);
            return res.Success ? res.Value : string.Empty;
        }

        #endregion
    }
}
