using System.Collections.Generic;
using NaughtyAttributes;
using TMPro;
using Turnroot.Gameplay.Combat;
using Turnroot.UI;
using Turnroot.Utilities.AbstractScripts;
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
    public partial class BattleChoiceUI : MonoBehaviour
    {
        #region Inspector Fields

        [BoxGroup("References")]
        [InfoBox("Fade for the entire BattleChoiceUI panel.")]
        public UIFade PanelFade;

        [BoxGroup("References")]
        [InfoBox("Prefab containing a UiChoice component used for each battle list entry.")]
        public GameObject BattleUiChoicePrefab;

        [BoxGroup("References")]
        [InfoBox("Parent container in the canvas where battle choice entries are instantiated.")]
        public VerticalLayoutGroup ChoiceContainer;

        [BoxGroup("Confirm Popup")]
        [InfoBox("Fade for the confirm/cancel popup shown when a battle is selected.")]
        public UIFade ConfirmPopupFade;

        [BoxGroup("Confirm Popup")]
        [InfoBox("The Confirm UiChoice inside the popup.")]
        public UiChoice ConfirmChoice;

        [BoxGroup("Confirm Popup")]
        [InfoBox("The Cancel UiChoice inside the popup.")]
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

        [BoxGroup("Detail Panel - Background")]
        public Color NormalBackgroundColor = Color.white;

        [BoxGroup("Detail Panel - Background")]
        public Color RequiredBackgroundColor = Color.red;

        [BoxGroup("Detail Panel - Background")]
        public Color ParalogueBackgroundColor = Color.blue;

        [BoxGroup("Detail Panel - Background")]
        [InfoBox("Images that receive the background colour based on battle type.")]
        public Image[] BackgroundImages;

        [BoxGroup("Detail Panel - Map")]
        [InfoBox("Images shown when UnexploredMaps is off — display the plain MapSprite.")]
        public Image MapImage;

        [BoxGroup("Detail Panel - Map")]
        [InfoBox("Component that renders the 4-quadrant smoky map when UnexploredMaps is on.")]
        public MapQuadrantBlendImage MapQuadrantDisplay;

        [BoxGroup("Detail Panel - Rewards")]
        public TextMeshProUGUI GoldRewardText;

        [BoxGroup("Detail Panel - Rewards")]
        public VerticalLayoutGroup ItemsRewardContainer;

        [BoxGroup("Detail Panel - Rewards")]
        public TextMeshProUGUI ItemRewardLabelPrefab;

        #endregion

        #region Private State

        private HubManager _hubManager;
        private Brain.Brain _brain;

        private readonly List<UiChoice> _battleChoices = new();
        private readonly List<AllGameBattlesTable.BattleEntry> _availableBattles = new();
        private readonly List<GameObject> _rewardItemLabels = new();

        private int _currentIndex;

        private bool _confirmPopupActive;
        private int _confirmPopupIndex; // 0 = Confirm, 1 = Cancel
        #endregion

        #region Public API
        public void Open(HubManager hubManager)
        {
            _hubManager = hubManager;
            _brain = hubManager._brain;

            BuildChoiceList();
            PanelFade?.Show();
        }

        public void Close()
        {
            CloseConfirmPopup(silent: true);
            ClearChoiceList();
            PanelFade?.Hide();
        }

        public void ForwardInput(string action) => HandleInput(action);

        #endregion
    }
}
