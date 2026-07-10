using UnityEngine;

namespace Turnroot.Utilities
{
    public class RandomizePositionOnAwake : MonoBehaviour
    {
        public Transform[] choices;

        private void Awake()
        {
            if (choices != null && choices.Length > 0)
            {
                transform.position = choices[Random.Range(0, choices.Length)].position;
            }
        }
    }
}
