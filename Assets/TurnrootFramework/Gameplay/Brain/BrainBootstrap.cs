using UnityEngine;
using UnityEngine.SceneManagement;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Guarantees the TurnrootBrain scene is loaded before any other scene's
    /// Awake/OnEnable runs. Because this runs BeforeSceneLoad, the Brain and all
    /// of its components are fully initialized before any scene object executes —
    /// no readiness checks, events, or polling are ever required.
    /// </summary>
    public static class BrainBootstrap
    {
        public const string BrainSceneName = "TurnrootBrain";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void LoadBrainBeforeEverything()
        {
            var existing = SceneManager.GetSceneByName(BrainSceneName);
            if (existing.IsValid() && existing.isLoaded)
            {
                return;
            }

            SceneManager.LoadScene(BrainSceneName, LoadSceneMode.Additive);
        }
    }
}
