using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Turnroot.Utilities.Ui
{
    public class ScrollDownNumber : MonoBehaviour
    {
        public TextMeshProUGUI NumberText;
        public int StartNumber = 0;
        public int EndNumber = 100;

        public string Suffix = "";
        public string Prefix = "";

        public int CurrentNumber { get; private set; }
        public float ScrollDuration = 1f;
        public AnimationCurve EaseOutCurve = new(
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(1f, 1f, 2f, 0f)
        );

        private float currentTime = 0f;

        public AudioSource Audio;
        public AudioClip ScrollSound;

        public void OnDestroy()
        {
            if (Audio != null)
            {
                Audio.Stop();
            }
            StopAllCoroutines();
            CurrentNumber = StartNumber;
            currentTime = 0f;
        }

        public void StartScroll()
        {
            if (Audio != null && ScrollSound != null)
            {
                Audio.clip = ScrollSound;
                Audio.Play();
            }
            StopAllCoroutines();
            StartCoroutine(ScrollCoroutine());
        }

        private System.Collections.IEnumerator ScrollCoroutine()
        {
            CurrentNumber = StartNumber;
            currentTime = 0f;

            while (currentTime < ScrollDuration)
            {
                currentTime += Time.deltaTime;
                float t = Mathf.Clamp01(currentTime / ScrollDuration);
                float easedT = EaseOutCurve.Evaluate(t);
                CurrentNumber = Mathf.RoundToInt(Mathf.Lerp(StartNumber, EndNumber, easedT));
                NumberText.text = Prefix + CurrentNumber.ToString() + Suffix;
                yield return null;
            }

            CurrentNumber = EndNumber;
            NumberText.text = Prefix + CurrentNumber.ToString() + Suffix;
        }
    }
}
