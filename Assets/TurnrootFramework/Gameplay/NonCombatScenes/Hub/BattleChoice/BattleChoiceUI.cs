using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using TMPro;
using Turnroot.GameSettings;
using Turnroot.UI;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;
using UnityEngine.UI;
using static Turnroot.Gameplay.Brain.GamewideContextBrainHelpers;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    /// <summary>
    /// Manages the Battlefields selection UI in the hub.
    /// Reads AllGameBattleChoices from HubManager, cross-references with SceneFlowBrain
    /// to determine which battles are currently available, builds a list of UiChoices,
    /// and populates a detail panel for the highlighted battle.
    /// </summary>
    public class BattleChoiceUI : MonoBehaviour
    {
        #region Inspector Fields

        [BoxGroup("References")]
        [Tooltip("Fade for the entire BattleChoiceUI panel.")]
        public UIFade PanelFade;

        [BoxGroup("References")]
        [Tooltip("Prefab containing a UiChoice component used for each battle list entry.")]
        public GameObject BattleUiChoicePrefab;

        [BoxGroup("References")]
        [Tooltip("Parent container in the canvas where battle choice entries are instantiated.")]
        public Transform ChoiceContainer;

        [BoxGroup("References")]
        [Tooltip("Input provider shared with the hub. Subscribed to while this menu is open.")]
        public UiInputProvider InputProvider;

        [BoxGroup("Confirm Popup")]
        [Tooltip("Fade for the confirm/cancel popup shown when a battle is selected.")]
        public UIFade ConfirmPopupFade;

        [BoxGroup("Confirm Popup")]
        [Tooltip("The Confirm UiChoice inside the popup.")]
        public UiChoice ConfirmChoice;

        [BoxGroup("Confirm Popup")]
        [Tooltip("The Cancel UiChoice inside the popup.")]
        public UiChoice CancelChoice;

        [BoxGroup("Audio")]
        public AudioSource SfxAudio;

        [BoxGroup("Audio")]
        public AudioClip NavigateClip;

        [BoxGroup("Audio")]
        public AudioClip SelectClip;

        [BoxGroup("Detail Panel - Text")]
        public TextMeshProUGUI BattleName;

        [BoxGroup("Detail Panel - Text")]
        public TextMeshProUGUI BattleDescription;

        [BoxGroup("Detail Panel - Difficulty")]
        [Tooltip("Exactly 3 images that represent the difficulty pips (first = pip 1, etc.).")]
        public Image[] DifficultyImages;

        [BoxGroup("Detail Panel - Difficulty")]
        public Sprite DifficultyActiveSprite;

        [BoxGroup("Detail Panel - Difficulty")]
        public Sprite DifficultyInactiveSprite;

        [BoxGroup("Detail Panel - Flags")]
        [Tooltip("GameObjects to activate when the battle is a Required Story battle.")]
        public GameObject[] RequiredObjects;

        [BoxGroup("Detail Panel - Flags")]
        [Tooltip("GameObjects to activate when the battle is a Paralogue battle.")]
        public GameObject[] ParalogueObjects;

        [BoxGroup("Detail Panel - Background")]
        public Color NormalBackgroundColor = Color.white;

        [BoxGroup("Detail Panel - Background")]
        public Color RequiredBackgroundColor = Color.red;

        [BoxGroup("Detail Panel - Background")]
        public Color ParalogueBackgroundColor = Color.blue;

        [BoxGroup("Detail Panel - Background")]
        [Tooltip("Images that receive the background colour based on battle type.")]
        public Image[] BackgroundImages;

        [BoxGroup("Detail Panel - Map")]
        [Tooltip("Images shown when UnexploredMaps is off — display the plain MapSprite.")]
        public Image[] MapImages;

        [BoxGroup("Detail Panel - Map")]
        [Tooltip("Component that renders the 4-quadrant smoky map when UnexploredMaps is on.")]
        public MapQuadrantBlendImage MapQuadrantDisplay;

        #endregion

        #region Private State

        private HubManager _hubManager;
        private Brain.Brain _brain;

        private readonly List<UiChoice> _battleChoices = new();
        private readonly List<BattleChoiceStruct> _availableBattles = new();

        private int _currentIndex;

        private bool _confirmPopupActive;
        private int _confirmPopupIndex; // 0 = Confirm, 1 = Cancel
        #endregion

        #region Public API

        /// <summary>Called by HubManager when the player enters the Battlefields submenu.</summary>
        public void Open(HubManager hubManager)
        {
            _hubManager = hubManager;
            _brain = hubManager._brain;

            BuildChoiceList();

            if (InputProvider != null)
            {
                InputProvider.OnInput += HandleInput;
            }

            PanelFade?.Show();
        }

        /// <summary>Called by HubManager when the player leaves the Battlefields submenu.</summary>
        public void Close()
        {
            CloseConfirmPopup(silent: true);

            if (InputProvider != null)
            {
                InputProvider.OnInput -= HandleInput;
            }

            ClearChoiceList();
            PanelFade?.Hide();
        }

        #endregion

        #region List Building

        private void BuildChoiceList()
        {
            ClearChoiceList();

            if (_hubManager == null || _hubManager.AllGameBattleChoices == null)
            {
                return;
            }

            var availableSceneNames = GetAvailableBattleSceneNames();

            foreach (var battle in _hubManager.AllGameBattleChoices)
            {
                if (battle.BattleScene == null || battle.BattleScene.IsEmpty)
                {
                    continue;
                }

                if (!availableSceneNames.Contains(battle.BattleScene.SceneName))
                {
                    continue;
                }

                _availableBattles.Add(battle);

                var instance = Instantiate(BattleUiChoicePrefab, ChoiceContainer);
                var choice = instance.GetComponent<UiChoice>();

                var label = instance.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    label.text = battle.BattleName;
                }

                _battleChoices.Add(choice);
            }

            _currentIndex = 0;
            UpdateChoiceSelection();
        }

        private HashSet<string> GetAvailableBattleSceneNames()
        {
            var result = new HashSet<string>();

            if (_brain?.sceneFlowBrain == null)
            {
                return result;
            }

            var available = _brain.sceneFlowBrain.GetAvailableScenes();
            if (available == null)
            {
                return result;
            }

            var graph = _brain.sceneFlowBrain.sceneFlowGraph;
            if (graph == null)
            {
                "BattleChoiceUI: No scene flow graph found in Brain.".LogError();
                return result;
            }

            var battleSceneNames = new HashSet<string>(
                graph.GetBattleScenes().Select(n => n.sceneName)
            );

            foreach (var opt in available)
            {
                if (battleSceneNames.Contains(opt.sceneName))
                {
                    result.Add(opt.sceneName);
                }
            }

            return result;
        }

        private void ClearChoiceList()
        {
            foreach (var choice in _battleChoices)
            {
                if (choice != null)
                {
                    Destroy(choice.gameObject);
                }
            }

            _battleChoices.Clear();
            _availableBattles.Clear();
            _currentIndex = 0;
        }

        #endregion

        #region Input

        private void HandleInput(string action)
        {
            if (_confirmPopupActive)
            {
                HandleConfirmPopupInput(action);
                return;
            }

            if (_battleChoices.Count == 0)
            {
                return;
            }

            if (action == InputActionConstants.Cancel)
            {
                _hubManager?.BackFromBattleChoice();
                return;
            }

            if (InputProvider != null)
            {
                InputProvider.Navigate(
                    action,
                    _battleChoices.ToArray(),
                    ref _currentIndex,
                    _battleChoices.Count,
                    ShowConfirmPopup
                );
            }
            else
            {
                UiChoiceHandler.HandleNavigation(
                    action,
                    _battleChoices.ToArray(),
                    ref _currentIndex,
                    _battleChoices.Count,
                    ShowConfirmPopup
                );
            }

            if (action is InputActionConstants.NavigateUp or InputActionConstants.NavigateDown)
            {
                SfxAudio?.PlayOneShot(NavigateClip);
            }

            UpdateChoiceSelection();
        }

        private void ShowConfirmPopup()
        {
            if (_currentIndex < 0 || _currentIndex >= _availableBattles.Count)
            {
                return;
            }

            SfxAudio?.PlayOneShot(SelectClip);
            _confirmPopupActive = true;
            _confirmPopupIndex = 0;
            ConfirmPopupFade?.Show();
            UpdateConfirmPopupSelection();
        }

        private void HandleConfirmPopupInput(string action)
        {
            if (action == InputActionConstants.Cancel)
            {
                CloseConfirmPopup();
                return;
            }

            var choices = new[] { ConfirmChoice, CancelChoice };

            UiChoiceHandler.HandleNavigation(
                action,
                choices,
                ref _confirmPopupIndex,
                choices.Length,
                OnConfirmPopupSelect
            );

            UpdateConfirmPopupSelection();
        }

        private void OnConfirmPopupSelect()
        {
            if (_confirmPopupIndex == 0)
            {
                SfxAudio?.PlayOneShot(SelectClip);
                StartBattle(_availableBattles[_currentIndex]);
            }
            else
            {
                CloseConfirmPopup();
            }
        }

        private void CloseConfirmPopup(bool silent = false)
        {
            if (!_confirmPopupActive && !silent)
            {
                return;
            }

            _confirmPopupActive = false;
            ConfirmPopupFade?.Hide();
            UpdateChoiceSelection();
        }

        private void UpdateConfirmPopupSelection()
        {
            if (_confirmPopupIndex == 0)
            {
                ConfirmChoice?.Select();
                CancelChoice?.Deselect();
            }
            else
            {
                ConfirmChoice?.Deselect();
                CancelChoice?.Select();
            }
        }

        #endregion

        #region Selection & Detail Panel

        private void UpdateChoiceSelection()
        {
            for (int i = 0; i < _battleChoices.Count; i++)
            {
                if (_battleChoices[i] == null)
                {
                    continue;
                }

                if (i == _currentIndex)
                {
                    _battleChoices[i].Select();
                }
                else
                {
                    _battleChoices[i].Deselect();
                }
            }

            if (_currentIndex >= 0 && _currentIndex < _availableBattles.Count)
            {
                UpdateDetailPanel(_availableBattles[_currentIndex]);
            }
        }

        private void UpdateDetailPanel(BattleChoiceStruct battle)
        {
            if (BattleName != null)
            {
                BattleName.text = battle.BattleName;
            }

            if (BattleDescription != null)
            {
                BattleDescription.text = battle.BattleDescription;
            }

            if (DifficultyImages != null)
            {
                for (int i = 0; i < DifficultyImages.Length; i++)
                {
                    if (DifficultyImages[i] == null)
                    {
                        continue;
                    }

                    if (i < battle.BattleDifficulty)
                    {
                        DifficultyImages[i].sprite = DifficultyActiveSprite;
                    }
                    else
                    {
                        DifficultyImages[i].sprite = DifficultyInactiveSprite;
                    }
                }
            }

            SetObjectsActive(RequiredObjects, battle.RequiredStoryBattle);
            SetObjectsActive(ParalogueObjects, battle.ParalogueBattle);

            Color bgColor;
            if (battle.RequiredStoryBattle)
            {
                bgColor = RequiredBackgroundColor;
            }
            else if (battle.ParalogueBattle)
            {
                bgColor = ParalogueBackgroundColor;
            }
            else
            {
                bgColor = NormalBackgroundColor;
            }

            if (BackgroundImages != null)
            {
                foreach (var img in BackgroundImages)
                {
                    if (img != null)
                    {
                        img.color = bgColor;
                    }
                }
            }

            UpdateMapImages(battle);
        }

        private void UpdateMapImages(BattleChoiceStruct battle)
        {
            bool useUnexplored =
                GameplayGeneralSettings.Instance != null
                && GameplayGeneralSettings.Instance.UnexploredMaps;

            if (!useUnexplored)
            {
                if (MapImages != null)
                {
                    foreach (var img in MapImages)
                    {
                        if (img != null)
                        {
                            img.gameObject.SetActive(true);
                            img.sprite = battle.MapSprite;
                        }
                    }
                }

                if (MapQuadrantDisplay != null)
                {
                    MapQuadrantDisplay.gameObject.SetActive(false);
                }

                return;
            }

            // Unexplored maps: hide flat images, show quadrant blend display.
            if (MapImages != null)
            {
                foreach (var img in MapImages)
                {
                    if (img != null)
                    {
                        img.gameObject.SetActive(false);
                    }
                }
            }

            if (MapQuadrantDisplay == null)
            {
                return;
            }

            MapQuadrantDisplay.gameObject.SetActive(true);

            var explorationStatus = GetExplorationStatus(battle);
            MapQuadrantDisplay.SetFromExplorationStatus(
                battle.MapExplorationSprites,
                explorationStatus
            );
        }

        private ExploredStatus GetExplorationStatus(BattleChoiceStruct battle)
        {
            if (
                _brain?.gamewideContextBrain?.MapExplorationStatuses == null
                || battle.MapForExploration == null
            )
            {
                return default;
            }

            return _brain.gamewideContextBrain.MapExplorationStatuses.Find(s =>
                s.map != null && s.map == battle.MapForExploration
            );
        }

        #endregion

        #region Battle Launch

        private void StartBattle(BattleChoiceStruct battle)
        {
            if (_brain?.sceneFlowBrain == null)
            {
                $"BattleChoiceUI: No SceneFlowBrain available to start battle '{battle.BattleName}'.".LogError();
                return;
            }

            _hubManager?.LoadingScreen?.Show();
            _brain.sceneFlowBrain.TransitionToSceneByName(battle.BattleScene.SceneName);
        }

        #endregion

        #region Helpers

        private static void SetObjectsActive(GameObject[] objects, bool active)
        {
            if (objects == null)
            {
                return;
            }

            foreach (var obj in objects)
            {
                if (obj != null)
                {
                    obj.SetActive(active);
                }
            }
        }

        #endregion
    }
}
