using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    /// <summary>
    /// Attach to a battle choice prefab to control which GameObjects are shown
    /// based on whether the battle is a Required Story battle or a Paralogue battle.
    /// </summary>
    public class BattleChoiceTypeDisplay : MonoBehaviour
    {
        [Tooltip("GameObjects to activate when the battle is a Required Story battle.")]
        public GameObject[] RequiredObjects;

        [Tooltip("GameObjects to activate when the battle is a Paralogue battle.")]
        public GameObject[] ParalogueObjects;

        public void SetRequiredActive(bool active) => SetObjects(RequiredObjects, active);

        public void SetParalogueActive(bool active) => SetObjects(ParalogueObjects, active);

        private static void SetObjects(GameObject[] objects, bool active)
        {
            if (objects == null)
            {
                return;
            }

            foreach (var obj in objects)
            {
                obj?.SetActive(active);
            }
        }
    }
}
