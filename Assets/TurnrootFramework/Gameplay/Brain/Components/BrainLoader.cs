using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Turnroot.Gameplay.Brain
{
    public class BrainLoader : MonoBehaviour
    {
        private void Awake()
        {
            LoadBrainScene();
        }

        private void OnDisable()
        {
            SceneManager.UnloadSceneAsync("TurnrootBrain");
        }

        private void OnDestroy()
        {
            SceneManager.UnloadSceneAsync("TurnrootBrain");
        }

        private void LoadBrainScene()
        {
            Debug.Log("Loading TurnrootBrain scene.");
            try
            {
                SceneManager.LoadScene("TurnrootBrain", LoadSceneMode.Additive);
                Debug.Log("TurnrootBrain scene loaded successfully.");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to load TurnrootBrain scene: " + e.Message);
                Debug.Break();
            }
        }
    }
}
