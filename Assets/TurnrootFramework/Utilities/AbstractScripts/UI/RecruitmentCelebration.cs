using NaughtyAttributes;
using TMPro;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain;
using UnityEngine;
using UnityEngine.Playables;

namespace Turnroot.Utilities.AbstractScripts.UI
{
    public class RecruitmentCelebration : MonoBehaviour
    {
        public AudioSource audioSource;
        public AudioClip recruitmentFanfare;
        public PlayableDirector celebrationTimeline;

        [HideInInspector]
        public CharacterInstance characterInstance;
        private Brain _brain;

        public TextMeshProUGUI NameText;

        public string Suffix = " has joined your team!";

        public void Activate(CharacterInstance character = null)
        {
            characterInstance = character;
            _brain = FindFirstObjectByType<Brain>();
            if (audioSource != null && recruitmentFanfare != null)
            {
                audioSource.PlayOneShot(recruitmentFanfare);
            }

            celebrationTimeline.stopped += Progress;
            if (characterInstance != null)
            {
                NameText.text = characterInstance.CharacterTemplate.DisplayName + Suffix;
            }
            celebrationTimeline.Play();
        }

        public void Progress(PlayableDirector _)
        {
            if (_brain != null)
            {
                _brain.PublishHubCharacterRecruitCompleted(characterInstance);
            }
            else
            {
                "RecruitmentCelebration: Brain instance not found, cannot publish recruit completion event.".LogWarning(
                    "RecruitmentCelebration"
                );
            }
        }

        public void OnDestroy() => celebrationTimeline.stopped -= Progress;

        [Button]
        public void Preview() => Activate(null);
    }
}
