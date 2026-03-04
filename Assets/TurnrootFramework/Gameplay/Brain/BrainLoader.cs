using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Loads the TurnrootBrain scene additively and manages its lifecycle.
    /// </summary>
    public class BrainLoader : MonoBehaviour
    {
        private const string BrainSceneName = "TurnrootBrain";

        private void Awake() => LoadBrainScene();

        private void OnDisable() => UnloadBrainScene();

        private void OnDestroy() => UnloadBrainScene();

        private OperationResult LoadBrainScene()
        {
            try
            {
                // Unity's SceneManager can throw if scene doesn't exist
                SceneManager.LoadScene(BrainSceneName, LoadSceneMode.Additive);
                return OperationResult.Successful();
            }
            catch (System.Exception e)
            {
                var error = $"Failed to load brain scene '{BrainSceneName}': {e.Message}";
                error.LogError();
                return OperationResult.Failure(error);
            }
        }

        private void UnloadBrainScene() => SceneManager.UnloadSceneAsync(BrainSceneName);
    }
}
