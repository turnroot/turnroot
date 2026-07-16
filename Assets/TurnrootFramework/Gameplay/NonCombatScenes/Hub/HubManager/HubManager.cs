using System;
using System.Collections.Generic;
using NaughtyAttributes;
using TMPro;
using Turnroot.Gameplay.NonCombatScenes.Hub.Docks;
using Turnroot.Gameplay.NonCombatScenes.Hub.Shop;
using Turnroot.NonCombatScenes.Abstract;
using Turnroot.UI;
using Turnroot.UI.Components.Notifications;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using Turnroot.Utilities.Weather;
using UnityEngine;
using UnityEngine.Events;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    [Serializable]
    public struct DoThingAtChapter
    {
        public int Chapter;
        public UnityEvent Event;
    }

    [RequireComponent(typeof(UiInputProvider))]
    [RequireComponent(typeof(SpecificUiHandler))]
    [RequireComponent(typeof(FastTravelManager))]
    [RequireComponent(typeof(HubManagerSwitcher))]
    /// <remarks>
    /// This may need editing for your project, but if you aren't making major logic changes, you should
    /// be able to wrangle it to work for you just with UI changes and inspector stuff
    /// </remarks>
    public partial class HubManager : MonoBehaviour
    {
        #region Fields

        [Foldout("Core")]
        [HideInInspector]
        public Brain.Brain _brain;

        [Foldout("Core")]
        [InfoBox("Events that occur in the hub at specific chapters.")]
        public DoThingAtChapter[] DoThingsAtChapters;

        [Foldout("Core")]
        [InfoBox("Input provider used for navigating hub choices.")]
        public UiInputProvider InputProvider;

        [Foldout("Core")]
        [InfoBox("Loading screen controller used during scene transitions.")]
        public LoadingScreenController LoadingScreen;

        [Foldout("Core")]
        [InfoBox("Prefab containing the menu canvas used while settings is open.")]
        public GameObject MenuCanvasPrefab;

        [Foldout("Core")]
        public AudioClip HubBackgroundMusic;

        [Foldout("Core")]
        [InfoBox("Text element used to display the current hub date (day/month/year).")]
        public TextMeshProUGUI dateText;

        [HorizontalLine(color: EColor.Red)]
        [Foldout("UI")]
        public UiChoice EndDay;

        [Foldout("UI")]
        public UiChoice Exit;

        [Foldout("UI")]
        [InfoBox(
            "Activated when a required battle's day limit has been reached and End Day is disabled."
        )]
        public GameObject ForcedBattleIndicator;

        [Foldout("UI")]
        public UiChoice Settings;

        [Foldout("UI")]
        [InfoBox("UiChoice for the Explore entry in the main hub menu.")]
        public UiChoice ExploreChoice;

        [Foldout("UI")]
        [InfoBox("UiChoice for the Battlefields entry in the main hub menu.")]
        public UiChoice BattlefieldsChoice;

        [HorizontalLine(color: EColor.Orange)]
        [Foldout("UI")]
        [InfoBox("The BattleChoiceUI component used to display and navigate available battles.")]
        public BattleChoiceUI BattleChoiceUi;

        private GameObject _menuCanvasInstance;
        private bool _settingsMenuOpen;
        private Action _menuDepthChangedHandler;
        private Character.HubCharacterManager _hubCharacterManager;
        private SceneSkyboxSetter _sceneSkyboxSetter;

        [HorizontalLine(color: EColor.Yellow)]
        [Foldout("Explore/UI")]
        [InfoBox("Tutorial shown the first time Explore is entered.")]
        public GameObject ExploreTutorialPrefab;
        private ShopsManager shopsManager;
        private Dock dock;

        [HorizontalLine(color: EColor.Green)]
        [Foldout("UI")]
        [InfoBox("UI text used to show the current chapter number and name.")]
        public TextMeshProUGUI ChapterNumberAndNameText;

        [Foldout("UI")]
        [InfoBox("Format string for chapter display. {0} = chapter number, {1} = chapter name.")]
        public string ChapterNumberAndNameFormat = "Chapter {0}: {1}";

        [Foldout("Runtime")]
        [HideInInspector]
        public GameDate gameDate;

        [HorizontalLine(color: EColor.Blue)]
        [Foldout("Cameras")]
        [InfoBox("Fade used when returning from traversal/POI interaction back to the hub.")]
        public UIFade HubFadeToBlack;

        [Foldout("Cameras")]
        [InfoBox(
            "The main overlay/HUD to hide when opening any vendor UI and restore when closing it."
        )]
        public UIFade MainOverlayUiFade;

        [Foldout("Cameras")]
        [InfoBox("Fade used to show/hide the hub action UI.")]
        public UIFade HubActionsFade;

        [Foldout("Cameras")]
        [InfoBox("Fade used to show/hide the back button UI.")]
        public UIFade BackButtonFade;

        [Foldout("Cameras")]
        [InfoBox("Field of view used for the hub camera when not in traversal zoom/POI focus.")]
        public float HubMainFov;

        [Foldout("Cameras")]
        [InfoBox("Possible camera positions for randomising the hub camera on load.")]
        public Transform[] cameraPoints;

        [Foldout("Cameras")]
        public Camera GeneralCamera;

        [HorizontalLine(color: EColor.Indigo)]
        [Foldout("Explore/Movement")]
        public Transform TraversalStartAvatarPoint;

        [Foldout("Characters")]
        [InfoBox("Collider used to sample terrain height for unit spawn points.")]
        public MeshCollider SpawnGroundCollider;

        [Foldout("Characters")]
        [InfoBox("Raycast distance used when sampling spawn-point height.")]
        public float SpawnPointRaycastDistance = 20f;

        [HorizontalLine(color: EColor.Violet)]
        [Foldout("UI")]
        public NotificationsHelper notifications;

        private DockShipStatus[] pastShipDockedStatuses;

        private const string dockShipStatusLtmKey = "Hub_DockedShipStatuses";

        [Serializable]
        private class DockShipStatusContainer
        {
            public DockShipStatus[] statuses;
        }

        [Foldout("Runtime")]
        [HideInInspector]
        public HubInputMode CurrentInputMode = HubInputMode.None;

        private UiChoice[] _navigableChoices;

        public HubInputMode PreviousInputMode { get; private set; } = HubInputMode.None;

        public HubSublocationName? CurrentLocationName { get; private set; }
        public Transform CurrentLocationPoint { get; private set; }
        public Transform CurrentTraversalAvatarPoint { get; private set; }

        public enum HubInputMode
        {
            None,
            Location,
            Chosen,
            MarketChoice,
            Battlefields,
            Docks,
            Training,
            Traversal,
        }

        private readonly Dictionary<Transform, float> _spawnPointHeights = new();

        #endregion
    }
}
