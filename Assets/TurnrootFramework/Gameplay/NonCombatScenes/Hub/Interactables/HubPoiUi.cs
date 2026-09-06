using NaughtyAttributes;
using TMPro;
using Turnroot.Characters;
using Turnroot.Gameplay.NonCombatScenes.Hub.Blacksmith;
using Turnroot.Gameplay.PlayerSettings;
using Turnroot.Utilities;
using UnityEngine;
using static Turnroot.Gameplay.NonCombatScenes.Hub.HubManager;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public enum NonShopPoiType
    { // The demo doesn't use all of these, but they are all implemented and usable if you want them
        Blacksmith,
        Healer,
        Enchanter,
        Maps,
        Quests,
        Library,
        Recruitment,
        DanceHall,
        Spa,
    }

    [RequireComponent(typeof(Collider))]
    public class HubPoiUi : HubFadableVisualBase, IHubSelectable
    {
        #region Inspector Fields

        [Tooltip(
            "What kind of interaction this POI drives (MarketPOI, DocksPOI, TrainingPOI, or UnitPOI)."
        )]
        public HubPoiType Type;

        [ShowIf(nameof(Type), HubPoiType.MarketPOI)]
        public bool IsShop = false;

        [HideIf(nameof(IsShop))]
        public NonShopPoiType NonShopType;
        private Shop.Shop _shop;
        private Blacksmith.Blacksmith _blacksmith;
        private Healer _healer;
        private Enchanter _enchanter;
        private Maps _maps;
        private Quests _quests;
        private Library _library;
        private Recruitment _recruitment;
        private DanceHall _dancehall;

        private Spa _spa;

        [Tooltip("The target camera transform to move to when this POI is selected.")]
        public Transform CameraPoint;

        [Tooltip("If true, selecting this POI moves the camera to the CameraPoint.")]
        public bool MoveCameraOnSelect = true;

        [Tooltip("Text element used to show the POI label.")]
        public TextMeshPro Label;

        [Tooltip("Label text displayed on the POI (updated at runtime).")]
        public string LabelText;

        [Tooltip("If this POI represents a unit, the associated character instance.")]
        public CharacterInstance UnitCharacter;

        [Tooltip(
            "The point where the avatar model spawns and toward which the unit turns (set at runtime for Unit POIs)."
        )]
        public Transform AvatarPoint;

        [Tooltip("Optional badge object used to display an icon or status on the POI.")]
        public GameObject Badge;

        [Tooltip(
            "Material used on the badge; an instance is created at runtime to avoid modifying the original."
        )]
        public Material BadgeMaterial;
        private Material _badgeMaterialInstance;

        [Tooltip("Texture used for the badge when enabled.")]
        public Texture BadgeTexture;

        [Tooltip("Texture used when selection is forbidden (e.g., shop closed).")]
        public Texture ForbiddenBadgeTexture;
        private Texture _currentBadgeTexture;

        [Tooltip("Toggle whether the badge is shown on this POI.")]
        public bool ShowBadge = false;

        [HideInInspector]
        public bool CanSelect = true;

        bool IHubSelectable.CanSelect => CanSelect;

        private HubManager hubmanager;

        private bool ChildReferencesSet = false;

        #endregion

        #region Public API

        public void SetLabel(string text)
        {
            LabelText = text;
            if (Label != null)
            {
                Label.text = text;
            }
        }

        public void SetUnitCharacter(CharacterInstance character)
        {
            UnitCharacter = character;
            if (character != null)
            {
                SetLabel(character.CharacterTemplate.DisplayName);
            }
        }

        public void SetBadgeVisible(bool visible)
        {
            Badge?.SetActive(visible);
        }

        public void SetBadgeTexture(Texture texture)
        {
            if (BadgeMaterial != null)
            {
                if (_badgeMaterialInstance == null)
                {
                    _badgeMaterialInstance = Instantiate(BadgeMaterial);
                    if (Badge != null)
                    {
                        if (Badge.TryGetComponent<Renderer>(out var badgeRenderer))
                        {
                            badgeRenderer.material = _badgeMaterialInstance;
                        }
                    }
                }

                if (_badgeMaterialInstance != null)
                {
                    _badgeMaterialInstance.mainTexture = texture;
                }
            }
        }

        #endregion

        #region Lifecycle

        private void Awake()
        {
            hubmanager = HubManager.GetCurrent();
            if (poiVisual == null)
            {
                $"HubPoiUi on {gameObject.name} has no poiVisual assigned, disabling.".LogWarning();
                return;
            }

            if (!IsShop)
            {
                switch (NonShopType)
                {
                    case NonShopPoiType.Blacksmith:
                        _ = TryGetComponent(out _blacksmith);
                        break;
                    case NonShopPoiType.Healer:
                        _ = TryGetComponent(out _healer);
                        break;
                    case NonShopPoiType.Enchanter:
                        _ = TryGetComponent(out _enchanter);
                        break;
                    case NonShopPoiType.Maps:
                        _ = TryGetComponent(out _maps);
                        break;
                    case NonShopPoiType.Quests:
                        _ = TryGetComponent(out _quests);
                        break;
                    case NonShopPoiType.Library:
                        _ = TryGetComponent(out _library);
                        break;
                    case NonShopPoiType.Recruitment:
                        _ = TryGetComponent(out _recruitment);
                        break;
                }
            }

            InitializeVisualMaterials();
            SetupUiState();
            HandleSubLocationType();
            Hide();
        }

        private void Update()
        {
            FaceCamera();
            if (ChildReferencesSet)
            {
                return;
            }
            if (
                NonShopType == NonShopPoiType.Blacksmith
                && !IsShop
                && _blacksmith != null
                && hubmanager != null
            )
            {
                _blacksmith._inventoryBrain = hubmanager._brain.inventoryBrain;
                _blacksmith._storehouseBrain = hubmanager._brain.storehouseBrain;
                _blacksmith._charactersBrain = hubmanager._brain.charactersBrain;

                bool hasRepairWork = hubmanager._brain.charactersBrain.RepairWorkAvailable;
                bool hasForgeWork = hubmanager._brain.charactersBrain.ForgeWorkAvailable;
                $"Blacksmith work available - repair: {hasRepairWork}, forge: {hasForgeWork}".LogInfo();
                if (!hasRepairWork && !hasForgeWork)
                {
                    CanSelect = false;
                    SetBadgeTexture(ForbiddenBadgeTexture);
                    SetLabel("No blacksmith work");
                }
                else if (!hasRepairWork && hasForgeWork)
                {
                    if (_blacksmith.TryGetComponent<BlacksmithUi>(out var blacksmithUi))
                    {
                        blacksmithUi.SetMode(BlacksmithMode.Forge);
                    }
                }
            }

            ChildReferencesSet = true;
        }

        #endregion

        #region Helpers

        private void SetupUiState()
        {
            poiVisual?.SetActive(false);

            SetLabel(LabelText);
            SetBadgeVisible(ShowBadge);
            if (ShowBadge && BadgeMaterial != null && BadgeTexture != null)
            {
                _currentBadgeTexture = BadgeMaterial.mainTexture;
                SetBadgeTexture(BadgeTexture);
            }
        }

        private void HandleSubLocationType()
        {
            switch (Type)
            {
                case HubPoiType.MarketPOI:
                    if (IsShop)
                    {
                        _shop = TryGetComponent<Shop.Shop>(out var shop) ? shop : null;
                        if (
                            _shop == null
                            || (hubmanager != null && !_shop.ShopOpen(hubmanager.gameDate))
                        )
                        {
                            LabelText = _shop == null ? "Unavailable" : "Not Open";
                            CanSelect = false;
                            SetBadgeTexture(ForbiddenBadgeTexture);
                            SetLabel(LabelText);
                        }
                        else
                        {
                            CanSelect = true;
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        #endregion
        #region Selection
        public void Select()
        {
            if (hubmanager == null)
            {
                $"HubPoiUi: No HubManager found for {name}, cannot select.".LogWarning();
                return;
            }

            PlayPoiSelectSound();

            hubmanager.SpecificUiInputHandler.SetCurrentSelection(
                hubmanager.CurrentLocationName,
                this
            );

            bool setChosen = false;
            switch (Type)
            {
                case HubPoiType.MarketPOI:
                case HubPoiType.DocksPOI:
                case HubPoiType.TrainingPOI:
                case HubPoiType.UnitPOI:
                    setChosen = true;
                    break;
            }

            if (setChosen)
            {
                hubmanager.SetInputMode(HubInputMode.Chosen);
            }

            if (CameraPoint != null)
            {
                if (MoveCameraOnSelect)
                {
                    if (
                        hubmanager._brain.cameraBrain != null
                        && GameplayPlayerSettings.Instance.AnimatedCameraMovement
                    )
                    {
                        _ = hubmanager._brain.cameraBrain.StartCameraTransition(
                            hubmanager.GeneralCamera,
                            CameraPoint,
                            fadeDuration
                        );
                    }
                    else if (hubmanager._brain.cameraBrain != null)
                    {
                        _ = hubmanager._brain.cameraBrain.MoveCameraInstant(
                            hubmanager.GeneralCamera,
                            CameraPoint
                        );
                    }
                }
                else
                {
                    // fade to black, move, fade back in (use the same structure as the traversal transition)
                    if (
                        hubmanager.HubFadeToBlack != null
                        && hubmanager.GeneralCamera != null
                        && CameraPoint != null
                    )
                    {
                        UnityEngine.Events.UnityAction onVisible = null;
                        UnityEngine.Events.UnityAction onHidden = null;

                        onVisible = () =>
                        {
                            // Move camera while screen is black
                            hubmanager.GeneralCamera.transform.SetPositionAndRotation(
                                CameraPoint.position,
                                CameraPoint.rotation
                            );

                            hubmanager.HubFadeToBlack.OnVisible.RemoveListener(onVisible);
                            hubmanager.HubFadeToBlack.Hide();
                        };

                        onHidden = () =>
                        {
                            hubmanager.HubFadeToBlack.OnHidden.RemoveListener(onHidden);
                        };

                        hubmanager.HubFadeToBlack.OnVisible.AddListener(onVisible);
                        hubmanager.HubFadeToBlack.OnHidden.AddListener(onHidden);
                        hubmanager.HubFadeToBlack.Show();
                    }
                }
            }
            Hide();
        }
        #endregion
    }
}
