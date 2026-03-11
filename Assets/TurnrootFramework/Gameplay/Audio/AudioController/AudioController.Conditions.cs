using UnityEngine;

namespace Turnroot.Gameplay.Audio
{
    public partial class AudioController : MonoBehaviour
    {
        #region Runtime Conditions

        public void SetCondition(string key, bool value) => _runtimeConditions[key] = value;

        public void ClearCondition(string key) => _runtimeConditions.Remove(key);

        public void ClearAllConditions() => _runtimeConditions.Clear();

        #endregion
    }
}

