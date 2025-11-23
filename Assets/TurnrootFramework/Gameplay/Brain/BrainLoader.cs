using UnityEngine;
using UnityEngine.SceneManagement;

namespace TurnrootFramework.Gameplay.Brain
{
    public class BrainLoader : MonoBehaviour
    {
        private void Start()
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
            SceneManager.LoadScene("TurnrootBrain", LoadSceneMode.Additive);
        }
    }
}