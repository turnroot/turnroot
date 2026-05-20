using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using TMPro;
using Turnroot.GameSettings;
using Turnroot.UI;
using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.UI;

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
        [Tooltip("Prefab containing a UiChoice component used for each battle list entry.")]
        public GameObject BattleUiChoicePrefab;

        [BoxGroup("References")]
        [Tooltip("Parent container in the canvas where battle choice entries are instantiated.")]
        public Transform ChoiceContainer;

        [BoxGroup("References")]
        [Tooltip("Input provider shared with the hub. Subscribed to while this menu is open.")]
        public UiInputProvider InputProvider;

        [BoxGroup("Detail Panel – Text")]
        public TextMeshProUGUI BattleName;

        [BoxGroup("Detail Panel – Text")]
        public TextMeshProUGUI BattleDescription;

        [BoxGroup("Detail Panel – Difficulty")]
        [Tooltip("Exactly 3 images that represent the difficulty pips (first = pip 1, etc.).")]
        public Image[] DifficultyImages;

        [BoxGroup("Detail Panel – Difficulty")]
        public Sprite DifficultyActiveSprite;

        [BoxGroup("Detail Panel – Difficulty")]
        public Sprite DifficultyInactiveSprite;

        [BoxGroup("Detail Panel – Flags")]
        [Tooltip("GameObjects to activate when the battle is a Required Story battle.")]
        public GameObject[] RequiredObjects;

        [BoxGroup("Detail Panel – Flags")]
        [Tooltip("GameObjects to activate when the battle is a Paralogue battle.")]
        public GameObject[] ParalogueObjects;

        [BoxGroup("Detail Panel – Background")]
        public Color NormalBackgroundColor = Color.white;

        [BoxGroup("Detail Panel – Background")]
        public Color RequiredBackgroundColor = Color.red;

        [BoxGroup("Detail Panel – Background")]
        public Color ParalogueBackgroundColor = Color.blue;

        [BoxGroup("Detail Panel – Background")]
        [Tooltip("Images that receive the background colour based on battle type.")]
        public Image[] BackgroundImages;

        [BoxGroup("Detail Panel – Map")]
        [Tooltip("Images that display the map sprite for the highlighted battle.")]
        public Image[] MapImages;

        #endregion

        #region Private State

        private HubManager _hubManager;
        private Brain.Brain _brain;

        private readonly List<UiChoice> _battleChoices = new();
        private readonly List<BattleChoiceStruct> _availableBattles = new();

        private int _currentIndex;

        #endregion

        #region Public API

        /// <summary>
        /// Called by HubManager when the player enters the Battlefields submenu.
        /// </summary>
        public void Open(HubManager hubManager)
        {
            _hubManager = hubManager;
            _brain = hubManager._brain;

            BuildChoiceList();

            if (InputProvider != null)
            {
                InputProvider.OnInput += HandleInput;
            }
        }

        /// <summary>
        /// Called by HubManager when the player leaves the Battlefields submenu.
        /// </summary>
        public void Close()
        {
            if (InputProvider != null)
            {
                InputProvider.OnInput -= HandleInput;
            }

            ClearChoiceList();
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

            // Determine available battle scene names via SceneFlowBrain.
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

                // Set the choice label to the battle name.
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

            // All scenes reachable from the current scene.
            var available = _brain.sceneFlowBrain.GetAvailableScenes();
            if (available == null)
            {
                return result;
            }

            // Collect scene names that are flagged as battles in the graph.
            var graph = _brain.sceneFlowBrain.sceneFlowGraph;
            if (graph == null)
            {
                "No scene flow graph found in Brain while building battle choice list.".LogError();
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
            if (_battleChoices.Count == 0)
            {
                return;
            }

            if (InputProvider != null)
            {
                InputProvider.Navigate(
                    action,
                    _battleChoices.ToArray(),
                    ref _currentIndex,
                    _battleChoices.Count,
                    OnSelectPressed
                );
            }
            else
            {
                UiChoiceHandler.HandleNavigation(
                    action,
                    _battleChoices.ToArray(),
                    ref _currentIndex,
                    _battleChoices.Count,
                    OnSelectPressed
                );
            }

            if (action is InputActionConstants.Cancel or InputActionConstants.NavigateLeft)
            {
                _hubManager?.BackFromBattleChoice();
                return;
            }

            UpdateChoiceSelection();
        }

        private void OnSelectPressed()
        {
            if (_currentIndex < 0 || _currentIndex >= _availableBattles.Count)
            {
                return;
            }

            var battle = _availableBattles[_currentIndex];
            StartBattle(battle);
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
            // Text fields.
            if (BattleName != null)
            {
                BattleName.text = battle.BattleName;
            }

            if (BattleDescription != null)
            {
                BattleDescription.text = battle.BattleDescription;
            }

            // Difficulty pips.
            if (DifficultyImages != null)
            {
                for (int i = 0; i < DifficultyImages.Length; i++)
                {
                    if (DifficultyImages[i] == null)
                    {
                        continue;
                    }

                    DifficultyImages[i].sprite =
                        i < battle.BattleDifficulty
                            ? DifficultyActiveSprite
                            : DifficultyInactiveSprite;
                }
            }

            // Required / paralogue flags.
            SetObjectsActive(RequiredObjects, battle.RequiredStoryBattle);
            SetObjectsActive(ParalogueObjects, battle.ParalogueBattle);

            // Background colour.
            Color bgColor =
                battle.RequiredStoryBattle ? RequiredBackgroundColor
                : battle.ParalogueBattle ? ParalogueBackgroundColor
                : NormalBackgroundColor;

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

            // Map sprite.
            UpdateMapImages(battle);
        }

        private void UpdateMapImages(BattleChoiceStruct battle)
        {
            if (MapImages == null || MapImages.Length == 0)
            {
                return;
            }

            bool useUnexplored =
                GameplayGeneralSettings.Instance != null
                && GameplayGeneralSettings.Instance.UnexploredMaps;

            if (!useUnexplored)
            {
                // Simple case: just show the plain map sprite.
                foreach (var img in MapImages)
                {
                    if (img != null)
                    {
                        img.sprite = battle.MapSprite;
                    }
                }
            }
            // TODO: unexplored-maps logic (quadrant sprites from MapExplorationSprites)
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
