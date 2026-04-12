using UnityEngine;

namespace Turnroot.Utilities
{
    [RequireComponent(typeof(AudioSource))]
    public class MusicPlayer : MonoBehaviour
    {
        public AudioSource Player => GetComponent<AudioSource>();
    }
}
