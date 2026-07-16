using UnityEngine;

namespace Turnroot.Utilities
{
    public class RandomizePosition : MonoBehaviour
    {
        public Transform[] choices;

        public void SetPosition(Transform target)
        {
            if (choices != null && choices.Length > 0)
            {
                target.position = choices[Random.Range(0, choices.Length)].position;
            }
        }
    }
}
