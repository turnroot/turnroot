using System.Collections.Generic;
using Coffee.UIEffects;
using TMPro;
using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.UI.Components.GridMenu;
using Turnroot.UI.Components.Menu;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.UI.Components
{
    /// <summary>
    /// Manages unit selection UI with multiple columns, allowing players to select units for battle.
    /// </summary>
    public class UnitSelectionColumns : MonoBehaviour
    {
        private Brain _brain;
        private int TotalColumns => Columns.Length;

        public GameObject UnitCellPrefab;

        public GameObject UnitCountText;
        public GameObject[] Columns;

        public int MaxSelectedUnits;

        public int SelectedCount;

        public void Initialize(Brain brain)
        {
            _brain = brain;

            var playerTeamRoster =
                _brain.gamewideContextBrain.CreateOrRecallGamewidePersistentPlayerRoster();
            var playerTeamRosterInstance = _brain.gamewideContextBrain.GetOrCreatePlayerTeamRoster(
                playerTeamRoster
            );
            var LongTermMemory = _brain.ltm;

            // Use the persistent roster (Gamewide PlayerTeamRoster) as the authoritative source
            // for unit selection UI. We will only use runtime instances for read-only display data
            // (character instance info) and will never mutate the persistent roster from the UI.
            var units = playerTeamRoster?.characters ?? new Characters.Roster.UnitPlacement[0];
            if (units.Length == 0 && playerTeamRosterInstance != null)
            {
                // Fallback in the unlikely case persistent roster exists but has no characters
                TurnrootLogger.Log(
                    "UnitSelectionColumns: persistent roster has no placements; falling back to runtime instance placements",
                    TurnrootLogger.LogLevel.Warning
                );
                units = playerTeamRosterInstance.GetPlacements();
            }

            // Ensure default selection state (adds required units, applies LTM selections, fills to max)
            PreBattleSelectionHelper.EnsureDefaultPreBattleSelections(
                _brain,
                playerTeamRoster,
                playerTeamRosterInstance,
                MaxSelectedUnits,
                _brain?.battleBrain?.PreparationObject?.RequiredPlayerUnits
            );

            int unitCount = units.Length;
            var u = LtmKeys.UnitSelectedForBattlePrefix;
            var keys = LongTermMemory?.RecallKeysByPrefix(u) ?? new List<string>();
            MaxSelectedUnits =
                _brain?.battleBrain?.PreparationObject?.MaxPlayerTeamUnits ?? MaxSelectedUnits;
            var keysSet = new HashSet<string>(keys);

            // Count currently selected units only among units present in this roster (prefer runtime instance state, fall back to LTM)
            int currentlySelectedCount = 0;

            for (int i = 0; i < unitCount; i++)
            {
                var unit = units[i];
                if (unit == null)
                {
                    continue;
                }

                // Pre-compute an initial selection state for counting purposes. Prefer runtime instance when available.
                var matchedInstance =
                    playerTeamRosterInstance?.GetInstanceFor(unit.CharacterData)
                    ?? _brain.gamewideContextBrain.FindInstanceByTemplate(unit.CharacterData);

                var keyForUnit = u + unit.CharacterData.name;
                var isSelectedForCount =
                    matchedInstance != null
                        ? matchedInstance.IsSelectedForBattle
                        : LongTermMemory?.RecallBool(keyForUnit) ?? false;

                if (isSelectedForCount)
                {
                    currentlySelectedCount++;
                }

                var whichColumn = i % TotalColumns;
                var unitCell = Instantiate(UnitCellPrefab, Columns[whichColumn].transform);

                // Ensure the instantiated unit cell has a GridMenuItem so it appears in the MenuBase menuItems
                if (!unitCell.TryGetComponent<UnitCellGridMenuItem>(out var gridMenuItem))
                {
                    gridMenuItem = unitCell.AddComponent<UnitCellGridMenuItem>();
                    gridMenuItem.IsSelectedForBattle = false;
                    gridMenuItem.CanBeSelectedForBattle = true;
                }

                // Associate the UI item with the runtime CharacterInstance for this unit (if available)
                matchedInstance =
                    playerTeamRosterInstance?.GetInstanceFor(unit.CharacterData)
                    ?? _brain.gamewideContextBrain.FindInstanceByTemplate(unit.CharacterData);
                gridMenuItem.CharacterInstanceData = matchedInstance;

                // Column and Row are used by GridMenu navigation; Row is integer division (floor)
                gridMenuItem.Column = whichColumn;
                gridMenuItem.Row = i / TotalColumns;
                gridMenuItem.SetItemNamePublic($"UnitCell_{unit.CharacterData.name}");

                ConfigureUnitCell(
                    unitCell,
                    unit,
                    u,
                    keysSet,
                    LongTermMemory,
                    ref currentlySelectedCount
                );
            }

            // After instantiating unit cells, refresh parent menu so newly added GridMenuItems are registered
            var parentMenu = GetComponentInParent<MenuBase>(true);
            parentMenu?.RefreshMenuItems();

            // Initialize the displayed selected count from computed selections
            SelectedCount = currentlySelectedCount;

            RecomputeSelectedCount();
            UpdateUnitCountText();
        }

        public void RecomputeSelectedCount()
        {
            int count = 0;
            foreach (var col in Columns)
            {
                if (col == null)
                {
                    continue;
                }

                var cells = col.GetComponentsInChildren<UnitCellGridMenuItem>(true);
                foreach (var c in cells)
                {
                    if (c.IsSelectedForBattle)
                    {
                        count++;
                    }
                }
            }
            SelectedCount = Mathf.Clamp(count, 0, MaxSelectedUnits);
            UpdateUnitCountText();
        }

        private void ConfigureUnitCell(
            GameObject unitCell,
            Characters.Roster.UnitPlacement unit,
            string prefix,
            HashSet<string> keySet,
            LongTermMemory ltm,
            ref int currentlySelectedCount
        )
        {
            var uf = new UtilityFunctions();
            var gridMenuItem = unitCell.GetComponent<UnitCellGridMenuItem>();

            SetNameLabel(uf, unitCell, unit);
            SetPortraitImage(uf, unitCell, unit);
            SetClassLabel(uf, unitCell, gridMenuItem);
            ConfigureSelection(
                uf,
                unitCell,
                gridMenuItem,
                unit,
                prefix,
                ltm,
                ref currentlySelectedCount
            );
        }

        private void SetNameLabel(
            UtilityFunctions uf,
            GameObject unitCell,
            Characters.Roster.UnitPlacement unit
        )
        {
            var nameT = uf.FindChildByTag(unitCell, "UnitCellUnitName");
            if (nameT == null || !nameT.TryGetComponent<TextMeshProUGUI>(out var nameLbl))
            {
                return;
            }

            var name = unit.CharacterData?.DisplayName ?? "";
            nameLbl.text = name;
        }

        private void SetPortraitImage(
            UtilityFunctions uf,
            GameObject unitCell,
            Characters.Roster.UnitPlacement unit
        )
        {
            var portraitT = uf.FindChildByTag(unitCell, "UnitCellUnitPortrait");
            if (portraitT == null || !portraitT.TryGetComponent<UnityEngine.UI.Image>(out var img))
            {
                return;
            }

            var portrait = unit.CharacterData?.DefaultPortrait?.RuntimeSprite;
            if (portrait != null)
            {
                img.sprite = portrait;
            }
        }

        private void SetClassLabel(
            UtilityFunctions uf,
            GameObject unitCell,
            UnitCellGridMenuItem gridMenuItem
        )
        {
            var classT = uf.FindChildByTag(unitCell, "UnitCellUnitClass");
            if (classT == null || !classT.TryGetComponent<TextMeshProUGUI>(out var classLbl))
            {
                return;
            }

            if (gridMenuItem?.CharacterInstanceData != null)
            {
                var inst = gridMenuItem.CharacterInstanceData;
                var className =
                    inst?.GetCurrentClass()?.ClassData?.Identity?.ClassName
                    ?? inst?.CharacterTemplate?.StartingClass?.Identity?.ClassName
                    ?? "n/a";
                classLbl.text = className;
            }
            else
            {
                classLbl.text = "n/a";
            }
        }

        private void ConfigureSelection(
            UtilityFunctions uf,
            GameObject unitCell,
            UnitCellGridMenuItem gridMenuItem,
            Characters.Roster.UnitPlacement unit,
            string prefix,
            LongTermMemory ltm,
            ref int currentlySelectedCount
        )
        {
            var selectedT = uf.FindChildByTag(unitCell, "UnitCellSelected");
            if (selectedT == null || gridMenuItem == null)
            {
                return;
            }

            var selectionIndicator = selectedT.gameObject;
            if (selectionIndicator == null)
            {
                return;
            }

            var key = prefix + unit.CharacterData.name;
            var prep = _brain?.battleBrain?.PreparationObject;
            var isSelected = false;
            if (gridMenuItem.CharacterInstanceData != null)
            {
                isSelected =
                    prep != null
                        ? prep.IsBattleSelected(gridMenuItem.CharacterInstanceData)
                        : gridMenuItem.CharacterInstanceData.IsSelectedForBattle;
            }
            else
            {
                isSelected = ltm.RecallBool(key);
            }

            // If the unit is required for this battle, enable them but don't save it to LTM
            var requiredUnits =
                _brain.battleBrain.PreparationObject?.RequiredPlayerUnits
                ?? new List<Characters.CharacterData>();

            if (requiredUnits.Contains(unit.CharacterData))
            {
                if (!isSelected)
                {
                    isSelected = true;
                    currentlySelectedCount++;
                }

                var requiredT = uf.FindChildByTag(unitCell, "UnitCellRequiredIndicator");
                if (requiredT != null)
                {
                    requiredT.gameObject.SetActive(true);
                    gridMenuItem.CanBeSelectedForBattle = false;
                }
                else
                {
                    gridMenuItem.CanBeSelectedForBattle = true;
                }
            }

            selectionIndicator.SetActive(isSelected);
            gridMenuItem.IsSelectedForBattle = isSelected;

            if (!isSelected)
            {
                return;
            }

#if COFFEE_UIEFFECTS
            if (selectionIndicator.TryGetComponent<UIEffect>(out var uiEffect))
            {
                uiEffect.transitionRate = Random.Range(0, 1f);
            }
#endif
        }

        public void UpdateUnitCountText()
        {
            if (UnitCountText == null)
            {
                return;
            }

            if (
                UnitCountText != null
                && UnitCountText.TryGetComponent<TextMeshProUGUI>(out var textLbl)
            )
            {
                textLbl.text = $"{SelectedCount} / {MaxSelectedUnits} Units";
            }
        }
    }
}
