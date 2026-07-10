using UnityEngine;

namespace Turnroot.Utilities
{
    public class RandomizePosition : MonoBehaviour
    {
        public Transform[] choices;

        public void SetPosition()
        {
            if (choices != null && choices.Length > 0)
            {
                transform.position = choices[Random.Range(0, choices.Length)].position;
            }
        }
    }
}
