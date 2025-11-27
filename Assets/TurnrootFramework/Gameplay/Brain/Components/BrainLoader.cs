using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Loads the TurnrootBrain scene additively and manages its lifecycle.
    /// </summary>
    public class BrainLoader : MonoBehaviour
    {
        private const string BrainSceneName = "TurnrootBrain";

        private void Awake()
        {
            LoadBrainScene();
        }

        private void OnDisable()
        {
            UnloadBrainScene();
        }

        private void OnDestroy()
        {
            UnloadBrainScene();
        }

        private void LoadBrainScene()
        {
            Debug.Log($"Loading {BrainSceneName} scene.");

            try
            {
                SceneManager.LoadScene(BrainSceneName, LoadSceneMode.Additive);
                Debug.Log($"{BrainSceneName} scene loaded successfully.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load {BrainSceneName} scene: {e.Message}");
                Debug.Break();
            }
        }

        private void UnloadBrainScene()
        {
            SceneManager.UnloadSceneAsync(BrainSceneName);
        }
    }
}
