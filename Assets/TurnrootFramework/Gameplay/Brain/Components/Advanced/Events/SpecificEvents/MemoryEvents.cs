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

        public string EncodeString(string value) => DeviceDataCipher.EncryptToBase64(value);

        public string DecodeString(string encodedString) =>
            DeviceDataCipher.DecryptFromBase64(encodedString);

        #endregion
    }
}
