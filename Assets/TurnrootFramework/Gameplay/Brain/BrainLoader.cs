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
                SceneManager.LoadScene(BrainSceneName, LoadSceneMode.Additive);
                return OperationResult.Successful();
            }
            catch (System.Exception e)
            {
                TurnrootLogger.Log(
                    $"Failed to load brain scene '{BrainSceneName}': {e.Message}",
                    TurnrootLogger.LogLevel.Error
                );
                Debug.Break();
                return OperationResult.Failure(
                    $"Failed to load brain scene '{BrainSceneName}': {e.Message}"
                );
            }
        }

        private void UnloadBrainScene() => SceneManager.UnloadSceneAsync(BrainSceneName);
    }
}
