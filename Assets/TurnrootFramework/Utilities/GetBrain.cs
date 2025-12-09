using Turnroot.Gameplay.Brain;
using UnityEngine.SceneManagement;

namespace Turnroot.Utilities
{
    /// <summary>
    /// Helper to get the Brain component from an additive scene.
    /// </summary>
    public static class GetBrain
    {
        public static Brain Get()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene.name != "Main")
                {
                    foreach (var rootObj in scene.GetRootGameObjects())
                    {
                        var brain = rootObj.GetComponentInChildren<Brain>();
                        return brain != null ? brain : null;
                    }
                }
            }
            return null;
        }
    }
}
