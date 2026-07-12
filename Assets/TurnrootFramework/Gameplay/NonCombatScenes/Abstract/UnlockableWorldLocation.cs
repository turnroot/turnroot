using System;
using NaughtyAttributes;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.NonCombatScenes.Abstract
{
    [Serializable]
    public struct WorldVisuals
    {
        public GameObject[] enabledWhenLocked;
        public GameObject[] disabledWhenLocked;
    }

    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class UnlockableWorldLocation : MonoBehaviour
    {
        public Collider playerCollider;
        public Transform fastTravelPoint;
        public bool isUnlocked;

        [InfoBox(
            "If this is false, you will need to manually unlock the location through perhaps a certain chapter or conversation"
        )]
        public bool unlocksOnFirstEntry;
        public UIFade newRegionDisplay;
        public float popupHideDelay = 2f;
        public AudioClip newRegionSfx;
        public AudioSource newRegionAudioSource;
        public WorldVisuals worldVisuals;

        private void Start()
        {
            for (int i = 0; i < worldVisuals.enabledWhenLocked.Length; i++)
            {
                worldVisuals.enabledWhenLocked[i].SetActive(false);
            }
            for (int i = 0; i < worldVisuals.disabledWhenLocked.Length; i++)
            {
                worldVisuals.disabledWhenLocked[i].SetActive(true);
            }
            if (TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = true;
            }
            else
            {
                Debug.LogWarning("UnlockableWorldLocation: No Rigidbody component found.");
            }
        }

        private void OnEnable() => newRegionDisplay.OnVisible.AddListener(HidePopupAfterDelay);

        private void OnDisable() => newRegionDisplay.OnVisible.RemoveListener(HidePopupAfterDelay);

        private void OnTriggerEnter(Collider other)
        {
            if (other == playerCollider && unlocksOnFirstEntry && !isUnlocked)
            {
                Unlock();
            }
        }

        public void Unlock()
        {
            isUnlocked = true;
            newRegionDisplay.Show();
            newRegionAudioSource.PlayOneShot(newRegionSfx);
        }

        private void HidePopupAfterDelay() => newRegionDisplay.HideAfterTime(popupHideDelay);
    }
}
