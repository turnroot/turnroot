using Turnroot.Utilities;
using Turnroot.Utilities.Weather;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public partial class EndOfDay : MonoBehaviour
    {
        public SceneSkyboxSetter Weather;

        private void Start()
        {
            if (Weather == null)
            {
                "EndOfDay: Weather reference is missing".LogError();
            }

            var brain = FindFirstObjectByType<Brain.Brain>();
            if (brain != null)
            {
                var date = brain.ltm.GetGameDate();
                if (date != GameDate.Default)
                {
                    HubDayStateStore.Initialize(brain, date);
                }
            }

            Weather.SetupForScenePublic(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
        }
    }
}
