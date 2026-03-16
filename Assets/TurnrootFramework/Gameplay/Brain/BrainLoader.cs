using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Loads the TurnrootBrain scene additively and persists across scene changes.
    /// The Brain scene remains loaded for the entire application lifetime.
    /// </summary>
    public class BrainLoader : MonoBehaviour
    {
        private const string BrainSceneName = "TurnrootBrain";
        private static bool _brainSceneLoaded = false;

        private void Awake()
        {
            // Only load the Brain scene once
            if (!_brainSceneLoaded)
            {
                LoadBrainScene();
                _brainSceneLoaded = true;
            }
        }

        private OperationResult LoadBrainScene()
        {
            try
            {
                // Unity's SceneManager can throw if scene doesn't exist
                SceneManager.LoadScene(BrainSceneName, LoadSceneMode.Additive);
                $"BrainLoader: Loaded '{BrainSceneName}' scene additively".LogInfo();
                return OperationResult.Successful();
            }
            catch (System.Exception e)
            {
                var error = $"Failed to load brain scene '{BrainSceneName}': {e.Message}";
                error.LogError();
                return OperationResult.Failure(error);
            }
        }
    }
}
