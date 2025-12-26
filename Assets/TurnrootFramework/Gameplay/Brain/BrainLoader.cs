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

        private void LoadBrainScene()
        {
#if UNITY_EDITOR
            Debug.Log($"Loading {BrainSceneName} scene.");
#endif

            try
            {
                SceneManager.LoadScene(BrainSceneName, LoadSceneMode.Additive);
#if UNITY_EDITOR
                Debug.Log($"{BrainSceneName} scene loaded successfully.");
#endif
            }
            catch (System.Exception e)
            {
#if UNITY_EDITOR
                Debug.LogError($"Failed to load {BrainSceneName} scene: {e.Message}");
#endif
                Debug.Break();
            }
        }

        private void UnloadBrainScene() => SceneManager.UnloadSceneAsync(BrainSceneName);
    }
}
