using System.Collections;
using System.Collections.Generic;
using Turnroot.Gameplay.Objects;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    [RequireComponent(typeof(Collider))]
    public class HubSimpleItemPickup
        : HubFadableVisualBase,
            IHubSelectable,
            IDistanceVisibilityHandler
    {
        public bool CanSelect;
        private HubManager _hubManager;
        private int _currentChapter;
        public UIFade ItemPickupUiFade;

        public float FadeOnscreenDuration = 3f;
        private ObjectItem _currentItem;
        public HubSimpleItem[] Items;

        bool IHubSelectable.CanSelect => CanSelect;

        public Transform AvatarPosition;

        public float ShowDistance = 8f;
        public float HideDistance = 10f;

        public bool IsDistanceVisible { get; set; }

        Transform IDistanceVisibilityHandler.AvatarPosition => AvatarPosition;
        float IDistanceVisibilityHandler.ShowDistance => ShowDistance;
        float IDistanceVisibilityHandler.HideDistance => HideDistance;
        public Vector3 DistanceVisibilityPosition => transform.position;
        public string DistanceVisibilityOwnerName => gameObject.name;

        private void Awake()
        {
            _hubManager = HubManager.GetCurrent();
            InitializeVisualMaterials();
            Hide();
            _currentChapter = _hubManager._brain.saveFileBrain.ActiveSaveFile.ChapterNumber;
            _currentItem = InitializeItem();
        }

        private ObjectItem InitializeItem()
        {
            var availableItems = new List<HubSimpleItem>();
            foreach (var item in Items)
            {
                foreach (var chapter in item.AvailableChapters)
                {
                    if (chapter == _currentChapter)
                    {
                        availableItems.Add(item);
                        break;
                    }
                }
            }

            CanSelect = availableItems.Count > 0;

            if (availableItems.Count == 0)
            {
                return null;
            }
            else
            {
                var randomIndex = Random.Range(0, availableItems.Count);
                return availableItems[randomIndex].Item;
            }
        }

        public void Select()
        {
            if (!CanSelect || _currentItem == null)
            {
                return;
            }

            PlayPoiSelectSound();
            _hubManager.SetInputMode(HubManager.HubInputMode.None);
            Hide();

            ItemPickupUiFade?.Show();
            var instance = new ObjectItemInstance(_currentItem);
            _hubManager._brain.storehouseBrain.DepositItem(instance);
            StartCoroutine(FadeOutAfterDuration());
        }

        private IEnumerator FadeOutAfterDuration()
        {
            yield return new WaitForSeconds(FadeOnscreenDuration);
            ItemPickupUiFade?.Hide();
            _hubManager.RevertToPreviousInputMode();
        }

        private void Update()
        {
            FaceCamera();
            this.UpdateDistanceVisibility();
        }
    }
}
