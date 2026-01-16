using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Small marker component attached to spawned unit model GameObjects to indicate
    /// which runtime unit (by Id) that model belongs to. This makes lookups and cleanup
    /// robust without brittle name parsing.
    /// </summary>
    public class UnitModelOwnership : MonoBehaviour
    {
        [Tooltip("Stable Unit Id from CharacterInstance.Id")]
        public string UnitId;

        [Tooltip("Human friendly display name for debugging")]
        public string DisplayName;
    }
}
